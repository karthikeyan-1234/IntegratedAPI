using IntegratedAPI.DTOs;
using IntegratedAPI.Tenant_Management;

using System.Security.Claims;
using System.Text.Json;

namespace IntegratedAPI.Tenant_Management { 
    /// <summary>
    /// Resolves tenant information from Keycloak JWT "groups" claim
    /// </summary>
    public class KeycloakTenantResolver : ITenantResolver
    {
        private readonly ILogger<KeycloakTenantResolver> _logger;
        private readonly ITenantConnectionStringProvider _connectionStringProvider;

        public KeycloakTenantResolver(
            ILogger<KeycloakTenantResolver> logger,
            ITenantConnectionStringProvider connectionStringProvider)
        {
            _logger = logger;
            _connectionStringProvider = connectionStringProvider;
        }

        public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context)
        {
            try
            {
                var user = context.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogDebug("User is not authenticated, skipping tenant resolution");
                    return null;
                }

                // Extract groups claim (Keycloak stores groups as JSON array in "groups" claim)
                var groupsClaim = user.FindFirst("groups");

                if (groupsClaim == null)
                {
                    _logger.LogWarning("No 'groups' claim found in JWT token for user {User}",
                        user.Identity.Name ?? "Unknown");
                    return null;
                }

                // Parse the groups claim value
                string? tenantId = null;

                try
                {
                    // Groups claim can be either a JSON array or a single string
                    var groupsValue = groupsClaim.Value;

                    if (groupsValue.StartsWith("["))
                    {
                        // It's a JSON array
                        var groups = JsonSerializer.Deserialize<string[]>(groupsValue);

                        if (groups == null || groups.Length == 0)
                        {
                            _logger.LogWarning("Groups claim is empty for user {User}", user.Identity.Name);
                            return null;
                        }

                        // Take the first group as tenant identifier
                        // Remove leading slash if present (Keycloak groups often have /GROUPNAME format)
                        tenantId = groups[0].TrimStart('/');

                        _logger.LogInformation("Resolved tenant {TenantId} from groups: {Groups}",
                            tenantId, string.Join(", ", groups));
                    }
                    else
                    {
                        // It's a single string value
                        tenantId = groupsValue.TrimStart('/');
                        _logger.LogInformation("Resolved tenant {TenantId} from single group claim", tenantId);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse groups claim: {GroupsClaim}", groupsClaim.Value);
                    return null;
                }

                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogWarning("Tenant ID is empty after parsing groups claim");
                    return null;
                }

                // Get connection string for this tenant
                var connectionString = await _connectionStringProvider.GetConnectionStringAsync(tenantId);

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("No connection string found for tenant {TenantId}", tenantId);
                    return null;
                }

                var tenantInfo = new TenantInfo
                {
                    TenantId = tenantId,
                    ConnectionString = connectionString,
                    Metadata = new Dictionary<string, string>
                    {
                        ["UserId"] = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                        ["Email"] = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                        ["PreferredUsername"] = user.FindFirst("preferred_username")?.Value ?? string.Empty
                    }
                };

                _logger.LogInformation("Successfully resolved tenant {TenantId} for user {User}",
                    tenantId, user.Identity.Name);

                return tenantInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during tenant resolution");
                return null;
            }
        }
    }
}
