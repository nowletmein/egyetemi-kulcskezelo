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

public class BuildingServiceTests : IAsyncLifetime {
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
    public async Task GetAll_ShouldReturnEmptyList_WhenNoBuildingsExist() {
        var service = new BuildingService(_context);

        var result = await service.GetAll();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllBuildingsWithFloorsAndRooms() {
        var building = new Building { BuildingCode = "A", BuildingName = "Gazdaságtudományi Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom { Id = "1", Name = "A1", FloorId = floor.Id, Capacity = 200, Equipment = "Projektor, Mikrofon" };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);

        var result = await service.GetAll();

        var createdBuilding = result.FirstOrDefault(b => b.BuildingCode == "A");
        Assert.NotNull(createdBuilding);
        Assert.Equal("A", createdBuilding.BuildingCode);
        Assert.Single(createdBuilding.Floors);
        Assert.Single(createdBuilding.Floors.First().Rooms);
    }

    [Fact]
    public async Task Get_ShouldReturnBuilding_WhenBuildingExists() {
        var building = new Building { BuildingCode = "I", BuildingName = "Műszaki Informatika Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);

        var result = await service.Get(building.Id);

        Assert.NotNull(result);
        Assert.Equal(building.Id, result.Id);
        Assert.Equal("Műszaki Informatika Kar", result.BuildingName);
    }

    [Fact]
    public async Task Get_ShouldThrowBuildingNotFoundException_WhenBuildingDoesNotExist() {
        var service = new BuildingService(_context);

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.Get(99999));
    }

    [Fact]
    public async Task GetInfo_ShouldReturnBuildingInfo_WhenBuildingExists() {
        var building = new Building { BuildingCode = "C", BuildingName = "Mérnöki Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);

        var result = await service.GetInfo(building.Id);

        Assert.NotNull(result);
        Assert.Equal("C", result.BuildingCode);
        Assert.Equal("Mérnöki Kar", result.BuildingName);
    }

    [Fact]
    public async Task GetInfo_ShouldThrowBuildingNotFoundException_WhenBuildingDoesNotExist() {
        var service = new BuildingService(_context);

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.GetInfo(99999));
    }


    [Fact]
    public async Task Create_ShouldCreateBuildingSuccessfully_WhenDataIsValid() {
        var service = new BuildingService(_context);
        var dto = new BuildingCreateDto { BuildingCode = "P", BuildingName = "Polinszky Terem" };

        var result = await service.Create(dto);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("P", result.BuildingCode);
        Assert.Equal("Polinszky Terem", result.BuildingName);
    }

    [Fact]
    public async Task Create_ShouldThrowBuildingAlreadyExistsException_WhenCodeOrNameExists() {
        var existingBuilding = new Building { BuildingCode = "M", BuildingName = "Humántudományi Kar" };
        _context.Buildings.Add(existingBuilding);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);
        var duplicateDto = new BuildingCreateDto { BuildingCode = "M", BuildingName = "Humántudományi Kar" };

        await Assert.ThrowsAsync<BuildingAlreadyExistsException>(() => service.Create(duplicateDto));
    }


    [Fact]
    public async Task Update_ShouldUpdateFieldsSuccessfully_WhenValidDataProvided() {
        var building = new Building { BuildingCode = "C", BuildingName = "C Épület" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);
        var updateDto = new BuildingUpdateDto { BuildingCode = "C", BuildingName = "Mérnöki Kar" };

        var result = await service.Update(building.Id, updateDto);

        Assert.NotNull(result);
        Assert.Equal("C", result.BuildingCode);
        Assert.Equal("Mérnöki Kar", result.BuildingName);
    }

    [Fact]
    public async Task Update_ShouldIgnoreValues_WhenStringOrDefaultValuesProvided() {
        var building = new Building { BuildingCode = "T", BuildingName = "Testnevelés és Sportközpont" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);
        var updateDto = new BuildingUpdateDto { BuildingCode = "string", BuildingName = "" };

        var result = await service.Update(building.Id, updateDto);

        Assert.Equal("T", result.BuildingCode);
        Assert.Equal("Testnevelés és Sportközpont", result.BuildingName);
    }

    [Fact]
    public async Task Update_ShouldThrowBuildingNotFoundException_WhenBuildingDoesNotExist() {
        var service = new BuildingService(_context);
        var updateDto = new BuildingUpdateDto { BuildingCode = "X", BuildingName = "Nem létező épület" };

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.Update(99999, updateDto));
    }

    [Fact]
    public async Task Delete_ShouldRemoveBuildingSuccessfully_WhenBuildingExists() {
        var building = new Building { BuildingCode = "D", BuildingName = "Mérnöki Kar" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var service = new BuildingService(_context);

        var result = await service.Delete(building.Id);
        var buildingInDb = await _context.Buildings.FindAsync(building.Id);

        Assert.True(result);
        Assert.Null(buildingInDb);
    }

    [Fact]
    public async Task Delete_ShouldThrowBuildingNotFoundException_WhenBuildingDoesNotExist() {
        var service = new BuildingService(_context);

        await Assert.ThrowsAsync<BuildingNotFoundException>(() => service.Delete(99999));
    }

}
