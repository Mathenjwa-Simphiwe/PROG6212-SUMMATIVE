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
        private readonly ILogger<ApplicationDbContext> _logger;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger = null) : base(options)
        {
            _logger = logger;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimStatusHistory> ClaimStatusHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=LAB7L955R\\SQLEXPRESS;Database=CMCS;Trusted_Connection=true;TrustServerCertificate=true;");
            }

            // Suppress the pending model changes warning
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

         protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Your existing model configuration...
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            });

            // Configure Claim entity
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasKey(c => c.ClaimId);
                entity.Property(c => c.HoursWorked).HasColumnType("decimal(18,2)");
                entity.Property(c => c.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(c => c.Status).IsRequired().HasMaxLength(20);
                entity.Property(c => c.FileName).HasMaxLength(255);
                entity.Property(c => c.ContentType).HasMaxLength(100);

                entity.HasOne(c => c.Lecturer)
                      .WithMany()
                      .HasForeignKey(c => c.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasCheckConstraint("CK_Claim_Month", "[Month] BETWEEN 1 AND 12");
                entity.HasCheckConstraint("CK_Claim_HoursWorked", "[HoursWorked] >= 0");
            });

            // Configure ClaimStatusHistory entity
            modelBuilder.Entity<ClaimStatusHistory>(entity =>
            {
                entity.HasKey(h => h.HistoryId);
                entity.Property(h => h.Status).IsRequired().HasMaxLength(50);
                entity.Property(h => h.ActionBy).IsRequired().HasMaxLength(100);

                entity.HasOne(h => h.Claim)
                      .WithMany(c => c.StatusHistory)
                      .HasForeignKey(h => h.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Remove the SeedData method from here if you're seeding in Program.cs
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving changes to database");
                throw;
            }
        }
    }
}


