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

public class IssueTicketServiceTests : IAsyncLifetime {
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
        var user = new User { Name = "Portás Péter", Email = "portas@example.com" };
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

        return (user, room);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOpenTicket_WhenRequestIsValid() {
        var (user, room) = await CreateUserAndRoomAsync();
        var key = new Key { RoomId = room.Id };
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            KeyId = key.Id,
            RoomId = room.Id,
            Description = "Eltört a kulcs"
        };

        var result = await service.CreateAsync(dto, user.Id, CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal(key.Id, result.KeyId);
        Assert.Equal(room.Id, result.RoomId);
        Assert.Equal(user.Id, result.ReporterId);
        Assert.Equal("Eltört a kulcs", result.Description);
        Assert.Equal("Open", result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateTicket_WhenKeyIdIsNull() {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            RoomId = room.Id,
            Description = "Nem működik a projektor"
        };

        var result = await service.CreateAsync(dto, user.Id, CancellationToken.None);

        Assert.Null(result.KeyId);
        Assert.Equal(1, await _context.IssueTickets.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenDescriptionIsEmpty(string description) {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            RoomId = room.Id,
            Description = description
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(dto, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowClassRoomNotFoundException_WhenRoomDoesNotExist() {
        var (user, _) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            RoomId = "missing",
            Description = "Hiba"
        };

        await Assert.ThrowsAsync<ClassRoomNotFoundException>(
            () => service.CreateAsync(dto, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowKeysNotFoundException_WhenKeyDoesNotExist() {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            KeyId = 99999,
            RoomId = room.Id,
            Description = "Hiba"
        };

        await Assert.ThrowsAsync<KeysNotFoundException>(
            () => service.CreateAsync(dto, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUserNotFoundException_WhenReporterDoesNotExist() {
        var (_, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var dto = new IssueTicketCreateDto {
            RoomId = room.Id,
            Description = "Hiba"
        };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.CreateAsync(dto, 99999, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNewestTicketFirst() {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Első" },
            user.Id,
            CancellationToken.None);
        await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Második" },
            user.Id,
            CancellationToken.None);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Második", result[0].Description);
    }

    [Fact]
    public async Task GetByReporterAsync_ShouldReturnOnlyReportersTickets() {
        var (user, room) = await CreateUserAndRoomAsync();
        var otherUser = new User { Name = "Másik Felhasználó", Email = "masik@example.com" };
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();
        var service = new IssueTicketService(_context);
        await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Saját" },
            user.Id,
            CancellationToken.None);
        await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Másé" },
            otherUser.Id,
            CancellationToken.None);

        var result = await service.GetByReporterAsync(user.Id, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Saját", result[0].Description);
    }

    [Fact]
    public async Task GetByReporterAsync_ShouldReturnEmptyList_WhenUserHasNoTickets() {
        var (user, _) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);

        var result = await service.GetByReporterAsync(user.Id, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldChangeStatus_WhenStatusIsValid() {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var ticket = await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Hiba" },
            user.Id,
            CancellationToken.None);

        var result = await service.UpdateStatusAsync(
            ticket.Id,
            IssueTicketStatuses.Resolved,
            CancellationToken.None);

        Assert.Equal(IssueTicketStatuses.Resolved, result.Status);
        var ticketInDb = await _context.IssueTickets.FindAsync(ticket.Id);
        Assert.Equal(IssueTicketStatuses.Resolved, ticketInDb!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowArgumentException_WhenStatusIsInvalid() {
        var (user, room) = await CreateUserAndRoomAsync();
        var service = new IssueTicketService(_context);
        var ticket = await service.CreateAsync(
            new IssueTicketCreateDto { RoomId = room.Id, Description = "Hiba" },
            user.Id,
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateStatusAsync(ticket.Id, "Unknown", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowIssueTicketNotFoundException_WhenTicketDoesNotExist() {
        var service = new IssueTicketService(_context);

        await Assert.ThrowsAsync<IssueTicketNotFoundException>(
            () => service.UpdateStatusAsync(
                99999,
                IssueTicketStatuses.Closed,
                CancellationToken.None));
    }
}
