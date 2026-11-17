using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Exceptions;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MaintenanceServiceTests : IAsyncLifetime {
    private static readonly DateTime Start = new(2026, 11, 20, 10, 0, 0);

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

    private async Task<(User Admin, ClassRoom Room, ClassRoom OtherRoom)> CreateAdminAndRoomsAsync() {
        var admin = new User { Name = "Rendszergazda", Email = "admin@example.com" };
        _context.Users.Add(admin);

        var building = new Building { BuildingCode = "A", BuildingName = "A épület" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom { Id = "A101", FloorId = floor.Id, Name = "Előadó" };
        var otherRoom = new ClassRoom { Id = "A102", FloorId = floor.Id, Name = "Labor" };
        _context.ClassRooms.AddRange(room, otherRoom);
        await _context.SaveChangesAsync();

        return (admin, room, otherRoom);
    }

    private async Task<Booking> AddBookingAsync(
        int userId,
        string roomId,
        DateTime startTime,
        DateTime endTime,
        string status = BookingStatuses.Active) {
        var booking = new Booking {
            UserId = userId,
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            SpecialRequest = ""
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    private static MaintenanceCreateDto CreateDto(
        string roomId,
        DateTime startTime,
        DateTime endTime) => new MaintenanceCreateDto {
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime
        };

    [Fact]
    public async Task CreateAsync_ShouldCreateMaintenance_WhenRequestIsValid() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);

        var result = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.True(result.Maintenance.Id > 0);
        Assert.Equal(room.Id, result.Maintenance.RoomId);
        Assert.Equal(admin.Id, result.Maintenance.AdminId);
        Assert.Equal(Start, result.Maintenance.StartTime);
        Assert.Equal(Start.AddHours(2), result.Maintenance.EndTime);
        Assert.Empty(result.AffectedBookings);
        Assert.Equal(1, await _context.Maintenances.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnAffectedBookings_WhenBookingsOverlap() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        var later = await AddBookingAsync(admin.Id, room.Id, Start.AddHours(1), Start.AddHours(3));
        var earlier = await AddBookingAsync(admin.Id, room.Id, Start.AddHours(-1), Start.AddMinutes(30));
        var service = new MaintenanceService(_context);

        var result = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.Equal(
            new[] { earlier.Id, later.Id },
            result.AffectedBookings.Select(booking => booking.Id));
    }

    [Fact]
    public async Task CreateAsync_ShouldNotReturnBookings_ThatOnlyTouchTheMaintenance() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        await AddBookingAsync(admin.Id, room.Id, Start.AddHours(-1), Start);
        await AddBookingAsync(admin.Id, room.Id, Start.AddHours(2), Start.AddHours(3));
        var service = new MaintenanceService(_context);

        var result = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.Empty(result.AffectedBookings);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotReturnNoShowBookings() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        await AddBookingAsync(
            admin.Id,
            room.Id,
            Start,
            Start.AddHours(1),
            BookingStatuses.NoShow);
        var service = new MaintenanceService(_context);

        var result = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.Empty(result.AffectedBookings);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotReturnBookings_InAnotherRoom() {
        var (admin, room, otherRoom) = await CreateAdminAndRoomsAsync();
        await AddBookingAsync(admin.Id, otherRoom.Id, Start, Start.AddHours(1));
        var service = new MaintenanceService(_context);

        var result = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.Empty(result.AffectedBookings);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenStartIsNotBeforeEnd() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                CreateDto(room.Id, Start, Start),
                admin.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowClassRoomNotFoundException_WhenRoomDoesNotExist() {
        var (admin, _, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);

        await Assert.ThrowsAsync<ClassRoomNotFoundException>(
            () => service.CreateAsync(
                CreateDto("missing", Start, Start.AddHours(1)),
                admin.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUserNotFoundException_WhenAdminDoesNotExist() {
        var (_, room, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.CreateAsync(
                CreateDto(room.Id, Start, Start.AddHours(1)),
                99999,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowMaintenanceAlreadyExistsException_WhenMaintenanceOverlaps() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);
        await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        await Assert.ThrowsAsync<MaintenanceAlreadyExistsException>(
            () => service.CreateAsync(
                CreateDto(room.Id, Start.AddHours(1), Start.AddHours(3)),
                admin.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowOverlappingMaintenance_InAnotherRoom() {
        var (admin, room, otherRoom) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);
        await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        await service.CreateAsync(
            CreateDto(otherRoom.Id, Start, Start.AddHours(2)),
            admin.Id,
            CancellationToken.None);

        Assert.Equal(2, await _context.Maintenances.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMaintenancesOrderedByStartTime() {
        var (admin, room, otherRoom) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);
        await service.CreateAsync(
            CreateDto(room.Id, Start.AddDays(1), Start.AddDays(1).AddHours(1)),
            admin.Id,
            CancellationToken.None);
        await service.CreateAsync(
            CreateDto(otherRoom.Id, Start, Start.AddHours(1)),
            admin.Id,
            CancellationToken.None);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(otherRoom.Id, result[0].RoomId);
        Assert.Equal(room.Id, result[1].RoomId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveMaintenance_WhenItExists() {
        var (admin, room, _) = await CreateAdminAndRoomsAsync();
        var service = new MaintenanceService(_context);
        var created = await service.CreateAsync(
            CreateDto(room.Id, Start, Start.AddHours(1)),
            admin.Id,
            CancellationToken.None);

        await service.DeleteAsync(created.Maintenance.Id, CancellationToken.None);

        Assert.Equal(0, await _context.Maintenances.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowMaintenanceNotFoundException_WhenItDoesNotExist() {
        var service = new MaintenanceService(_context);

        await Assert.ThrowsAsync<MaintenanceNotFoundException>(
            () => service.DeleteAsync(99999, CancellationToken.None));
    }
}
