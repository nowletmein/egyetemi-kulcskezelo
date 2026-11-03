using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/rooms")]
    public class RoomController : ControllerBase {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService) {
            _roomService = roomService;
        }

        [HttpPost("create")]
        [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<RoomResponse>> Create(
            [FromBody] RoomCreateDto roomCreateDto,
            CancellationToken cancellationToken) {
            var room = await _roomService.CreateRoomAsync(
                roomCreateDto,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, room);
        }

        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken) {
            await _roomService.DeleteRoomAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
