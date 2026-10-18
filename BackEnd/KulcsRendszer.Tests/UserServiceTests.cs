using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Tests {
    public class UserServiceTests {
        [Fact]
        public async Task RegisterAsyncCreatesInstructorAndNormalizesEmail() {
            var service = CreateService(out var context);

            var user = await service.Register(
                new RegisterDto {
                    Name = "Test User",
                    Email = "  USER@Example.COM ",
                    Password = "ValidPassword1!"
                },
                CancellationToken.None);

            Assert.NotNull(user);
            Assert.Equal("user@example.com", user.Email);
            Assert.Null(user.RoleId);
            Assert.True(BCrypt.Net.BCrypt.Verify("ValidPassword1!", user.PasswordHash));
            await context.DisposeAsync();
        }

        [Fact]
        public async Task RegisterAsyncReturnsNullForDuplicateEmail() {
            var service = CreateService(out var context);
            var request = new RegisterDto {
                Name = "Test User",
                Email = "user@example.com",
                Password = "ValidPassword1!"
            };

            await service.Register(request, CancellationToken.None);
            var duplicate = await service.Register(request, CancellationToken.None);

            Assert.Null(duplicate);
            await context.DisposeAsync();
        }

        [Fact]
        public async Task LoginAsyncReturnsUserForNormalizedEmailAndCorrectPassword() {
            var service = CreateService(out var context);
            await service.Register(
                new RegisterDto {
                    Name = "Test User",
                    Email = "user@example.com",
                    Password = "ValidPassword1!"
                },
                CancellationToken.None);

            var user = await service.Login(
                new LoginDto {
                    Email = " USER@EXAMPLE.COM ",
                    Password = "ValidPassword1!"
                },
                CancellationToken.None);

            Assert.NotNull(user);
            Assert.Equal("user@example.com", user.Email);
            await context.DisposeAsync();
        }

        [Fact]
        public async Task LoginAsyncReturnsNullForWrongPassword() {
            var service = CreateService(out var context);
            await service.Register(
                new RegisterDto {
                    Name = "Test User",
                    Email = "user@example.com",
                    Password = "ValidPassword1!"
                },
                CancellationToken.None);

            var user = await service.Login(
                new LoginDto {
                    Email = "user@example.com",
                    Password = "WrongPassword1!"
                },
                CancellationToken.None);

            Assert.Null(user);
            await context.DisposeAsync();
        }

        [Fact]
        public async Task LoginAsyncReturnsNullForUnknownEmail() {
            var service = CreateService(out var context);

            var user = await service.Login(
                new LoginDto {
                    Email = "unknown@example.com",
                    Password = "ValidPassword1!"
                },
                CancellationToken.None);

            Assert.Null(user);
            await context.DisposeAsync();
        }

        private static UserService CreateService(out KulcsRendszerDbContext context) {
            var options = new DbContextOptionsBuilder<KulcsRendszerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new KulcsRendszerDbContext(options);
            context.Roles.Add(new Role { Id = 4, Name = UserRole.Instructor });
            context.SaveChanges();
            return new UserService(context);
        }
    }
}
