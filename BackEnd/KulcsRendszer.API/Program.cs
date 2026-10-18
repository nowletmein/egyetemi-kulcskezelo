using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using KulcsRendszer.DataContext.Context;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using KulcsRendszer.API.Middleware;

namespace KulcsRendszer.API {
    public class Program {
        private const string BuildingMigrationId = "20261018095009_AddBuildingFloorRelationship";
        private const string FloorRoomMigrationId = "20261014121157_AddFloorRoomRelationship";

        public static async Task Main(string[] args) {
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
            builder.Services.AddScoped<IRoomService, RoomService>();

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

            using (var scope = app.Services.CreateAsyncScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<KulcsRendszerDbContext>();
                await BaselineEnsureCreatedDatabaseAsync(dbContext);
                await dbContext.Database.MigrateAsync();
            }

            await app.RunAsync();
        }

        private static async Task BaselineEnsureCreatedDatabaseAsync(KulcsRendszerDbContext dbContext) {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            try {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT CASE
                        WHEN OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL THEN 0
                        ELSE 1
                    END;
                    """;
                var historyTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                var historyIsEmpty = historyTableExists
                    ? "NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory])"
                    : "1 = 1";

                command.CommandText = $"""
                    SELECT CASE
                        WHEN OBJECT_ID(N'[dbo].[ClassRooms]', N'U') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Floors]', N'U') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Roles]', N'U') IS NOT NULL
                            AND COL_LENGTH(N'dbo.ClassRooms', N'Building') IS NOT NULL
                            AND COL_LENGTH(N'dbo.ClassRooms', N'FloorId') IS NOT NULL
                            AND COL_LENGTH(N'dbo.Floors', N'Number') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Buildings]', N'U') IS NULL
                            AND {historyIsEmpty}
                        THEN 1
                        WHEN OBJECT_ID(N'[dbo].[ClassRooms]', N'U') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                            AND OBJECT_ID(N'[dbo].[Roles]', N'U') IS NOT NULL
                            AND COL_LENGTH(N'dbo.ClassRooms', N'Building') IS NOT NULL
                            AND COL_LENGTH(N'dbo.ClassRooms', N'Floor') IS NOT NULL
                            AND COL_LENGTH(N'dbo.ClassRooms', N'FloorId') IS NULL
                            AND OBJECT_ID(N'[dbo].[Floors]', N'U') IS NULL
                            AND OBJECT_ID(N'[dbo].[Buildings]', N'U') IS NULL
                            AND {historyIsEmpty}
                        THEN 2
                        ELSE 0
                    END;
                    """;

                var baselineVersion = Convert.ToInt32(await command.ExecuteScalarAsync());
                if (baselineVersion == 0) {
                    return;
                }

                var baselineMigrationId = baselineVersion == 1
                    ? BuildingMigrationId
                    : FloorRoomMigrationId;
                var migrations = dbContext.Database.GetMigrations().ToArray();
                var baselineMigrationIndex = Array.IndexOf(migrations, baselineMigrationId);
                if (baselineMigrationIndex < 1) {
                    throw new InvalidOperationException(
                        $"Could not find migration '{baselineMigrationId}' to baseline the existing database.");
                }

                await using var transaction = await connection.BeginTransactionAsync();
                command.Transaction = transaction;
                if (!historyTableExists) {
                    command.CommandText = """
                        CREATE TABLE [dbo].[__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                        """;
                    await command.ExecuteNonQueryAsync();
                }

                command.CommandText = """
                    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (@migrationId, @productVersion);
                    """;
                var migrationIdParameter = command.CreateParameter();
                migrationIdParameter.ParameterName = "@migrationId";
                command.Parameters.Add(migrationIdParameter);

                var productVersionParameter = command.CreateParameter();
                productVersionParameter.ParameterName = "@productVersion";
                productVersionParameter.Value = "9.0.20";
                command.Parameters.Add(productVersionParameter);

                for (var index = 0; index < baselineMigrationIndex; index++) {
                    migrationIdParameter.Value = migrations[index];
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            } finally {
                await connection.CloseAsync();
            }
        }
    }
}
