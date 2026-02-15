namespace IntegratedAPI.Tenant_Management
{
    public interface ITenantConnectionStringProvider
    {
        Task<string> GetConnectionString();
    }
}
