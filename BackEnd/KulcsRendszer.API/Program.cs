using System.Text;
using KulcsRendszer.API.Middleware;
using KulcsRendszer.API.Services;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace KulcsRendszer.API {
    public class Program {

        public static async Task Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options => {
                options.AddPolicy("ReactPolicy", policy => {
                    policy.WithOrigins("http://localhost:3000") // React app origin
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "http://localhost:5000",
                    ValidAudience = "http://localhost:3000",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecretKeyThatIsAtLeast32CharactersLong!!ADDSFHsfgsdgksgkaowaefagnamfaklfaijg")),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents {
                    OnAuthenticationFailed = context => {
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

            //swagger gen
            builder.Services.AddControllers();
            builder.Services.AddSingleton<AuditLogStore>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            //controllers
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IBuildingService, BuildingService>();
            builder.Services.AddScoped<IFloorService, FloorService>();
            builder.Services.AddScoped<IKeyService, KeyService>();
            //database service
            //builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            //options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=database,1433;Database=KulcsRendszerDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true",
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));

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

            app.UseCors("ReactPolicy");

            app.UseMiddleware<ApiExceptionHandlingMiddleware>();

            app.UseAuthentication();
            app.UseMiddleware<AuditLogMiddleware>();
            app.UseAuthorization();

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
            using (var scope = app.Services.CreateScope()) {
                try {
                    if (app.Environment.IsDevelopment()) {
                        await DbInitializer.SeedAsync(scope.ServiceProvider, builder.Configuration);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Adatbázis migrácó hiba: {ex.Message}");
                }
            }
            app.Run();
        }
    }
}
