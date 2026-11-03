using System.Collections.Generic;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {

    [ApiController]
    [Route("api/Room")]
    [Authorize]
    public class RoomController : ControllerBase {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService) {
            _roomService = roomService;
        }


        [HttpGet("getall")]
        public async Task<ActionResult<List<RoomResponse>>> GetAll() {
            var rooms = await _roomService.GetAll();
            return Ok(rooms);
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<RoomResponse>> Get(string id) {
            var room = await _roomService.Get(id);
            return Ok(room);
        }

        [HttpGet("info/{id}")]
        public async Task<ActionResult<RoomInfoDto>> GetInfo(string id) {
            var roomInfo = await _roomService.GetInfo(id);
            return Ok(roomInfo);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RoomResponse>> Create([FromBody] RoomCreateDto roomCreateDto) {
            var createdRoom = await _roomService.Create(roomCreateDto);
            return CreatedAtAction(nameof(Get), new { id = createdRoom.Id }, createdRoom);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RoomResponse>> Update(string id, [FromBody] RoomUpdateDto roomUpdateDto) {
            var updatedRoom = await _roomService.Update(id, roomUpdateDto);
            return Ok(updatedRoom);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id) {
            await _roomService.Delete(id);
            return NoContent();
        }
    }
}
