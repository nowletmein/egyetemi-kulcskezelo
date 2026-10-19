<<<<<<< HEAD
<<<<<<< HEAD
﻿using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using KulcsRendszer.Services.Services;
using KulcsRendszer.API.Middleware;
=======
﻿using DotNetEnv;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
>>>>>>> a50f011 (Fix: Fixed connection string in program.cs and in unit tests, fixed bug with not being able to get data from .env)

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

            //controllers
            builder.Services.AddScoped<IUserService, UserService>();
<<<<<<< HEAD
            builder.Services.AddScoped<IRoomService, RoomService>();
=======

>>>>>>> a50f011 (Fix: Fixed connection string in program.cs and in unit tests, fixed bug with not being able to get data from .env)
            //database service
            //builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            //options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

<<<<<<< HEAD
            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=db;Database=KulcsRendszerDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"));

=======
            var projectRoot = Path.Combine(AppContext.BaseDirectory, "../../../../../");
            var envPath = Path.GetFullPath(Path.Combine(projectRoot, ".env"));

            // Betöltjük az explicit útvonalon lévő fájlt
            Env.Load(envPath);
            Console.WriteLine(Env.Load(envPath));

            // Most már kiolvashatod a változót:
            var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");

            var connectionString = $"Server=localhost,1433;Database=KulcsRendszerDB;User Id=sa;Password={password};TrustServerCertificate=True;";
            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
                options.UseSqlServer(connectionString));
>>>>>>> a50f011 (Fix: Fixed connection string in program.cs and in unit tests, fixed bug with not being able to get data from .env)

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
                    dbContext.Database.Migrate();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Adatbázis migrácó hiba: {ex.Message}");
                }
            }

            app.Run();
        }
    }
}
<<<<<<< HEAD
=======
﻿using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
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

            //controllers
            builder.Services.AddScoped<IUserService, UserService>();

            //database service
            //builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            //options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

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
                    dbContext.Database.Migrate();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Adatbázis migrácó hiba: {ex.Message}");
                }
            }

            app.Run();
        }
    }
}
>>>>>>> 30ea9b9 (Force fix line endings for CI)
=======
>>>>>>> a50f011 (Fix: Fixed connection string in program.cs and in unit tests, fixed bug with not being able to get data from .env)
