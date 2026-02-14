using IntegratedAPI.DTOs;

namespace IntegratedAPI.Tenant_Management
{
    public interface ITenantResolver
    {
        Task<TenantInfo?> ResolveTenantAsync(HttpContext context);
    }
}
