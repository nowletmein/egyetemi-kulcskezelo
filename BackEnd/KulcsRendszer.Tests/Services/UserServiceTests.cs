using Microsoft.EntityFrameworkCore;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Services;
using Xunit;

public class UserServiceTests : IAsyncLifetime {
    private KulcsRendszerDbContext _context;
    

    
    public async Task InitializeAsync() {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=KulcsRendszerDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<KulcsRendszerDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new KulcsRendszerDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
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
        // Ha az Id Identity/AutoIncrement mező, a beszúrás után az EF Core kitölti a generált Id-t
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
