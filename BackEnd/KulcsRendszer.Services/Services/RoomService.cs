using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IRoomService {
        Task<IReadOnlyList<RoomResponse>?> GetByFloorAsync(int floorId, CancellationToken cancellationToken);
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
                .Select(room => new RoomResponse(
                    room.Id,
                    room.Building,
                    room.FloorId,
                    room.Name,
                    room.Capacity,
                    room.Equipment))
                .ToListAsync(cancellationToken);
        }
    }
}
