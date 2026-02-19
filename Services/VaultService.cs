using Microsoft.AspNetCore.Mvc;

using VaultSharp;

namespace IntegratedAPI.Services
{
    public class VaultService : IVaultService
    {
        private readonly IVaultClient _vaultClient;
        private readonly ILogger<VaultService> _logger;

        public VaultService(IVaultClient vaultClient, ILogger<VaultService> logger)
        {
            _vaultClient = vaultClient;
            _logger = logger;
        }

        public async Task<string?> GetConnectionString(string tenant)
        {
            try
            {
                // KV-v2: path is tenant name, mountPoint is "tenants"
                var secret = await _vaultClient.V1.Secrets.KeyValue.V2
                    .ReadSecretAsync(path: tenant.ToUpper(), mountPoint: "tenants");

                if (secret.Data.Data.TryGetValue("connectionString", out var connStringObj))
                {
                    return connStringObj.ToString()!;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return null;
            }
        }


    }
}
