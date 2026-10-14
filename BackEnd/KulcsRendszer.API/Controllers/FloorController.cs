using KulcsRendszer.DataContext.Dtos;
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

        /// <summary>
        /// Lists the rooms assigned to a floor.
        /// </summary>
        [HttpGet("{floorId:int}/rooms")]
        [ProducesResponseType(typeof(IReadOnlyList<RoomResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetRooms(
            int floorId,
            CancellationToken cancellationToken) {
            if (floorId < 1) {
                return BadRequest("The floor ID must be greater than zero.");
            }

            return Ok(await _roomService.GetByFloorAsync(floorId, cancellationToken));
        }
    }
}
