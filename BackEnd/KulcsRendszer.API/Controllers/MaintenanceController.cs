using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/maintenance")]
    [Authorize(Roles = "Admin")]
    public class MaintenanceController : ControllerBase {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService) {
            _maintenanceService = maintenanceService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<MaintenanceDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MaintenanceDto>>> GetAll(
            CancellationToken cancellationToken) {
            var maintenances = await _maintenanceService.GetAllAsync(cancellationToken);
            return Ok(maintenances);
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(MaintenanceCreateResultDto),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<MaintenanceCreateResultDto>> Create(
            [FromBody] MaintenanceCreateDto maintenanceCreateDto,
            CancellationToken cancellationToken) {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminIdClaim?.Value, out var adminId)) {
                return Unauthorized();
            }

            var result = await _maintenanceService.CreateAsync(
                maintenanceCreateDto,
                adminId,
                cancellationToken);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken) {
            await _maintenanceService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
