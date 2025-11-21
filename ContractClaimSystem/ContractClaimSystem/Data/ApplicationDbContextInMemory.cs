using Microsoft.EntityFrameworkCore;
using ContractClaimSystem.Models;

namespace ContractClaimSystem.Data
{
    public class ApplicationDbContextInMemory : DbContext
    {
        public ApplicationDbContextInMemory(DbContextOptions<ApplicationDbContextInMemory> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimStatusHistory> ClaimStatusHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Simple configuration for In-Memory database
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Claim>()
                .HasMany(c => c.StatusHistory)
                .WithOne(h => h.Claim)
                .HasForeignKey(h => h.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data for in-memory database
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FirstName = "Simphiwe",
                    LastName = "Mathenjwa",
                    Email = "lecturer@emeris.co.za",
                    Password = "password123",
                    Role = "Lecturer",
                    HourlyRate = 250.00m,
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    UserId = 2,
                    FirstName = "nolan",
                    LastName = "wires",
                    Email = "coordinator@emeris.co.za",
                    Password = "password123",
                    Role = "Coordinator",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    UserId = 3,
                    FirstName = "Cole",
                    LastName = "Palmer",
                    Email = "manager@emeris.co.za",
                    Password = "password123",
                    Role = "Manager",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    UserId = 4,
                    FirstName = "Lionel",
                    LastName = "Messi",
                    Email = "hr@emeris.co.za",
                    Password = "password123",
                    Role = "HR",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}
