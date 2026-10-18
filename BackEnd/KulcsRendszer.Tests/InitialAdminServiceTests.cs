using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KulcsRendszer.Tests {
    public class InitialAdminServiceTests {
        [Fact]
        public async Task EnsureAdministratorAsyncCreatesAdministratorOnEmptyDatabase() {
            await using var context = CreateContext();
            using var logger = new TestLogger<InitialAdminService>();
            var service = new InitialAdminService(context, logger);

            await service.EnsureAdministratorAsync(
                new InitialAdminSettings {
                    Email = "  ADMIN@Example.COM ",
                    Password = "AdminPassword1!"
                },
                CancellationToken.None);

            var administrator = await context.Users.SingleAsync();
            Assert.Equal("admin@example.com", administrator.Email);
            Assert.Equal(1, administrator.RoleId);
            Assert.True(BCrypt.Net.BCrypt.Verify("AdminPassword1!", administrator.PasswordHash));
        }

        [Fact]
        public async Task EnsureAdministratorAsyncDoesNotCreateAdministratorTwice() {
            await using var context = CreateContext();
            using var logger = new TestLogger<InitialAdminService>();
            var service = new InitialAdminService(context, logger);
            var settings = new InitialAdminSettings {
                Email = "admin@example.com",
                Password = "AdminPassword1!"
            };

            await service.EnsureAdministratorAsync(settings, CancellationToken.None);
            await service.EnsureAdministratorAsync(settings, CancellationToken.None);

            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task EnsureAdministratorAsyncHandlesMissingConfigurationWithoutThrowing() {
            await using var context = CreateContext();
            using var logger = new TestLogger<InitialAdminService>();
            var service = new InitialAdminService(context, logger);

            var exception = await Record.ExceptionAsync(() =>
                service.EnsureAdministratorAsync(new InitialAdminSettings(), CancellationToken.None));

            Assert.Null(exception);
            Assert.Empty(context.Users);
            Assert.Contains(
                "Initial administrator configuration is missing. No administrator was created.",
                logger.Messages);
        }

        [Fact]
        public async Task EnsureAdministratorAsyncDoesNotWritePasswordToLogs() {
            await using var context = CreateContext();
            using var logger = new TestLogger<InitialAdminService>();
            var service = new InitialAdminService(context, logger);
            const string password = "AdminPassword1!";

            await service.EnsureAdministratorAsync(
                new InitialAdminSettings {
                    Email = "admin@example.com",
                    Password = password
                },
                CancellationToken.None);

            Assert.DoesNotContain(logger.Messages, message => message.Contains(password));
        }

        private static KulcsRendszerDbContext CreateContext() {
            var options = new DbContextOptionsBuilder<KulcsRendszerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new KulcsRendszerDbContext(options);
            context.Roles.Add(new Role { Id = 1, Name = UserRole.Administrator });
            context.SaveChanges();
            return context;
        }

        private sealed class TestLogger<T> : ILogger<T>, IDisposable {
            public List<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull {
                return this;
            }

            public bool IsEnabled(LogLevel logLevel) {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) {
                Messages.Add(formatter(state, exception));
            }

            public void Dispose() {
            }
        }
    }
}
