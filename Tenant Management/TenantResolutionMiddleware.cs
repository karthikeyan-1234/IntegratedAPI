namespace IntegratedAPI.Tenant_Management
{
    // Middleware to extract tenant from JWT and set in HttpContext
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract from "groups" claim in JWT
            var tenantClaim = context.User?.FindFirst("groups")?.Value;

            if (!string.IsNullOrEmpty(tenantClaim))
            {
                // Format: "/WIPRO" -> "WIPRO"
                var tenantId = tenantClaim.Trim('/');
                context.Items["TenantId"] = tenantId;
            }

            await _next(context);
        }
    }

    // Extension method for easy access
    public static class HttpContextExtensions
    {
        public static string GetTenantId(this HttpContext context)
        {
            return context.Items["TenantId"] as string;
        }
    }
}
