
using Microsoft.EntityFrameworkCore;
using KulcsRendszer.DataContext.Context;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KulcsRendszer.API {
    public class Program {
        public static void Main(string[] args) {
            
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options => {
                options.AddPolicy("ReactPolicy", policy => {
                    policy.WithOrigins("http://localhost:3000") // React app origin
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            //swagger gen
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //database service
            //builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            // options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=db;Database=KulcsRendszerDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"));
            //healthchek for database
            builder.Services.AddHealthChecks()
            .AddDbContextCheck<KulcsRendszerDbContext>();


            var app = builder.Build();
            
            // After app building:
            // Enable middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();
            app.MapHealthChecks("/api/health");
            app.MapGet("/", () => "A váz sikeresen elindult!");

            using (var scope = app.Services.CreateScope()) {
                try {
                    var dbContext = scope.ServiceProvider.GetRequiredService<KulcsRendszerDbContext>();
                    dbContext.Database.EnsureCreated(); // vagy dbContext.Database.Migrate();
                } catch (Exception) {
                    Console.WriteLine("Adatbázis már létezik");
                }
            //}
            app.Run();
        }
    }
}
