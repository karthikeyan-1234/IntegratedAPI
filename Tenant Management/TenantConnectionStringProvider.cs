using IntegratedAPI.DTOs;
using IntegratedAPI.Services;

using System.Text.Json;

namespace IntegratedAPI.Tenant_Management
{

    public class TenantConnectionStringProvider : ITenantConnectionStringProvider
    {
        private readonly HttpContext _httpContext;
        private readonly IVaultService _vaultService;
        private readonly ILogger<TenantConnectionStringProvider> _logger;

        public TenantConnectionStringProvider(
            IHttpContextAccessor httpContextAccessor,
            IVaultService vaultService,
            ILogger<TenantConnectionStringProvider> logger)
        {
            _httpContext = httpContextAccessor.HttpContext!;
            _vaultService = vaultService;
            _logger = logger;
        }

        public async Task<string> GetConnectionString()
        {
            var tenantId = _httpContext?.Items["tenant"]!.ToString();
            if (!string.IsNullOrEmpty(tenantId))
            {
                var connObj = await _vaultService.GetConnectionString(tenantId);

                var connectionString = connObj;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }
            }

            _logger.LogWarning("No tenant connection string available");
            return string.Empty;
        }
    }
}