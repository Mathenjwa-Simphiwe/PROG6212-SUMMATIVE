using ContractClaimSystem.Data;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace ContractClaimSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // TEST CONNECTION STRINGS FIRST - BEFORE ANY SERVICES
            var connectionStrings = new[]
            {
                "Server=(localdb)\\mssqllocaldb;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
                "Server=localhost;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
                "Server=.\\SQLEXPRESS;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
                "Server=localhost\\SQLEXPRESS;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
                "Server=LAB7L955R\\SQLEXPRESS;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;"
            };

            string workingConnectionString = null;

            foreach (var connString in connectionStrings)
            {
                try
                {
                    // Create a temporary service provider to test the connection
                    var services = new ServiceCollection();
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connString));

                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var canConnect = context.Database.CanConnect();
                    if (canConnect)
                    {
                        workingConnectionString = connString;
                        Console.WriteLine($" Connected using: {connString}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Failed with: {connString}");
                    Console.WriteLine($"   Error: {ex.Message}");
                }
            }

            if (workingConnectionString == null)
            {
                Console.WriteLine(" Could not connect to any SQL Server instance.");
                Console.WriteLine("Using In-Memory database for testing...");

                // Fallback to in-memory database
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("CMCS"));
            }
            else
            {
                // Use the working connection string
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(workingConnectionString));
            }

            // Add services to the container - AFTER testing connection
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "CMCS.Session";
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<SessionService>();

            var app = builder.Build();

            // Database initialization - AFTER building the app
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Ensure database is created
                    context.Database.EnsureCreated();

                    // Seed data if needed
                    if (!context.Users.Any())
                    {
                        context.Users.AddRange(
                            new User { UserId = 1, FirstName = "Simphiwe", LastName = "Mathenjwa", Email = "lecturer@emeris.co.za", Password = "password123", Role = "Lecturer", HourlyRate = 250.00m, CreatedDate = DateTime.Now },
                            new User { UserId = 2, FirstName = "Nolan", LastName = "Wires", Email = "coordinator@emeris.co.za", Password = "password123", Role = "Coordinator", HourlyRate = 0, CreatedDate = DateTime.Now },
                            new User { UserId = 3, FirstName = "Cole", LastName = "Palmer", Email = "manager@emeris.co.za", Password = "password123", Role = "Manager", HourlyRate = 0, CreatedDate = DateTime.Now },
                            new User { UserId = 4, FirstName = "Lionel", LastName = "Messi", Email = "hr@emeris.co.za", Password = "password123", Role = "HR", HourlyRate = 0, CreatedDate = DateTime.Now }
                        );
                        context.SaveChanges();
                        Console.WriteLine("Database seeded with test users!");
                    }

                    Console.WriteLine("Database initialization completed!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                }
            }

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            Console.WriteLine(" Application starting...");
            app.Run();
        }
    }
}
