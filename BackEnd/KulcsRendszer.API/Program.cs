using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using KulcsRendszer.API.Middleware;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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

            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32) {
                throw new InvalidOperationException("JWT signing key must be configured through Jwt__Key and be at least 32 characters long.");
            }

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options => {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter a valid JWT bearer token."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.Configure<JwtSettings>(options => {
                options.Key = jwtSettings.Key;
                options.Issuer = jwtSettings.Issuer;
                options.Audience = jwtSettings.Audience;
                options.ExpirationMinutes = jwtSettings.ExpirationMinutes;
            });
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IAuthAuditService, AuthAuditService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(options => {
                options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
                options.OnRejected = async (context, cancellationToken) => {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Too many authentication requests were made. Please try again later.",
                        Instance = context.HttpContext.Request.Path
                    }, cancellationToken);
                };
            });

            //controllers
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IInitialAdminService, InitialAdminService>();
            builder.Services.Configure<InitialAdminSettings>(
                builder.Configuration.GetSection("InitialAdmin"));

            //database service
            //builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
            //options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=KulcsRendszerDB;Trusted_Connection=True;TrustServerCertificate=true"));

            builder.Services.AddDbContext<KulcsRendszerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=db;Database=KulcsRendszerDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"));
            builder.Services.AddScoped<IRoomService, RoomService>();

            //healthchek for database
            builder.Services.AddHealthChecks()
            .AddDbContextCheck<KulcsRendszerDbContext>();


            var app = builder.Build();
            app.UseMiddleware<ApiExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment()) {
                app.UseHttpsRedirection();
            }

            app.UseCors("ReactPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/api/health");
            app.MapGet("/", () => "The application started successfully.");

            const int maxInitializationAttempts = 10;
            var databaseInitialized = false;
            Exception? lastInitializationException = null;

            for (var attempt = 1; attempt <= maxInitializationAttempts; attempt++) {
                try {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<KulcsRendszerDbContext>();
                    dbContext.Database.Migrate();
                    var initialAdminService = scope.ServiceProvider.GetRequiredService<IInitialAdminService>();
                    var initialAdminSettings = builder.Configuration
                        .GetSection("InitialAdmin")
                        .Get<InitialAdminSettings>() ?? new InitialAdminSettings();
                    initialAdminService
                        .EnsureAdministratorAsync(initialAdminSettings, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                    databaseInitialized = true;
                    break;
                }
                catch (Exception exception) {
                    lastInitializationException = exception;
                    using var loggingScope = app.Services.CreateScope();
                    var logger = loggingScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    if (attempt == maxInitializationAttempts) {
                        logger.LogError(
                            exception,
                            "The database could not be initialized after {MaxAttempts} attempts.",
                            maxInitializationAttempts);
                    }
                    else {
                        logger.LogWarning(
                            exception,
                            "Database initialization attempt {Attempt} of {MaxAttempts} failed.",
                            attempt,
                            maxInitializationAttempts);
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                }
            }

            if (!databaseInitialized) {
                throw new InvalidOperationException(
                    "The database could not be initialized. The application will stop.",
                    lastInitializationException);
            }

            app.Run();
        }
    }
}
