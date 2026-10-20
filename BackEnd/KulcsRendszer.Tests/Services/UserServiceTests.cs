using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class UserServiceTests : IAsyncLifetime {
    private KulcsRendszerDbContext _context = null!;

    public async Task InitializeAsync() {
        // Egyedi név minden tesztfuttatáshoz, hogy tiszta adatbázisból induljon
        var options = new DbContextOptionsBuilder<KulcsRendszerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KulcsRendszerDbContext(options);

        // Nem kell már EnsureCreatedAsync vagy külső kapcsolódás, az InMemory azonnal kész van
        await Task.CompletedTask;
    }

    public async Task DisposeAsync() {
        if (_context != null) {
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetInfo_ShouldReturnUser_WhenUserExists() {
        // Arrange
        _context.Users.Add(new User {
            Name = "Kovács Anna",
            Email = "anna@example.com",
            PasswordHash = "dummyhash"
        });
        await _context.SaveChangesAsync();

        var service = new UserService(_context);

        // Act
        var insertedUser = await _context.Users.FirstAsync(u => u.Email == "anna@example.com");
        var result = await service.GetInfo(insertedUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Kovács Anna", result.Name);
        Assert.Equal("anna@example.com", result.Email);
    }

    [Fact]
    public async Task GetInfo_ShouldReturnNull_WhenUserDoesNotExist() {
        // Arrange
        var service = new UserService(_context);

        // Act
        var result = await service.GetInfo(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnTrue_WhenUserIsDeleted() {
        // Arrange
        var user = new User {
            Name = "Törlendő Béla",
            Email = "bela@example.com",
            PasswordHash = "dummyhash"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);

        // Act
        var success = await service.Delete(user.Id);
        var userInDb = await _context.Users.FindAsync(user.Id);

        // Assert
        Assert.True(success);
        Assert.Null(userInDb);
    }
}
