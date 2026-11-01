using System;
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
    public class UserController : ControllerBase {
        private readonly IUserService _userService;

        public UserController(IUserService userService) {
            _userService = userService;
        }
        //ROLES:
        //for testing purposes while we dont have a seeder if you want to test and endpoint just replace [Authorize(Roles = "Oktato,Portas,Admin,Igazgato")] to [AllowAnonymous]
        //if you dont have a user token created with admin rights
        /*
        Oktato,
        Portas,
        Admin,
        Igazgato*/

        [HttpGet("{id}")]
        [Authorize(Roles = "Oktato,Portas,Admin,Igazgato")]
        public async Task<IActionResult> Get() {

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null) {
                return Unauthorized("User ID not found in token.");
            }
            //var user = await _userService.Get(System.Security.Claims.ClaimTypes.NameIdentifier);
            //MOVE THIS CHECK TO Service
            /* if (user == null) {
                 return NotFound($"User with this ID {id} is not found!");
               }*/
            var Id = int.Parse(claim.Value);
            var result = await _userService.Get(Id);
            return Ok(result);
        }

        [HttpGet("info/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetInfo(int id) {
            var userInfo = await _userService.GetInfo(id);
            //MOVE THIS CHECK TO Service
            if (userInfo == null) {
                return NotFound($"User with this ID {id} is not found!");
            }
            return Ok(userInfo);
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] UserCreateDto userCreateDto) {
            //check if user already exists
            return Ok(await _userService.Create(userCreateDto));
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] string email, [FromBody] string password) {
            return Ok(await _userService.Login(email, password));
        }


        [HttpPut("update")]
        [Authorize(Roles = "Oktato,Portas,Admin,Igazgato")]
        public async Task<IActionResult> Update([FromBody] UserUpdateDto userUpdateDto) {

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) {
                return Unauthorized("User ID not found in token.");
            }
            var Id = int.Parse(claim.Value);
            var updatedUser = await _userService.Update(Id, userUpdateDto);
            //MOVE THIS CHECK TO Service
            if (updatedUser == null) {
                return NotFound($"User with this ID {Id} is not found!");
            }
            return Ok(updatedUser);
        }

        [HttpPost("change-password")]
        [Authorize(Roles = "Oktato,Portas,Admin,Igazgato")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto) {
            try {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null) {
                    return Unauthorized("User ID not found in token.");
                }
                var Id = int.Parse(claim.Value);

                var success = await _userService.ChangePassword(Id, changePasswordDto);
                //MOVE THIS CHECK TO Service
                if (!success) {
                    return NotFound($"User with this ID {Id} is not found!");
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRoleToUser(int userId, [FromBody] int roleId) {
            return Ok(_userService.AddRoleToUser(userId, roleId));
        }

        [HttpPost("role/")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRole([FromBody] RoleCreateDto roleCreateDto) {
            return Ok(_userService.CreateRole(roleCreateDto));
        }

        [HttpDelete("role/{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(int Id) {
            return Ok(_userService.DeleteRole(Id));
        }

        [HttpDelete("role/remove/{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFromUser(int Id) {
            return Ok(_userService.RemoveRoleFromUser(Id));
        }
    }
}
