using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace IntegratedAPI.Auth
{
    public class KeycloakAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<KeycloakAuthorizationFilter> _logger;
        private readonly IConfiguration _configuration;

        private readonly string? _realm;
        private readonly string? _authServerUrl;
        private readonly string? _resource;
        private readonly string? _secret;

        public KeycloakAuthorizationFilter(IHttpClientFactory httpClientFactory, ILogger<KeycloakAuthorizationFilter> logger, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("KeycloakUMA");
            _logger = logger;
            _configuration = configuration;

            // Bind Keycloak settings from configuration
            var keycloakSection = _configuration.GetSection("Keycloak");
            _realm = keycloakSection.GetValue<string>("realm");
            _authServerUrl = keycloakSection.GetValue<string>("auth-server-url");
            _resource = keycloakSection.GetValue<string>("resource");
            _secret = keycloakSection.GetSection("credentials").GetValue<string>("secret");

        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
            {
                _logger.LogDebug("Action descriptor is not a ControllerActionDescriptor, skipping.");
                return;
            }

            var _httpContext = context.HttpContext;

            var methodMetadata = controllerActionDescriptor.MethodInfo.GetCustomAttributes(true);
            var controllerMetadata = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(true);
            var metadata = methodMetadata.Concat(controllerMetadata).ToArray();

            bool hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();
            if (hasAllowAnonymous)
            {
                _logger.LogDebug("AllowAnonymous detected, skipping authorization.");
                return;
            }

            var resourceAttr = metadata.OfType<ResourceAttribute>().FirstOrDefault();
            var permissionAttr = metadata.OfType<PermissionAttribute>().FirstOrDefault();

            if (resourceAttr == null || permissionAttr == null)
            {
                _logger.LogWarning("Access denied: Missing [Resource] or [Permission] attribute on protected endpoint.");
                context.Result = new ForbidResult();
                return;
            }

            var resource = resourceAttr.Name;
            var permissions = permissionAttr.permission_list
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Access denied: Missing or invalid Authorization header.");
                context.Result = new UnauthorizedResult();
                return;
            }

            var accessToken = authHeader.Substring("Bearer ".Length);

            var user = _httpContext.User; //✅ Ensure that in Keycloak, for api-app, a mapper for audience exists for this to work.

            foreach (var permission in permissions)
            {
                if (await CheckPermissionAsync(accessToken, resource, permission))
                {
                    _logger.LogInformation("Access granted for {Resource}#{Permission}", resource, permission);

                    var tenant = GetGroupFromToken(accessToken)!;
                    _httpContext.Items["tenant"] = tenant;
                    _httpContext.Items["group"] = tenant;

                    // ✅ ADD "Groups" claim to HttpContext.User
                    var claims = new List<Claim>
                        {
                            new Claim("Groups", tenant),  // Maps to HttpContext.User.FindFirst("Groups")
                            new Claim("Tenant", tenant)
                        };

                    var appIdentity = new ClaimsIdentity(claims, user.Identity?.AuthenticationType);
                    _httpContext.User.AddIdentity(appIdentity);  // ✅ Merges into current principal

                    return;
                }
            }

            _logger.LogWarning("Access denied: No matching UMA permissions found for {Resource}.", resource);
            context.Result = new ForbidResult();
        }

        private async Task<bool> CheckPermissionAsync(string token, string resource, string permission)
        {
            try
            {
                // Construct Keycloak token endpoint URL dynamically from config
                var tokenEndpoint = $"{_authServerUrl!.TrimEnd('/')}/realms/{_realm}/protocol/openid-connect/token";

                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
                    new KeyValuePair<string, string>("audience", _resource!),
                    new KeyValuePair<string, string>("response_mode", "decision"),
                    new KeyValuePair<string, string>("permission", $"{resource}#{permission}")
                });

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keycloak authorization failed ({StatusCode}): {Response}",
                        response.StatusCode, content);
                    return false;
                }

                // Check for JSON response containing "result": true
                return content.Contains("\"result\":true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error contacting Keycloak for permission check. Check connectivity.");
                return false;
            }
        }
    
    
        private string? GetGroupFromToken(string Token)
        {
            if (string.IsNullOrEmpty(Token))
                return null;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(Token);
            return token.Claims.FirstOrDefault(c => c.Type == "Groups")?.Value;
        }
    }
}
