using System;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class RoomServiceTests : IAsyncLifetime {
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

    private async Task<(Building Building, Floor Floor)> CreateFloorAsync(
        string buildingCode = "A",
        string buildingName = "A épület",
        int floorNumber = 1) {
        var building = new Building {
            BuildingCode = buildingCode,
            BuildingName = buildingName
        };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor {
            Number = floorNumber,
            BuildingId = building.Id
        };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        return (building, floor);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoRoomsExist() {
        var service = new RoomService(_context);

        var result = await service.GetAll();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnRoomsSortedByNameWithBuildingName() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.AddRange(
            new ClassRoom { Id = "R2", Name = "Z terem", FloorId = floor.Id },
            new ClassRoom { Id = "R1", Name = "A terem", FloorId = floor.Id });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);

        var result = await service.GetAll();

        Assert.Equal(2, result.Count);
        Assert.Equal("A terem", result[0].Name);
        Assert.Equal("Z terem", result[1].Name);
        Assert.All(result, room => Assert.Equal("A épület", room.Building));
        Assert.Equal(floor.Id, result[0].FloorId);
    }

    [Fact]
    public async Task Get_ShouldReturnRoomWithBuilding_WhenRoomExists() {
        var (_, floor) = await CreateFloorAsync();
        var room = new ClassRoom {
            Id = "A101",
            Name = "Előadó",
            FloorId = floor.Id,
            Capacity = 60,
            Equipment = "Projektor"
        };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);

        var result = await service.Get(room.Id);

        Assert.Equal(room.Id, result.Id);
        Assert.Equal("A épület", result.Building);
        Assert.Equal(floor.Id, result.FloorId);
        Assert.Equal("Előadó", result.Name);
        Assert.Equal(60, result.Capacity);
        Assert.Equal("Projektor", result.Equipment);
    }

    [Fact]
    public async Task Get_ShouldThrowRoomNotFoundException_WhenRoomDoesNotExist() {
        var service = new RoomService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(() => service.Get("missing"));
    }

    [Fact]
    public async Task GetInfo_ShouldReturnRoomInfo_WhenRoomExists() {
        var (_, floor) = await CreateFloorAsync(floorNumber: 0);
        var room = new ClassRoom {
            Id = "A001",
            Name = "Labor",
            FloorId = floor.Id,
            Capacity = 24,
            Equipment = "Számítógép"
        };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);

        var result = await service.GetInfo(room.Id);

        Assert.Equal(room.Id, result.Id);
        Assert.Equal(floor.Id, result.FloorId);
        Assert.Equal("Labor", result.Name);
        Assert.Equal(24, result.Capacity);
        Assert.Equal("Számítógép", result.Equipment);
    }

    [Fact]
    public async Task GetInfo_ShouldThrowRoomNotFoundException_WhenRoomDoesNotExist() {
        var service = new RoomService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(() => service.GetInfo("missing"));
    }

    [Fact]
    public async Task Create_ShouldCreateRoom_WhenDataIsValid() {
        var (_, floor) = await CreateFloorAsync();
        var dto = new RoomCreateDto {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Szemináriumi terem",
            Capacity = 30,
            Equipment = "Tábla"
        };
        var service = new RoomService(_context);

        var result = await service.Create(dto);
        var roomInDb = await _context.ClassRooms.FindAsync(dto.Id);

        Assert.NotNull(roomInDb);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal("A épület", result.Building);
        Assert.Equal(floor.Id, result.FloorId);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Capacity, result.Capacity);
        Assert.Equal(dto.Equipment, result.Equipment);
    }

    [Fact]
    public async Task Create_ShouldThrowFloorNotFoundException_WhenFloorDoesNotExist() {
        var service = new RoomService(_context);
        var dto = new RoomCreateDto {
            Id = "X101",
            FloorId = 99999,
            Name = "Nem létező terem"
        };

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.Create(dto));
    }

    [Fact]
    public async Task Create_ShouldThrowRoomAlreadyExistsException_WhenIdAlreadyExists() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Első terem"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomCreateDto {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Másik terem"
        };

        await Assert.ThrowsAsync<RoomAlreadyExistsException>(() => service.Create(dto));
    }

    [Fact]
    public async Task Create_ShouldThrowRoomAlreadyExistsException_WhenNameAlreadyExistsOnFloor() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Közös név"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomCreateDto {
            Id = "A102",
            FloorId = floor.Id,
            Name = "Közös név"
        };

        await Assert.ThrowsAsync<RoomAlreadyExistsException>(() => service.Create(dto));
    }

    [Fact]
    public async Task Create_ShouldAllowSameNameOnDifferentFloors() {
        var (building, firstFloor) = await CreateFloorAsync();
        var secondFloor = new Floor { Number = 2, BuildingId = building.Id };
        _context.Floors.Add(secondFloor);
        await _context.SaveChangesAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = firstFloor.Id,
            Name = "Labor"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomCreateDto {
            Id = "A201",
            FloorId = secondFloor.Id,
            Name = "Labor"
        };

        var result = await service.Create(dto);

        Assert.Equal("A201", result.Id);
        Assert.Equal(secondFloor.Id, result.FloorId);
    }

    [Fact]
    public async Task Update_ShouldUpdateRoomFields_WhenDataIsValid() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Régi név",
            Capacity = 10,
            Equipment = "Régi felszerelés"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomUpdateDto {
            FloorId = floor.Id,
            Name = "Új név",
            Capacity = 20,
            Equipment = "Projektor"
        };

        var result = await service.Update("A101", dto);

        Assert.Equal("A101", result.Id);
        Assert.Equal("A épület", result.Building);
        Assert.Equal("Új név", result.Name);
        Assert.Equal(20, result.Capacity);
        Assert.Equal("Projektor", result.Equipment);
    }

    [Fact]
    public async Task Update_ShouldThrowRoomNotFoundException_WhenRoomDoesNotExist() {
        var service = new RoomService(_context);
        var dto = new RoomUpdateDto { Name = "Nem létező terem" };

        await Assert.ThrowsAsync<RoomNotFoundException>(() => service.Update("missing", dto));
    }

    [Fact]
    public async Task Update_ShouldThrowFloorNotFoundException_WhenTargetFloorDoesNotExist() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Terem"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomUpdateDto { FloorId = 99999, Name = "Terem" };

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.Update("A101", dto));
    }

    [Fact]
    public async Task Update_ShouldThrowRoomAlreadyExistsException_WhenNameConflictsOnFloor() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.AddRange(
            new ClassRoom { Id = "A101", FloorId = floor.Id, Name = "Foglalt név" },
            new ClassRoom { Id = "A102", FloorId = floor.Id, Name = "Másik név" });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomUpdateDto {
            FloorId = floor.Id,
            Name = "Foglalt név",
            Capacity = 15
        };

        await Assert.ThrowsAsync<RoomAlreadyExistsException>(() => service.Update("A102", dto));
    }

    [Fact]
    public async Task Update_ShouldReturnNewBuilding_WhenRoomMovesToAnotherFloor() {
        var (_, firstFloor) = await CreateFloorAsync();
        var secondBuilding = new Building {
            BuildingCode = "B",
            BuildingName = "B épület"
        };
        _context.Buildings.Add(secondBuilding);
        await _context.SaveChangesAsync();
        var secondFloor = new Floor { Number = 1, BuildingId = secondBuilding.Id };
        _context.Floors.Add(secondFloor);
        await _context.SaveChangesAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = firstFloor.Id,
            Name = "Áthelyezett terem"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);
        var dto = new RoomUpdateDto {
            FloorId = secondFloor.Id,
            Name = "Áthelyezett terem",
            Capacity = 25,
            Equipment = "Tábla"
        };

        var result = await service.Update("A101", dto);

        Assert.Equal(secondFloor.Id, result.FloorId);
        Assert.Equal("B épület", result.Building);
        Assert.Equal("Áthelyezett terem", result.Name);
    }

    [Fact]
    public async Task Delete_ShouldRemoveRoom_WhenRoomExists() {
        var (_, floor) = await CreateFloorAsync();
        _context.ClassRooms.Add(new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Törlendő terem"
        });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);

        var result = await service.Delete("A101");
        var roomInDb = await _context.ClassRooms.FindAsync("A101");

        Assert.True(result);
        Assert.Null(roomInDb);
    }

    [Fact]
    public async Task Delete_ShouldThrowRoomNotFoundException_WhenRoomDoesNotExist() {
        var service = new RoomService(_context);

        await Assert.ThrowsAsync<RoomNotFoundException>(() => service.Delete("missing"));
    }

    [Fact]
    public async Task GetByFloorAsync_ShouldReturnNull_WhenFloorDoesNotExist() {
        var service = new RoomService(_context);

        var result = await service.GetByFloorAsync(99999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByFloorAsync_ShouldReturnEmptyList_WhenFloorHasNoRooms() {
        var (_, floor) = await CreateFloorAsync();
        var service = new RoomService(_context);

        var result = await service.GetByFloorAsync(floor.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByFloorAsync_ShouldReturnOnlyFloorRoomsSortedByName() {
        var (building, firstFloor) = await CreateFloorAsync();
        var secondFloor = new Floor { Number = 2, BuildingId = building.Id };
        _context.Floors.Add(secondFloor);
        await _context.SaveChangesAsync();
        _context.ClassRooms.AddRange(
            new ClassRoom { Id = "A102", FloorId = firstFloor.Id, Name = "Z terem" },
            new ClassRoom { Id = "A101", FloorId = firstFloor.Id, Name = "A terem" },
            new ClassRoom { Id = "A201", FloorId = secondFloor.Id, Name = "Másik emelet" });
        await _context.SaveChangesAsync();
        var service = new RoomService(_context);

        var result = await service.GetByFloorAsync(firstFloor.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("A terem", result[0].Name);
        Assert.Equal("Z terem", result[1].Name);
        Assert.All(result, room => Assert.Equal(firstFloor.Id, room.FloorId));
        Assert.All(result, room => Assert.Equal("A épület", room.Building));
    }
}
