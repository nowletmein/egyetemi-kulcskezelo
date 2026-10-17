using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.API.Middleware;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/floors")]
    public class FloorController : ControllerBase {
        private readonly IRoomService _roomService;

        public FloorController(IRoomService roomService) {
            _roomService = roomService;
        }

        [HttpGet("{floorId:int}/rooms")]
        [ProducesResponseType(typeof(IReadOnlyList<RoomResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetRooms(int floorId, CancellationToken cancellationToken) {
            if (floorId < 1) {
                throw new InvalidFloorIdException("The floor ID must be greater than zero.");
            }

            var rooms = await _roomService.GetByFloorAsync(floorId, cancellationToken);

            if (rooms is null) {
                throw new FloorNotFoundException($"Floor with ID {floorId} was not found.");
            }

            if (rooms.Count == 0) {
                throw new RoomsNotFoundException($"No rooms were found on floor with ID {floorId}.");
            }

            return Ok(rooms);
        }
    }
}
