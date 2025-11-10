using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Contexts
{
    public class ProjectDbContext: DbContext
    {
        public DbSet<IntegratedAPI.Models.employee> Employees { get; set; }

        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntegratedAPI.Models.employee>(e =>
            {
                e.ToTable("employee");
            });

            modelBuilder.Entity<IntegratedAPI.Models.employee>(entity =>
            {
                entity.HasKey(e => e.Id);
             
            });


            base.OnModelCreating(modelBuilder);
        }

        
    }
}
