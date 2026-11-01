using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class FloorServiceTests : IAsyncLifetime {
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


    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoFloorsExist() {
        var service = new FloorService(_context);

        var result = await service.GetAll();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllFloorsWithBuildingAndRooms() {
        var building = new Building { BuildingCode = "I", BuildingName = "Műszaki Informatika Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 2, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom { Id = "1", Name = "I104", FloorId = floor.Id, Capacity = 40, Equipment = "Projektor, Tábla" };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);

        var result = await service.GetAll();

        Assert.Single(result);
        Assert.Equal(2, result[0].Number);
        Assert.Equal(building.Id, result[0].BuildingId);
        Assert.Single(result[0].Rooms);
    }

    [Fact]
    public async Task Get_ShouldReturnFloor_WhenFloorExists() {
        var building = new Building { BuildingCode = "A", BuildingName = "Gazdaságtudományi Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);

        var result = await service.Get(floor.Id);

        Assert.NotNull(result);
        Assert.Equal(floor.Id, result.Id);
        Assert.Equal(1, result.Number);
    }

    [Fact]
    public async Task Get_ShouldThrowFloorNotFoundException_WhenFloorDoesNotExist() {
        var service = new FloorService(_context);

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.Get(99999));
    }

    [Fact]
    public async Task GetInfo_ShouldReturnFloorInfo_WhenFloorExists() {
        var building = new Building { BuildingCode = "C", BuildingName = "Mérnöki Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 3, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);

        var result = await service.GetInfo(floor.Id);

        Assert.NotNull(result);
        Assert.Equal(floor.Id, result.Id);
        Assert.Equal(3, result.Number);
        Assert.Equal(building.Id, result.BuildingId);
    }

    [Fact]
    public async Task GetInfo_ShouldThrowFloorNotFoundException_WhenFloorDoesNotExist() {
        var service = new FloorService(_context);

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.GetInfo(99999));
    }


    [Fact]
    public async Task Create_ShouldCreateFloorSuccessfully_WhenDataIsValid() {
        var building = new Building { BuildingCode = "P", BuildingName = "Polinszky Terem" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);
        var dto = new FloorCreateDto { Number = 1, BuildingId = building.Id };

        var result = await service.Create(dto);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(1, result.Number);
        Assert.Equal(building.Id, result.BuildingId);
    }

    [Fact]
    public async Task Create_ShouldThrowBuildingNotFoundException_WhenBuildingDoesNotExist() {
        var service = new FloorService(_context);
        var dto = new FloorCreateDto { Number = 1, BuildingId = 99999 };

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.Create(dto));
    }

    [Fact]
    public async Task Create_ShouldThrowFloorAlreadyExistsException_WhenFloorNumberExistsInSameBuilding() {
        var building = new Building { BuildingCode = "M", BuildingName = "Humántudományi Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        _context.Floors.Add(new Floor { Number = 1, BuildingId = building.Id });
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);
        var duplicateDto = new FloorCreateDto { Number = 1, BuildingId = building.Id };

        await Assert.ThrowsAsync<FloorAlreadyExistsException>(() => service.Create(duplicateDto));
    }


    [Fact]
    public async Task Update_ShouldUpdateFieldsSuccessfully_WhenValidDataProvided() {
        var building = new Building { BuildingCode = "B", BuildingName = "Mérnöki Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);
        var updateDto = new FloorUpdateDto { Number = 2, BuildingId = building.Id };

        var result = await service.Update(floor.Id, updateDto);

        Assert.NotNull(result);
        Assert.Equal(2, result.Number);
    }

    [Fact]
    public async Task Update_ShouldThrowFloorNotFoundException_WhenFloorDoesNotExist() {
        var service = new FloorService(_context);
        var updateDto = new FloorUpdateDto { Number = 1, BuildingId = 1 };

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.Update(99999, updateDto));
    }

    [Fact]
    public async Task Update_ShouldThrowBuildingNotFoundException_WhenNewBuildingDoesNotExist() {
        var building = new Building { BuildingCode = "N", BuildingName = "Humántudományi Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);
        var updateDto = new FloorUpdateDto { Number = 1, BuildingId = 99999 };

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.Update(floor.Id, updateDto));
    }

    [Fact]
    public async Task Update_ShouldThrowFloorAlreadyExistsException_WhenNumberConflictsInTargetBuilding() {
        var building = new Building { BuildingCode = "I", BuildingName = "Műszaki Informatika Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        _context.Floors.Add(new Floor { Number = 1, BuildingId = building.Id });
        var targetFloor = new Floor { Number = 2, BuildingId = building.Id };
        _context.Floors.Add(targetFloor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);
        var updateDto = new FloorUpdateDto { Number = 1, BuildingId = building.Id };

        await Assert.ThrowsAsync<FloorAlreadyExistsException>(() => service.Update(targetFloor.Id, updateDto));
    }


    [Fact]
    public async Task Delete_ShouldRemoveFloorSuccessfully_WhenFloorExists() {
        var building = new Building { BuildingCode = "N", BuildingName = "Humántudományi Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var service = new FloorService(_context);

        var result = await service.Delete(floor.Id);
        var floorInDb = await _context.Floors.FindAsync(floor.Id);

        Assert.True(result);
        Assert.Null(floorInDb);
    }

    [Fact]
    public async Task Delete_ShouldThrowFloorNotFoundException_WhenFloorDoesNotExist() {
        var service = new FloorService(_context);

        await Assert.ThrowsAsync<FloorNotFoundException>(() => service.Delete(99999));
    }

}
