
using Microsoft.EntityFrameworkCore;
using KulcsRendszer.DataContext.Context;

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

            //database service
            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
                options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

            var app = builder.Build();

            app.MapGet("/", () => "A váz sikeresen elindult!");

            app.Run();
        }
    }
}