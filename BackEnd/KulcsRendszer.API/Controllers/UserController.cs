using System;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase {
        private readonly IUserService _userService;

        public UserController(IUserService userService) {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {
            var user = await _userService.Get(id);
            //MOVE THIS CHECK TO Service
            if (user == null) {
                return NotFound($"User with this ID {id} is not found!");
            }
            return Ok(user);
        }

        [HttpGet("info/{id}")]
        public async Task<IActionResult> GetInfo(int id) {
            var userInfo = await _userService.GetInfo(id);
            //MOVE THIS CHECK TO Service
            if (userInfo == null) {
                return NotFound($"User with this ID {id} is not found!");
            }
            return Ok(userInfo);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto userCreateDto) {
            var user = await _userService.Create(userCreateDto);
            //check if user already exists
            return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto userUpdateDto) {
            var updatedUser = await _userService.Update(id, userUpdateDto);
            //MOVE THIS CHECK TO Service
            if (updatedUser == null) {
                return NotFound($"User with this ID {id} is not found!");
            }
            return Ok(updatedUser);
        }

        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto) {
            try {
                var success = await _userService.ChangePassword(id, changePasswordDto);
                //MOVE THIS CHECK TO Service
                if (!success) {
                    return NotFound($"User with this ID {id} is not found!");
                }
                return Ok(new { message = "Password updated successfully." });
            }
            catch (ArgumentException ex) {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex) {
                return StatusCode(401, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) {
            var success = await _userService.Delete(id);
            //MOVE THIS CHECK TO Service
            if (!success) {
                return NotFound($"User with this ID {id} is not found!");
            }
            return NoContent();
        }
        //Role
        [HttpPost("role/{userId}")]
        public async Task<IActionResult> AddRoleToUser(int userId, [FromBody] int roleId) {
            return Ok(_userService.AddRoleToUser(userId, roleId));
        }

        [HttpPost("role/")]
        public async Task<IActionResult> AddRole([FromBody] RoleCreateDto roleCreateDto) {
            return Ok(_userService.CreateRole(roleCreateDto));
        }

        [HttpDelete("role/{Id}")]
        public async Task<IActionResult> DeleteRole(int Id) {
            return Ok(_userService.DeleteRole(Id));
        }

        [HttpDelete("role/remove/{Id}")]
        public async Task<IActionResult> RemoveFromUser(int Id) {
            return Ok(_userService.RemoveRoleFromUser(Id));
        }
    }
}
