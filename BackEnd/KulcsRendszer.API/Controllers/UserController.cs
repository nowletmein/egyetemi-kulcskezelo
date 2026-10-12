using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase {
    private readonly IUserService _userService;
    public UserController(IUserService userService) {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int Id) {
        var result = await _userService.Get(Id);
        return Ok(result);
    }
}
