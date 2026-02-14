using IntegratedAPI.Tenant_Management;

namespace IntegratedAPI.Middlewares
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        // Standard key for storing tenant info in HttpContext.Items
        public const string TenantInfoKey = "TenantInfo";

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
        {
            try
            {
                // Resolve tenant from the current request
                var tenantInfo = await tenantResolver.ResolveTenantAsync(context);

                if (tenantInfo?.IsResolved == true)
                {
                    // Store tenant info in HttpContext.Items for access throughout the request pipeline
                    context.Items[TenantInfoKey] = tenantInfo;

                    _logger.LogDebug("Tenant {TenantId} resolved and stored in HttpContext for request {Path}",
                        tenantInfo.TenantId, context.Request.Path);
                }
                else
                {
                    _logger.LogDebug("No tenant resolved for request {Path}", context.Request.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TenantMiddleware while resolving tenant");
                // Don't fail the request, just continue without tenant info
            }

            // Continue to the next middleware
            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for registering tenant middleware
    /// </summary>
    public static class TenantMiddlewareExtensions
    {
        /// <summary>
        /// Adds tenant resolution middleware to the pipeline
        /// Should be added after UseAuthentication() to ensure User claims are available
        /// </summary>
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}
