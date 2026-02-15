using IntegratedAPI.Tenant_Management;

using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Contexts
{
    public class ProjectDbContext: DbContext
    {
        private readonly ITenantConnectionStringProvider? _connectionStringProvider;

        public DbSet<IntegratedAPI.Models.employee> Employees { get; set; }
        public DbSet<IntegratedAPI.Models.product> Products { get; set; }
        public DbSet<IntegratedAPI.Models.cartItem> CartItems { get; set; }

        public ProjectDbContext(DbContextOptions<ProjectDbContext> options, ITenantConnectionStringProvider tenantConnectionStringProvider) : base(options)
        {
            _connectionStringProvider = tenantConnectionStringProvider;
        }

        protected override async void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_connectionStringProvider != null)
            {
                var connectionString = await _connectionStringProvider.GetConnectionString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntegratedAPI.Models.employee>(e =>
            {
                e.ToTable("employee");
            });

            modelBuilder.Entity<IntegratedAPI.Models.product>(e =>
            {
                e.ToTable("product");
            });

            modelBuilder.Entity<IntegratedAPI.Models.cartItem>(e =>
            {
                e.ToTable("cartItem");
            });

            modelBuilder.Entity<IntegratedAPI.Models.employee>(entity =>
            {
                entity.HasKey(e => e.Id);
             
            });

            modelBuilder.Entity<IntegratedAPI.Models.product>(entity =>
            {
                entity.HasKey(e => e.id);
                entity.Property(e => e.name).IsRequired();
                entity.Property(e => e.id).UseIdentityColumn();

            });

            modelBuilder.Entity<IntegratedAPI.Models.cartItem>(entity =>
            {
                entity.ToTable("cartItem");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).UseIdentityColumn();
                entity.Property(e => e.product_id)
                      .HasColumnName("product_id") // Explicit column name
                      .IsRequired();

                // Explicitly configure the relationship
                entity.HasOne(ci => ci.Product)
                      .WithMany()
                      .HasForeignKey(ci => ci.product_id)
                      .HasConstraintName("FK_cartItem_product_product_id")
                      .OnDelete(DeleteBehavior.Cascade);
            });


            base.OnModelCreating(modelBuilder);
        }

        
    }
}
