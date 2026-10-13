using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase {
    private readonly IUserService _userService;

    public UserController(IUserService userService) {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id) {
        var result = await _userService.Get(id);
        return Ok(result);
    }

    [HttpGet("info/{id}")]
    public async Task<IActionResult> GetInfo(int id) {
        var result = await _userService.GetInfo(id);
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(UserCreateDto userCreateDto) {
        var result = await _userService.Create(userCreateDto);
        return Ok(result);
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, UserUpdateDto userUpdateDto) {
        var result = await _userService.Update(id, userUpdateDto);
        return Ok(result);
    }

    [HttpPost("change-password/{id}")]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto changePasswordDto) {
        await _userService.ChangePassword(id, changePasswordDto);
        return Ok(new { message = "Password updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) {
        await _userService.Delete(id);
        return NoContent();
    }
}