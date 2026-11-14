using System;
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

public class BookingServiceTests : IAsyncLifetime {
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

    private async Task<(User User, ClassRoom Room)> CreateUserAndRoomAsync() {
        var user = new User { Name = "Teszt Elek", Email = "test@example.com" };
        _context.Users.Add(user);

        var building = new Building { BuildingCode = "A", BuildingName = "A épület" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var floor = new Floor { Number = 1, BuildingId = building.Id };
        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        var room = new ClassRoom {
            Id = "A101",
            FloorId = floor.Id,
            Name = "Előadó",
            Capacity = 40
        };
        _context.ClassRooms.Add(room);
        await _context.SaveChangesAsync();

        return (user, room);
    }

    private async Task<Booking> AddBookingAsync(
        int userId,
        string roomId,
        DateTime startTime,
        DateTime endTime,
        string specialRequest = "",
        string status = BookingStatuses.Active) {
        var booking = new Booking {
            UserId = userId,
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            SpecialRequest = specialRequest
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }
    [Fact]
    public async Task GetAllBookingsAsync_ShouldReturnEmptyList_WhenNoBookingsExist() {
        var service = new BookingService(_context);

        var result = await service.GetAllBookingsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ShouldReturnAllBookingsSortedByStartTime() {
        var (user, room) = await CreateUserAndRoomAsync();
        var laterStart = new DateTime(2026, 6, 2, 12, 0, 0);
        var earlierStart = new DateTime(2026, 6, 2, 9, 0, 0);
        await AddBookingAsync(user.Id, room.Id, laterStart, laterStart.AddHours(1), "Későbbi");
        await AddBookingAsync(user.Id, room.Id, earlierStart, earlierStart.AddHours(1), "Korábbi");
        var service = new BookingService(_context);

        var result = await service.GetAllBookingsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(earlierStart, result[0].StartTime);
        Assert.Equal("Korábbi", result[0].SpecialRequest);
        Assert.Equal(laterStart, result[1].StartTime);
        Assert.All(result, booking => Assert.Equal(user.Id, booking.UserId));
        Assert.All(result, booking => Assert.Equal(room.Id, booking.RoomId));
    }

    [Fact]
    public async Task GetUserBookingsAsync_ShouldReturnUserBookings_WhenBookingsExist() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 7, 1, 10, 0, 0);
        var booking = await AddBookingAsync(
            user.Id,
            room.Id,
            startTime,
            startTime.AddHours(1),
            "Vizsga");
        var service = new BookingService(_context);

        var result = await service.GetUserBookingsAsync(user.Id, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(booking.Id, result[0].Id);
        Assert.Equal(user.Id, result[0].UserId);
        Assert.Equal(room.Id, result[0].RoomId);
        Assert.Equal(BookingStatuses.Active, result[0].Status);
        Assert.Equal("Vizsga", result[0].SpecialRequest);
    }

    [Fact]
    public async Task GetUserBookingsAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist() {
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.GetUserBookingsAsync(99999, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserBookingsAsync_ShouldThrowBookingNotFoundException_WhenUserHasNoBookings() {
        var (user, _) = await CreateUserAndRoomAsync();
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<BookingNotFoundException>(
            () => service.GetUserBookingsAsync(user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldCreateBooking_WhenRequestIsValid() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 8, 1, 10, 0, 0);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            SpecialRequest = "Csoportos foglalkozás"
        };
        var service = new BookingService(_context);

        var result = await service.AddUserBookingAsync(dto, CancellationToken.None);
        var bookingInDb = await _context.Bookings.FindAsync(result.Id);

        Assert.NotNull(bookingInDb);
        Assert.True(result.Id > 0);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(room.Id, result.RoomId);
        Assert.Equal(dto.StartTime, result.StartTime);
        Assert.Equal(dto.EndTime, result.EndTime);
        Assert.Equal(BookingStatuses.Active, result.Status);
        Assert.Equal(dto.SpecialRequest, result.SpecialRequest);
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrowArgumentException_WhenStartTimeEqualsEndTime() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 8, 1, 10, 0, 0);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = startTime,
            EndTime = startTime
        };
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrowArgumentException_WhenStartTimeIsAfterEndTime() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 8, 1, 11, 0, 0);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(-1)
        };
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist() {
        var (_, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 8, 1, 10, 0, 0);
        var dto = new BookingCreateDto {
            UserId = 99999,
            RoomId = room.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(1)
        };
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrowClassRoomNotFoundException_WhenRoomDoesNotExist() {
        var (user, _) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 8, 1, 10, 0, 0);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = "missing",
            StartTime = startTime,
            EndTime = startTime.AddHours(1)
        };
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<ClassRoomNotFoundException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrowBookingAlreadyExistsException_WhenBookingOverlaps() {
        var (user, room) = await CreateUserAndRoomAsync();
        var existingStart = new DateTime(2026, 8, 1, 10, 0, 0);
        await AddBookingAsync(user.Id, room.Id, existingStart, existingStart.AddHours(2));
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = existingStart.AddHours(1),
            EndTime = existingStart.AddHours(3)
        };
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<BookingAlreadyExistsException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldAllowBookingStartingWhenExistingBookingEnds() {
        var (user, room) = await CreateUserAndRoomAsync();
        var existingStart = new DateTime(2026, 8, 1, 10, 0, 0);
        await AddBookingAsync(user.Id, room.Id, existingStart, existingStart.AddHours(1));
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = existingStart.AddHours(1),
            EndTime = existingStart.AddHours(2)
        };
        var service = new BookingService(_context);

        var result = await service.AddUserBookingAsync(dto, CancellationToken.None);

        Assert.Equal(dto.StartTime, result.StartTime);
        Assert.Equal(2, await _context.Bookings.CountAsync());
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldAllowOverlappingTime_WhenBookingUsesAnotherRoom() {
        var (user, room) = await CreateUserAndRoomAsync();
        var secondRoom = new ClassRoom {
            Id = "A102",
            FloorId = room.FloorId,
            Name = "Másik terem"
        };
        _context.ClassRooms.Add(secondRoom);
        await _context.SaveChangesAsync();
        var startTime = new DateTime(2026, 8, 1, 10, 0, 0);
        await AddBookingAsync(user.Id, room.Id, startTime, startTime.AddHours(2));
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = secondRoom.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(2)
        };
        var service = new BookingService(_context);

        var result = await service.AddUserBookingAsync(dto, CancellationToken.None);

        Assert.Equal(secondRoom.Id, result.RoomId);
        Assert.Equal(2, await _context.Bookings.CountAsync());
    }

    [Fact]
    public async Task DeleteUserBookingAsync_ShouldRemoveBooking_WhenBookingExists() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 9, 1, 10, 0, 0);
        var booking = await AddBookingAsync(user.Id, room.Id, startTime, startTime.AddHours(1));
        var service = new BookingService(_context);

        var result = await service.DeleteUserBookingAsync(booking.Id, CancellationToken.None);
        var bookingInDb = await _context.Bookings.FindAsync(booking.Id);

        Assert.True(result);
        Assert.Null(bookingInDb);
    }

    [Fact]
    public async Task DeleteUserBookingAsync_ShouldThrowBookingNotFoundException_WhenBookingDoesNotExist() {
        var service = new BookingService(_context);

        await Assert.ThrowsAsync<BookingNotFoundException>(
            () => service.DeleteUserBookingAsync(99999, CancellationToken.None));
    }

    [Fact]
    public async Task GetBookingOwnerIdAsync_ShouldReturnUserId_WhenBookingExists() {
        var (user, room) = await CreateUserAndRoomAsync();
        var startTime = new DateTime(2026, 10, 1, 10, 0, 0);
        var booking = await AddBookingAsync(user.Id, room.Id, startTime, startTime.AddHours(1));
        var service = new BookingService(_context);

        var result = await service.GetBookingOwnerIdAsync(booking.Id, CancellationToken.None);

        Assert.Equal(user.Id, result);
    }

    [Fact]
    public async Task GetBookingOwnerIdAsync_ShouldReturnNull_WhenBookingDoesNotExist() {
        var service = new BookingService(_context);

        var result = await service.GetBookingOwnerIdAsync(99999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldThrow_WhenRoomIsUnderMaintenance() {
        var (user, room) = await CreateUserAndRoomAsync();
        var start = new DateTime(2026, 9, 1, 10, 0, 0);
        _context.Maintenances.Add(new Maintenance {
            RoomId = room.Id,
            AdminId = user.Id,
            StartTime = start,
            EndTime = start.AddHours(3)
        });
        await _context.SaveChangesAsync();
        var service = new BookingService(_context);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = start.AddHours(1),
            EndTime = start.AddHours(2)
        };

        await Assert.ThrowsAsync<BookingAlreadyExistsException>(
            () => service.AddUserBookingAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldCreateBooking_WhenMaintenanceEndsAtBookingStart() {
        var (user, room) = await CreateUserAndRoomAsync();
        var start = new DateTime(2026, 9, 1, 10, 0, 0);
        _context.Maintenances.Add(new Maintenance {
            RoomId = room.Id,
            AdminId = user.Id,
            StartTime = start.AddHours(-2),
            EndTime = start
        });
        await _context.SaveChangesAsync();
        var service = new BookingService(_context);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = start,
            EndTime = start.AddHours(1)
        };

        var result = await service.AddUserBookingAsync(dto, CancellationToken.None);

        Assert.Equal(BookingStatuses.Active, result.Status);
    }

    [Fact]
    public async Task AddUserBookingAsync_ShouldCreateBooking_WhenOverlappingBookingIsNoShow() {
        var (user, room) = await CreateUserAndRoomAsync();
        var start = new DateTime(2026, 9, 2, 10, 0, 0);
        await AddBookingAsync(
            user.Id,
            room.Id,
            start,
            start.AddHours(2),
            status: BookingStatuses.NoShow);
        var service = new BookingService(_context);
        var dto = new BookingCreateDto {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = start,
            EndTime = start.AddHours(1)
        };

        var result = await service.AddUserBookingAsync(dto, CancellationToken.None);

        Assert.Equal(BookingStatuses.Active, result.Status);
    }
}
