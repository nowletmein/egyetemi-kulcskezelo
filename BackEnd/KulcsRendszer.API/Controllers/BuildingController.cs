using System;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class BuildingController : ControllerBase {
        private readonly IBuildingService _buildingService;

        public BuildingController(IBuildingService buildingService) {
            _buildingService = buildingService;
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<BuildingDto>> Get(int id) {
            var building = await _buildingService.Get(id);
            return Ok(building);
        }

        [HttpGet("info/{id}")]
        public async Task<ActionResult<BuildingInfoDto>> GetInfo(int id) {
            var buildingInfo = await _buildingService.GetInfo(id);
            return Ok(buildingInfo);
        }

        [HttpPost("create")]
        public async Task<ActionResult<BuildingDto>> Create([FromBody] BuildingCreateDto buildingCreateDto) {
            var createdBuilding = await _buildingService.Create(buildingCreateDto);
            return CreatedAtAction(nameof(Get), new { id = createdBuilding.Id }, createdBuilding);
        }

        [HttpPut("update/{id}")]
        public async Task<ActionResult<BuildingDto>> Update(int id, [FromBody] BuildingUpdateDto buildingUpdateDto) {
            var updatedBuilding = await _buildingService.Update(id, buildingUpdateDto);
            return Ok(updatedBuilding);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id) {
            await _buildingService.Delete(id);
            return NoContent();
        }
    }
}
