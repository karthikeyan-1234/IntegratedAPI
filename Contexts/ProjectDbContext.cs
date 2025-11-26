using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Contexts
{
    public class ProjectDbContext: DbContext
    {
        public DbSet<IntegratedAPI.Models.employee> Employees { get; set; }
        public DbSet<IntegratedAPI.Models.product> Products { get; set; }

        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
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


            base.OnModelCreating(modelBuilder);
        }

        
    }
}
