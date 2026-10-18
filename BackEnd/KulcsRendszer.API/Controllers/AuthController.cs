using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase {
        private readonly IUserService _userService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuthAuditService _authAuditService;

        public AuthController(
            IUserService userService,
            IJwtTokenService jwtTokenService,
            IAuthAuditService authAuditService) {
            _userService = userService;
            _jwtTokenService = jwtTokenService;
            _authAuditService = authAuditService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken) {
            User? user;
            try {
                user = await _userService.Register(registerDto, cancellationToken);
            }
            catch (DuplicateEmailException exception) {
                await _authAuditService.LogAsync(
                    AuthAuditEvent.RegistrationFailed,
                    registerDto.Email,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken);
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Registration failed.",
                    detail: exception.Message);
            }

            if (user == null) {
                await _authAuditService.LogAsync(
                    AuthAuditEvent.RegistrationFailed,
                    registerDto.Email,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken);
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Registration failed.",
                    detail: "A user with this email already exists.");
            }

            await _authAuditService.LogAsync(
                AuthAuditEvent.RegistrationSucceeded,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            var response = CreateAuthResponse(user);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken) {
            var user = await _userService.Login(loginDto, cancellationToken);
            if (user == null) {
                await _authAuditService.LogAsync(
                    AuthAuditEvent.LoginFailed,
                    loginDto.Email,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken);
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication failed.",
                    detail: "Invalid email or password.");
            }

            await _authAuditService.LogAsync(
                AuthAuditEvent.LoginSucceeded,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Ok(CreateAuthResponse(user));
        }

        private AuthResponseDto CreateAuthResponse(User user) {
            var token = _jwtTokenService.CreateToken(user);
            var expiration = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;

            return new AuthResponseDto {
                Token = token,
                ExpiresAt = expiration,
                User = new UserInfoDto {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role?.Name.ToString()
                }
            };
        }
    }
}
