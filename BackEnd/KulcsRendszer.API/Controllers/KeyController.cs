using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeyController : ControllerBase {
        private readonly IKeyService _keyService;

        public KeyController(IKeyService keyService) {
            _keyService = keyService;
        }

        [HttpGet("getall")]
        public async Task<ActionResult<List<KeyDto>>> GetAll() {
            var keys = await _keyService.GetAll();
            return Ok(keys);
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<KeyDto>> Get(int id) {
            var key = await _keyService.Get(id);
            return Ok(key);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KeyDto>> Create([FromBody] KeyDto keyDto) {
            var createdKey = await _keyService.Create(keyDto);
            return CreatedAtAction(nameof(Get), new { id = createdKey.Id }, createdKey);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KeyDto>> Update(int id, [FromBody] KeyDto keyDto) {
            var updatedKey = await _keyService.Update(id, keyDto);
            return Ok(updatedKey);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id) {
            await _keyService.Delete(id);
            return NoContent();
        }

        [HttpPost("masterkey/{keyId:int}/permissions/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GrantMasterKeyPermission(int keyId, int userId) {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminIdClaim?.Value, out var grantedByAdminId)) {
                return Unauthorized();
            }

            var permission = await _keyService.GrantMasterKeyPermission(
                keyId,
                userId,
                grantedByAdminId);

            return StatusCode(StatusCodes.Status201Created, new {
                permission.Id,
                permission.KeyId,
                permission.UserId,
                permission.GrantedByAdminId,
                permission.GrantedAt
            });
        }

        [HttpDelete("masterkey/{keyId:int}/permissions/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeMasterKeyPermission(int keyId, int userId) {
            await _keyService.RevokeMasterKeyPermission(keyId, userId);
            return NoContent();
        }
    }
}
