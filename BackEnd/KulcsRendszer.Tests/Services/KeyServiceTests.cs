using System;
using System.Linq;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class KeyServiceTests : IAsyncLifetime {
    private KulcsRendszerDbContext _context = null!;

    public async Task InitializeAsync() {
        var options = new DbContextOptionsBuilder<KulcsRendszerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KulcsRendszerDbContext(options);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync() {
        if (_context != null) {
            await _context.DisposeAsync();
        }
    }

    private async Task<ClassRoom> CreateRoomAsync(string id = "A101") {
        var building = new Building {
            BuildingCode = "A",
            BuildingName = "A épület"
        };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor {
            Number = 1,
            BuildingId = building.Id
        };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom {
            Id = id,
            FloorId = floor.Id,
            Name = "Előadó",
            Capacity = 30,
            Equipment = "Projektor"
        };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    private async Task<User> CreateUserAsync(string email = "user@example.com") {
        var user = new User {
            Name = "Teszt Elek",
            Email = email
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<MasterKey> CreateMasterKeyAsync(
        string name = "Főkulcs",
        params ClassRoom[] accessibleRooms) {
        var key = new MasterKey {
            MasterKeyName = name,
            AccessibleRooms = accessibleRooms.ToList()
        };
        _context.MasterKeys.Add(key);
        await _context.SaveChangesAsync();
        return key;
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoKeysExist() {
        var service = new KeyService(_context);

        var result = await service.GetAll();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnStandardAndMasterKeysInIdOrder() {
        var room = await CreateRoomAsync();
        var standardKey = new Key { RoomId = room.Id };
        var masterKey = new MasterKey {
            MasterKeyName = "Főkulcs",
            AccessibleRooms = new List<ClassRoom> { room }
        };
        _context.Keys.AddRange(standardKey, masterKey);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.GetAll();

        Assert.Equal(2, result.Count);
        Assert.Equal(standardKey.Id, result[0].Id);
        Assert.False(result[0].IsMasterKey);
        Assert.Equal(room.Id, result[0].RoomId);
        Assert.True(result[1].IsMasterKey);
        Assert.Equal("Főkulcs", result[1].MasterKeyName);
        Assert.Equal(new[] { room.Id }, result[1].AccessibleRoomIds);
    }

    [Fact]
    public async Task Get_ShouldReturnKeyAndMasterKeyPermissions_WhenKeyExists() {
        var room = await CreateRoomAsync();
        var user = await CreateUserAsync();
        var admin = await CreateUserAsync("admin@example.com");
        var key = await CreateMasterKeyAsync("Főkulcs", room);
        var permission = new MasterKeyPermission {
            KeyId = key.Id,
            UserId = user.Id,
            GrantedByAdminId = admin.Id,
            GrantedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };
        _context.MasterKeyPermissions.Add(permission);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.Get(key.Id);

        Assert.Equal(key.Id, result.Id);
        Assert.True(result.IsMasterKey);
        Assert.Equal("Főkulcs", result.MasterKeyName);
        Assert.Equal(new[] { room.Id }, result.AccessibleRoomIds);
        var resultPermission = Assert.Single(result.MasterKeyPermissions);
        Assert.Equal(user.Id, resultPermission.UserId);
        Assert.Equal(admin.Id, resultPermission.GrantedByAdminId);
        Assert.Equal(permission.GrantedAt, resultPermission.GrantedAt);
    }

    [Fact]
    public async Task Get_ShouldThrowKeysNotFoundException_WhenKeyDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(() => service.Get(99999));
    }

    [Fact]
    public async Task Create_ShouldCreateStandardKeyWithoutRoom() {
        var service = new KeyService(_context);

        var result = await service.Create(new KeyDto());
        var keyInDb = await _context.Keys.FindAsync(result.Id);

        Assert.NotNull(keyInDb);
        Assert.True(result.Id > 0);
        Assert.Null(result.RoomId);
        Assert.False(result.IsMasterKey);
    }

    [Fact]
    public async Task Create_ShouldCreateStandardKeyWithExistingRoom() {
        var room = await CreateRoomAsync();
        var service = new KeyService(_context);

        var result = await service.Create(new KeyDto { RoomId = room.Id });

        Assert.Equal(room.Id, result.RoomId);
        Assert.False(result.IsMasterKey);
    }

    [Fact]
    public async Task Create_ShouldThrowRoomNotFoundException_WhenStandardKeyRoomDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => service.Create(new KeyDto { RoomId = "MISSING" }));

        Assert.Empty(await _context.Keys.ToListAsync());
    }

    [Fact]
    public async Task Create_ShouldCreateMasterKeyWithAccessibleRooms() {
        var firstRoom = await CreateRoomAsync("A101");
        var secondRoom = await CreateRoomAsync("A102");
        var service = new KeyService(_context);
        var dto = new KeyDto {
            IsMasterKey = true,
            MasterKeyName = "Épület főkulcsa",
            AccessibleRoomIds = new List<string> {
                firstRoom.Id,
                secondRoom.Id,
                firstRoom.Id
            }
        };

        var result = await service.Create(dto);
        var keyInDb = await _context.MasterKeys.FindAsync(result.Id);

        Assert.NotNull(keyInDb);
        Assert.True(result.IsMasterKey);
        Assert.Equal(dto.MasterKeyName, result.MasterKeyName);
        Assert.Equal(
            new[] { firstRoom.Id, secondRoom.Id },
            result.AccessibleRoomIds.OrderBy(id => id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Create_ShouldThrowArgumentException_WhenMasterKeyNameIsEmpty(
        string masterKeyName) {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.Create(new KeyDto {
            IsMasterKey = true,
            MasterKeyName = masterKeyName
        }));

        Assert.Empty(await _context.Keys.ToListAsync());
    }

    [Fact]
    public async Task Create_ShouldThrowRoomNotFoundException_WhenAccessibleRoomDoesNotExist() {
        var service = new KeyService(_context);
        var dto = new KeyDto {
            IsMasterKey = true,
            MasterKeyName = "Főkulcs",
            AccessibleRoomIds = new List<string> { "MISSING" }
        };

        await Assert.ThrowsAsync<RoomNotFoundException>(() => service.Create(dto));

        Assert.Empty(await _context.Keys.ToListAsync());
    }

    [Fact]
    public async Task Update_ShouldChangeStandardKeyRoom() {
        var firstRoom = await CreateRoomAsync("A101");
        var secondRoom = await CreateRoomAsync("A102");
        var key = new Key { RoomId = firstRoom.Id };
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.Update(key.Id, new KeyDto {
            RoomId = secondRoom.Id
        });

        Assert.Equal(secondRoom.Id, result.RoomId);
        Assert.False(result.IsMasterKey);
    }

    [Fact]
    public async Task Update_ShouldClearStandardKeyRoom_WhenRoomIdIsNull() {
        var room = await CreateRoomAsync();
        var key = new Key { RoomId = room.Id };
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.Update(key.Id, new KeyDto { RoomId = null });

        Assert.Null(result.RoomId);
    }

    [Fact]
    public async Task Update_ShouldThrowRoomNotFoundException_WhenStandardRoomDoesNotExist() {
        var key = new Key();
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => service.Update(key.Id, new KeyDto { RoomId = "MISSING" }));

        Assert.Null((await _context.Keys.FindAsync(key.Id))!.RoomId);
    }

    [Fact]
    public async Task Update_ShouldUpdateMasterKeyNameAndAccessibleRooms() {
        var firstRoom = await CreateRoomAsync("A101");
        var secondRoom = await CreateRoomAsync("A102");
        var key = await CreateMasterKeyAsync("Régi név", firstRoom);
        var service = new KeyService(_context);

        var result = await service.Update(key.Id, new KeyDto {
            IsMasterKey = true,
            MasterKeyName = "Új név",
            AccessibleRoomIds = new List<string> { secondRoom.Id }
        });

        Assert.True(result.IsMasterKey);
        Assert.Equal("Új név", result.MasterKeyName);
        Assert.Equal(new[] { secondRoom.Id }, result.AccessibleRoomIds);
    }

    [Fact]
    public async Task Update_ShouldAllowMasterKeyWithNoAccessibleRooms() {
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        var result = await service.Update(key.Id, new KeyDto {
            IsMasterKey = true,
            MasterKeyName = "Frissített főkulcs",
            AccessibleRoomIds = new List<string>()
        });

        Assert.Equal("Frissített főkulcs", result.MasterKeyName);
        Assert.Empty(result.AccessibleRoomIds);
    }

    [Fact]
    public async Task Update_ShouldThrowRoomNotFoundException_WhenMasterRoomDoesNotExist() {
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => service.Update(key.Id, new KeyDto {
                IsMasterKey = true,
                MasterKeyName = "Főkulcs",
                AccessibleRoomIds = new List<string> { "MISSING" }
            }));

        Assert.Equal(
            "Főkulcs",
            (await _context.MasterKeys.FindAsync(key.Id))!.MasterKeyName);
    }

    [Fact]
    public async Task Update_ShouldThrowArgumentException_WhenMasterKeyNameIsEmpty() {
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Update(key.Id, new KeyDto {
                IsMasterKey = true,
                MasterKeyName = " "
            }));
    }

    [Fact]
    public async Task Update_ShouldNotConvertStandardKeyToMasterKey() {
        var key = new Key();
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Update(key.Id, new KeyDto {
                IsMasterKey = true,
                MasterKeyName = "Főkulcs"
            }));

        Assert.Null(await _context.MasterKeys.FindAsync(key.Id));
    }

    [Fact]
    public async Task Update_ShouldNotConvertMasterKeyToStandardKey() {
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Update(key.Id, new KeyDto { IsMasterKey = false }));

        Assert.NotNull(await _context.MasterKeys.FindAsync(key.Id));
    }

    [Fact]
    public async Task Update_ShouldThrowKeysNotFoundException_WhenKeyDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.Update(99999, new KeyDto()));
    }

    [Fact]
    public async Task Delete_ShouldRemoveKey_WhenKeyExists() {
        var key = new Key();
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.Delete(key.Id);

        Assert.True(result);
        Assert.Null(await _context.Keys.FindAsync(key.Id));
    }

    [Fact]
    public async Task Delete_ShouldThrowKeysNotFoundException_WhenKeyDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(() => service.Delete(99999));
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldCreatePermission_WhenRequestIsValid() {
        var user = await CreateUserAsync();
        var admin = await CreateUserAsync("admin@example.com");
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        var result = await service.GrantMasterKeyPermission(
            key.Id,
            user.Id,
            admin.Id);
        var permissionInDb = await _context.MasterKeyPermissions
            .SingleOrDefaultAsync(permission =>
                permission.KeyId == key.Id && permission.UserId == user.Id);

        Assert.NotNull(permissionInDb);
        Assert.Equal(key.Id, result.KeyId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(admin.Id, result.GrantedByAdminId);
        Assert.Equal(DateTimeKind.Utc, result.GrantedAt.Kind);
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldThrowKeysNotFoundException_WhenKeyIsNotMasterKey() {
        var user = await CreateUserAsync();
        var admin = await CreateUserAsync("admin@example.com");
        var key = new Key();
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.GrantMasterKeyPermission(key.Id, user.Id, admin.Id));

        Assert.Empty(await _context.MasterKeyPermissions.ToListAsync());
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldThrowKeysNotFoundException_WhenMasterKeyDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.GrantMasterKeyPermission(99999, 1, 2));
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldThrowUserNotFoundException_WhenUserDoesNotExist() {
        var admin = await CreateUserAsync("admin@example.com");
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.GrantMasterKeyPermission(key.Id, 99999, admin.Id));

        Assert.Empty(await _context.MasterKeyPermissions.ToListAsync());
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldThrowUserNotFoundException_WhenAdminDoesNotExist() {
        var user = await CreateUserAsync();
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.GrantMasterKeyPermission(key.Id, user.Id, 99999));

        Assert.Empty(await _context.MasterKeyPermissions.ToListAsync());
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldThrowKeysAlreadyExistsException_WhenPermissionAlreadyExists() {
        var user = await CreateUserAsync();
        var admin = await CreateUserAsync("admin@example.com");
        var key = await CreateMasterKeyAsync();
        _context.MasterKeyPermissions.Add(new MasterKeyPermission {
            KeyId = key.Id,
            UserId = user.Id,
            GrantedByAdminId = admin.Id
        });
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysAlreadyExistsException>(
            () => service.GrantMasterKeyPermission(key.Id, user.Id, admin.Id));

        Assert.Single(await _context.MasterKeyPermissions.ToListAsync());
    }

    [Fact]
    public async Task RevokeMasterKeyPermission_ShouldDeletePermission_WhenPermissionExists() {
        var user = await CreateUserAsync();
        var admin = await CreateUserAsync("admin@example.com");
        var key = await CreateMasterKeyAsync();
        var permission = new MasterKeyPermission {
            KeyId = key.Id,
            UserId = user.Id,
            GrantedByAdminId = admin.Id
        };
        _context.MasterKeyPermissions.Add(permission);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        var result = await service.RevokeMasterKeyPermission(key.Id, user.Id);

        Assert.True(result);
        Assert.Empty(await _context.MasterKeyPermissions.ToListAsync());
    }

    [Fact]
    public async Task RevokeMasterKeyPermission_ShouldThrowKeysNotFoundException_WhenKeyIsNotMasterKey() {
        var user = await CreateUserAsync();
        var key = new Key();
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.RevokeMasterKeyPermission(key.Id, user.Id));
    }

    [Fact]
    public async Task RevokeMasterKeyPermission_ShouldThrowKeysNotFoundException_WhenMasterKeyDoesNotExist() {
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.RevokeMasterKeyPermission(99999, 1));
    }

    [Fact]
    public async Task RevokeMasterKeyPermission_ShouldThrowKeysNotFoundException_WhenUserHasNoPermission() {
        var user = await CreateUserAsync();
        var key = await CreateMasterKeyAsync();
        var service = new KeyService(_context);

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.RevokeMasterKeyPermission(key.Id, user.Id));
    }
}
