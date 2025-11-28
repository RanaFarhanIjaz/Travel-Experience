using Microsoft.EntityFrameworkCore;
using TravelShare.Models;

namespace TravelShare.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure any additional model settings if needed
            base.OnModelCreating(modelBuilder);
        }
    }
}