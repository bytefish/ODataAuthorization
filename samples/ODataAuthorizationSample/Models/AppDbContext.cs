using Microsoft.EntityFrameworkCore;

namespace AspNetCore3ODataPermissionsSample.Models
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .OwnsOne(c => c.HomeAddress)
                .WithOwner();

            modelBuilder.Entity<Customer>()
                .OwnsMany(x => x.FavoriteAddresses, cb => cb.HasKey("Id"));
        }
    }
}
