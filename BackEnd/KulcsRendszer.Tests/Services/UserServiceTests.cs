using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Exceptions;
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


    //userhez kapcsolódó tesztek
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

    [Fact]
    public async Task Delete_ShouldThrowUserNotFoundException_WhenUserDoesNotExist() {
        var service = new UserService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.Delete(99999));
    }

    [Fact]
    public async Task Create_ShouldThrowException_WhenUserAlreadyExists() {
        var existingUser = new User {
            Name = "Kiko Miko",
            Email = "kiko@example.com",
            PasswordHash = "dummyhash"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);

        var newUserDto = new UserCreateDto {
            Name = "Miko Kiko",
            Email = "kiko@example.com",
            Password = "newpassword"
        };

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.Create(newUserDto));
    }

    [Fact]
    public async Task Create_ShouldCreateUserSuccessfully_WhenDataIsValid() {
        var service = new UserService(_context);
        var createDto = new UserCreateDto {
            Name = "Major Anna",
            Email = "annumamu@example.com",
            Password = "Password123!"
        };

        var result = await service.Create(createDto);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Major Anna", result.Name);
        Assert.Equal("annumamu@example.com", result.Email);
    }

    [Fact]
    public async Task Get_ShouldReturnUserWithMappedRelations_WhenUserExists() {
        var role = new Role { Name = UserRole.Admin };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var user = new User {
            Name = "Nagy Péter",
            Email = "peter@example.com",
            PasswordHash = "hash",
            RoleId = role.Id
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);

        var result = await service.Get(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Nagy Péter", result.Name);
        Assert.Equal("peter@example.com", result.Email);
        Assert.NotNull(result.Bookings);
    }

    [Fact]
    public async Task Delete_ShouldRemoveUserAndKeepOtherDataIntact_SideEffectCheck() {
        var user1 = new User { Name = "User 1", Email = "u1@example.com", PasswordHash = "hash" };
        var user2 = new User { Name = "User 2", Email = "u2@example.com", PasswordHash = "hash" };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);

        await service.Delete(user1.Id);

        Assert.Null(await _context.Users.FindAsync(user1.Id));
        Assert.NotNull(await _context.Users.FindAsync(user2.Id));
    }


    //jelszóhoz kapcsolódó tesztek
    [Fact]
    public async Task ChangePassword_ShouldUpdateHashSuccessfully_WhenValidOldPasswordGiven() {
        var user = new User {
            Name = "Jelszó Teszt",
            Email = "jelszo@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);
        var dto = new ChangePasswordDto {
            OldPassword = "OldPass123",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var result = await service.ChangePassword(user.Id, dto);
        var userInDb = await _context.Users.FindAsync(user.Id);

        Assert.True(result);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass123", userInDb!.PasswordHash));
    }

    [Fact]
    public async Task Update_ShouldNotUpdatePassword_WhenPasswordIsEmpty() {
        var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123");
        var user = new User {
            Name = "Régi Név",
            Email = "regi@example.com",
            PasswordHash = oldHash
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);
        var updateDto = new UserUpdateDto {
            Name = "Frissített Név",
            Email = "frissitett@example.com",
            Password = ""
        };

        var updatedUser = await service.Update(user.Id, updateDto);
        var userInDb = await _context.Users.FindAsync(user.Id);

        Assert.NotNull(updatedUser);
        Assert.Equal("Frissített Név", updatedUser.Name);
        Assert.Equal(oldHash, userInDb!.PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_ShouldThrowArgumentException_WhenNewPasswordAndConfirmPasswordDoNotMatch() {
        var service = new UserService(_context);
        var dto = new ChangePasswordDto {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmPassword = "DifferentPassword123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePassword(1, dto));
    }

    [Fact]
    public async Task ChangePassword_ShouldThrowUserNotFoundException_WhenUserDoesNotExist() {
        var service = new UserService(_context);
        var dto = new ChangePasswordDto {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.ChangePassword(99999, dto));
    }

    [Fact]
    public async Task ChangePassword_ShouldThrowUnauthorizedAccessException_WhenOldPasswordIsIncorrect() {
        var user = new User {
            Name = "Jelszó Teszt",
            Email = "password@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOldPassword")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);
        var dto = new ChangePasswordDto {
            OldPassword = "WrongOldPassword",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ChangePassword(user.Id, dto));
    }

    [Fact]
    public async Task Create_ShouldHashPassword_AndNotSavePlainTextPassword() {
        var service = new UserService(_context);
        var plainPassword = "MySecretPassword123";
        var createDto = new UserCreateDto {
            Name = "Hash Teszt",
            Email = "hash@example.com",
            Password = plainPassword
        };

        var created = await service.Create(createDto);
        var userInDb = await _context.Users.FindAsync(created.Id);

        Assert.NotNull(userInDb);
        Assert.NotEqual(plainPassword, userInDb.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, userInDb.PasswordHash));
    }

    //rolehoz kapcsolódó tesztek
    [Fact]
    public async Task CreateRole_ShouldThrowRoleAlreadyExistsException_WhenRoleNameExists() {
        _context.Roles.Add(new Role { Name = UserRole.Admin });
        await _context.SaveChangesAsync();

        var service = new UserService(_context);
        var roleDto = new RoleCreateDto { Name = UserRole.Admin };

        await Assert.ThrowsAsync<RoleAlreadyExistsException>(() => service.CreateRole(roleDto));
    }

    [Fact]
    public async Task GetRole_ShouldThrowRoleNotFoundException_WhenRoleDoesNotExist() {
        var service = new UserService(_context);

        await Assert.ThrowsAsync<RoleNotFoundException>(() => service.GetRole(99999));
    }

    //performance tesztek
    [Fact]
    public async Task Get_ShouldRespondWithinAcceptableTimeLimit_PerformanceCheck() {
        var user = new User { Name = "Perf Felhasználó", Email = "perfhasznalo@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var service = new UserService(_context);
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        var result = await service.Get(user.Id);
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 200, $"Futási idő túl hosszú volt: {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Create_ShouldHandleMultipleSequentialRequests_StressCheck() {
        var service = new UserService(_context);
        int requestCount = 30;

        for (int i = 0; i < requestCount; i++) {
            await service.Create(new UserCreateDto {
                Name = $"StressUser_{i}",
                Email = $"stress_{i}@example.com",
                Password = "Password123"
            });
        }

        var totalUsers = await _context.Users.CountAsync();
        Assert.Equal(requestCount, totalUsers);
    }
}
