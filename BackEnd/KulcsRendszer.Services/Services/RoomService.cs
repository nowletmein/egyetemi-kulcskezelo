using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IRoomService {
        Task<IReadOnlyList<RoomResponse>?> GetByFloorAsync(int floorId, CancellationToken cancellationToken);
        Task<RoomResponse> CreateRoomAsync(RoomCreateDto roomCreateDto, CancellationToken cancellationToken);
        Task DeleteRoomAsync(string roomId, CancellationToken cancellationToken);
    }

    public sealed class RoomService : IRoomService {
        private readonly KulcsRendszerDbContext _dbContext;

        public RoomService(KulcsRendszerDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<RoomResponse>?> GetByFloorAsync(
            int floorId,
            CancellationToken cancellationToken) {
            var floorExists = await _dbContext.Floors
                .AnyAsync(floor => floor.Id == floorId, cancellationToken);

            if (!floorExists) {
                return null;
            }

            return await _dbContext.ClassRooms
                .Where(room => room.FloorId == floorId)
                .OrderBy(room => room.Name)
                .Select(room => new RoomResponse {
                    Id = room.Id,
                    Building = room.Floor.Building.BuildingName,
                    FloorId = room.FloorId,
                    Name = room.Name,
                    Capacity = room.Capacity,
                    Equipment = room.Equipment
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<RoomResponse> CreateRoomAsync(
            RoomCreateDto roomCreateDto,
            CancellationToken cancellationToken) {
            if (roomCreateDto.Name == null || roomCreateDto.Name.Length > 100) {
                throw new ArgumentException(
                    "Room name cannot be longer than 100 characters.",
                    nameof(roomCreateDto.Name));
            }

            var floorExists = await _dbContext.Floors
                .AnyAsync(floor => floor.Id == roomCreateDto.FloorId, cancellationToken);

            if (!floorExists) {
                throw new FloorNotFoundException(
                    $"Floor not found with id: {roomCreateDto.FloorId}");
            }

            var idExists = await _dbContext.ClassRooms
                .AnyAsync(room => room.Id == roomCreateDto.Id, cancellationToken);

            var nameExists = await _dbContext.ClassRooms
                .AnyAsync(
                    room => room.FloorId == roomCreateDto.FloorId &&
                            room.Name == roomCreateDto.Name,
                    cancellationToken);

            if (idExists || nameExists) {
                throw new ClassRoomAlreadyExistsException(
                    "A room with this id or name already exists.");
            }

            var room = new ClassRoom {
                Id = roomCreateDto.Id,
                FloorId = roomCreateDto.FloorId,
                Name = roomCreateDto.Name,
                Capacity = roomCreateDto.Capacity,
                Equipment = roomCreateDto.Equipment
            };

            _dbContext.ClassRooms.Add(room);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await _dbContext.ClassRooms
                .AsNoTracking()
                .Where(existingRoom => existingRoom.Id == room.Id)
                .Select(existingRoom => new RoomResponse {
                    Id = existingRoom.Id,
                    Building = existingRoom.Floor.Building.BuildingName,
                    FloorId = existingRoom.FloorId,
                    Name = existingRoom.Name,
                    Capacity = existingRoom.Capacity,
                    Equipment = existingRoom.Equipment
                })
                .SingleAsync(cancellationToken);
        }

        public async Task DeleteRoomAsync(
            string roomId,
            CancellationToken cancellationToken) {
            var room = await _dbContext.ClassRooms
                .FirstOrDefaultAsync(existingRoom => existingRoom.Id == roomId, cancellationToken);

            if (room == null) {
                throw new ClassRoomNotFoundException(
                    $"Room not found with id: {roomId}");
            }

            var hasBooking = await _dbContext.Bookings
                .AnyAsync(booking => booking.RoomId == roomId, cancellationToken);

            var hasMaintenance = await _dbContext.Maintenances
                .AnyAsync(maintenance => maintenance.RoomId == roomId, cancellationToken);

            var hasIssueTicket = await _dbContext.IssueTickets
                .AnyAsync(issueTicket => issueTicket.RoomId == roomId, cancellationToken);

            if (hasBooking || hasMaintenance || hasIssueTicket) {
                throw new InvalidOperationException(
                    $"Room cannot be deleted because it has related records: {roomId}");
            }

            _dbContext.ClassRooms.Remove(room);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
