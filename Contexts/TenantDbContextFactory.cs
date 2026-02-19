using IntegratedAPI.Tenant_Management;

using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Contexts
{
    // Contexts/TenantDbContextFactory.cs
    public class TenantDbContextFactory
    {
        private readonly ITenantConnectionStringProvider _tenantProvider;
        private readonly ILoggerFactory _loggerFactory;

        public TenantDbContextFactory(
            ITenantConnectionStringProvider tenantProvider,
            ILoggerFactory loggerFactory)
        {
            _tenantProvider = tenantProvider;
            _loggerFactory = loggerFactory;
        }

        public async Task<ProjectDbContext> CreateAsync()
        {
            var connectionString = await _tenantProvider.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Could not resolve tenant connection string from Vault.");

            var optionsBuilder = new DbContextOptionsBuilder<ProjectDbContext>();
            optionsBuilder
                .UseSqlServer(connectionString)
                .UseLoggerFactory(_loggerFactory);

            return new ProjectDbContext(optionsBuilder.Options);
        }
    }
}
