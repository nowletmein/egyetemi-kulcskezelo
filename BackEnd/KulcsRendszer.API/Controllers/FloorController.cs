using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {

    [ApiController]
    [Route("api/Floor")]
    public class FloorController : ControllerBase {
        private readonly IFloorService _floorService;
        private readonly IRoomService _roomService;

        public FloorController(IFloorService floorService, IRoomService roomService) {
            _floorService = floorService;
            _roomService = roomService;
        }

        [HttpGet("getall")]
        public async Task<ActionResult<List<FloorDto>>> GetAll() {
            var floors = await _floorService.GetAll();
            return Ok(floors);
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<FloorDto>> Get(int id) {
            var floor = await _floorService.Get(id);
            return Ok(floor);
        }

        [HttpGet("info/{id}")]
        public async Task<ActionResult<FloorInfoDto>> GetInfo(int id) {
            var floorInfo = await _floorService.GetInfo(id);
            return Ok(floorInfo);
        }

        [HttpGet("{floorId}/rooms")]
        [ProducesResponseType(typeof(IReadOnlyList<RoomResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetRooms(int floorId, CancellationToken cancellationToken) {
            var rooms = await _roomService.GetByFloorAsync(floorId, cancellationToken);
            return Ok(rooms);
        }

        [HttpPost("create")]
        public async Task<ActionResult<FloorDto>> Create([FromBody] FloorCreateDto floorCreateDto) {
            var createdFloor = await _floorService.Create(floorCreateDto);
            return CreatedAtAction(nameof(Get), new { id = createdFloor.Id }, createdFloor);
        }

        [HttpPut("update/{id:int}")]
        public async Task<ActionResult<FloorDto>> Update(int id, [FromBody] FloorUpdateDto floorUpdateDto) {
            var updatedFloor = await _floorService.Update(id, floorUpdateDto);
            return Ok(updatedFloor);
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id) {
            await _floorService.Delete(id);
            return NoContent();
        }
    }
}
