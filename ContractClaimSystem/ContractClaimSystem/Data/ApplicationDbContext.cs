using System.Collections.Generic;
using System.Reflection.Emit;
using ContractClaimSystem.Models;

namespace ContractClaimSystem.Data
{

    // Data/ApplicationDbContext.cs
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimStatusHistory> ClaimStatusHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.HourlyRate).HasColumnType("decimal(18,2)");
            });

            // Configure Claim entity
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasKey(c => c.ClaimId);
                entity.Property(c => c.HoursWorked).HasColumnType("decimal(18,2)");
                entity.Property(c => c.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(c => c.Lecturer)
                      .WithMany()
                      .HasForeignKey(c => c.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ClaimStatusHistory entity
            modelBuilder.Entity<ClaimStatusHistory>(entity =>
            {
                entity.HasKey(h => h.HistoryId);

                entity.HasOne(h => h.Claim)
                      .WithMany(c => c.StatusHistory)
                      .HasForeignKey(h => h.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
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
