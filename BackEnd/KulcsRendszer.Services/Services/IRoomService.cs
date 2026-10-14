using KulcsRendszer.DataContext.Dtos;

namespace KulcsRendszer.Services.Services {
    public interface IRoomService {
        Task<IReadOnlyList<RoomResponse>> GetByFloorAsync(int floorId, CancellationToken cancellationToken);
    }
}
