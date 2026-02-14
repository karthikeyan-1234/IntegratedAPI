namespace IntegratedAPI.Tenant_Management
{
    public class TenantConnectionStringProvider : ITenantConnectionStringProvider
    {
        public async Task<string?> GetConnectionStringAsync(string tenantId)
        {
            return await Task.FromResult("Test");
        }
    }
}
