namespace IntegratedAPI.Tenant_Management
{
    public interface ITenantConnectionStringProvider
    {
        Task<string?> GetConnectionStringAsync(string tenantId);
    }
}
