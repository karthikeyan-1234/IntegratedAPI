using IntegratedAPI.Tenant_Management;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IntegratedAPI.HealthChecks
{
    public class VaultSqlServerHealthCheck : IHealthCheck
    {
        private readonly ITenantConnectionStringProvider _tenantProvider;
        private readonly ILogger<VaultSqlServerHealthCheck> _logger;

        public VaultSqlServerHealthCheck(
            ITenantConnectionStringProvider tenantProvider,
            ILogger<VaultSqlServerHealthCheck> logger)
        {
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var connectionString = await _tenantProvider.GetConnectionString();

                if (string.IsNullOrEmpty(connectionString))
                    return HealthCheckResult.Unhealthy("Could not retrieve connection string from Vault.");

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy("SQL Server reachable via Vault connection string.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL health check failed");
                return HealthCheckResult.Unhealthy("SQL Server unreachable.", ex);
            }
        }
    }
}
