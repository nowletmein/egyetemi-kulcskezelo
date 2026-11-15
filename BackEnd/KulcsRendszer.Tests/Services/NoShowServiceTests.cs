using System;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class NoShowServiceTests : IAsyncLifetime {
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(15);
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

    private async Task<Booking> CreateBookingAsync(DateTime startTime) {
        var user = new User { Name = "Teszt Elek", Email = "test@example.com" };
        _context.Users.Add(user);

        var building = new Building { BuildingCode = "A", BuildingName = "A épület" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom { Id = "A101", FloorId = floor.Id, Name = "Előadó" };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();

        var booking = new Booking {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            Status = BookingStatuses.Active,
            SpecialRequest = ""
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return booking;
    }

    [Fact]
    public async Task MarkNoShowsAsync_ShouldMarkBooking_WhenGracePeriodPassedWithoutKeyPickup() {
        var start = new DateTime(2026, 11, 20, 10, 0, 0);
        var booking = await CreateBookingAsync(start);
        var service = new NoShowService(_context);

        var count = await service.MarkNoShowsAsync(
            start.AddMinutes(20), GracePeriod, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal(BookingStatuses.NoShow, booking.Status);
    }

    [Fact]
    public async Task MarkNoShowsAsync_ShouldNotMarkBooking_WhenKeyWasPickedUp() {
        var start = new DateTime(2026, 11, 20, 10, 0, 0);
        var booking = await CreateBookingAsync(start);
        var key = new Key { RoomId = booking.RoomId };
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        _context.KeyMovements.Add(new KeyMovement {
            KeyId = key.Id,
            UserId = booking.UserId,
            BookingId = booking.Id,
            Timestamp = start.AddMinutes(5)
        });
        await _context.SaveChangesAsync();
        var service = new NoShowService(_context);

        var count = await service.MarkNoShowsAsync(
            start.AddMinutes(20), GracePeriod, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Equal(BookingStatuses.Active, booking.Status);
    }

    [Fact]
    public async Task MarkNoShowsAsync_ShouldNotMarkBooking_WhenGracePeriodNotPassed() {
        var start = new DateTime(2026, 11, 20, 10, 0, 0);
        var booking = await CreateBookingAsync(start);
        var service = new NoShowService(_context);

        var count = await service.MarkNoShowsAsync(
            start.AddMinutes(10), GracePeriod, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Equal(BookingStatuses.Active, booking.Status);
    }
}
