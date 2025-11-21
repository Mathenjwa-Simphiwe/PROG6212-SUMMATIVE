using ContractClaimSystem.Data;
using ContractClaimSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace ContractClaimSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register both database contexts
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                // This will only be used if SQL Server connection works
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CMCS;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;");
            });

            // Always register the in-memory context as fallback
            builder.Services.AddDbContext<ApplicationDbContextInMemory>(options =>
                options.UseInMemoryDatabase("CMCS-InMemory"));

            // Add services to the container
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

            builder.Services.AddScoped<IDatabaseService, DatabaseService>();

            var app = builder.Build();

            // Database initialization
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                // Always initialize in-memory database first (guaranteed to work)
                try
                {
                    var memoryContext = services.GetRequiredService<ApplicationDbContextInMemory>();
                    memoryContext.Database.EnsureCreated();

                    // Check if we need to seed data
                    if (!memoryContext.Users.Any())
                    {
                        // Data is already seeded via HasData in OnModelCreating
                        Console.WriteLine("In-Memory database initialized with seeded data!");
                    }
                    else
                    {
                        Console.WriteLine(" In-Memory database already contains data!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" In-Memory database error: {ex.Message}");
                }

                // Try to initialize SQL Server database (optional)
                try
                {
                    var sqlContext = services.GetRequiredService<ApplicationDbContext>();
                    var canConnect = sqlContext.Database.CanConnect();
                    if (canConnect)
                    {
                        Console.WriteLine("SQL Server database connected and ready!");
                        sqlContext.Database.EnsureCreated();
                    }
                    else
                    {
                        Console.WriteLine("SQL Server not available - using In-Memory database only");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SQL Server not available: {ex.Message}");
                    Console.WriteLine("Using In-Memory database as primary data store");
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

            Console.WriteLine(" Application started successfully!");
            Console.WriteLine("Primary Data Store: In-Memory Database");
            app.Run();
        }
    }
}
