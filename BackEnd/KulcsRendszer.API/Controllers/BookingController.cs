using System.Security.Claims;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingController : ControllerBase {
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService;
        public BookingController(IBookingService bookingService, IUserService userService) {
            _bookingService = bookingService;
            _userService = userService;
        }

        [HttpGet("getall")]
        [Authorize(Roles = "Admin,Portas")]
        [ProducesResponseType(
            typeof(IReadOnlyList<BookingDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetAllBookings(
            CancellationToken cancellationToken) {
            var bookings = await _bookingService.GetAllBookingsAsync(
                cancellationToken);

            return Ok(bookings);
        }

        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<BookingDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetUserBookings(
            int userId,
            CancellationToken cancellationToken) {
            if (!TryGetCurrentUserId(out var currentUserId)) {
                return Unauthorized();
            }
            if (!User.IsInRole("Admin") && userId != currentUserId) {
                return Forbid();
            }

            var bookings = await _bookingService.GetUserBookingsAsync(
                userId,
                cancellationToken);

            return Ok(bookings);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<BookingDto>> AddUserBooking(
            [FromBody] BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken) {
            if (!User.IsInRole("Oktato") &&
                User.HasClaim(claim => claim.Type == ClaimTypes.Role)) {
                return Forbid();
            }
            if (!TryGetCurrentUserId(out var currentUserId)) {
                return Unauthorized();
            }
            if (bookingCreateDto.UserId != currentUserId) {
                return Forbid();
            }

            var booking = await _bookingService.AddUserBookingAsync(
                bookingCreateDto,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, booking);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteUserBooking(
            int bookingId,
            CancellationToken cancellationToken) {
            if (!TryGetCurrentUserId(out var currentUserId)) {
                return Unauthorized();
            }

            var bookingOwnerId = await _bookingService.GetBookingOwnerIdAsync(
                bookingId,
                cancellationToken);
            if (bookingOwnerId == null) {
                return NotFound();
            }
            if (!User.IsInRole("Admin") && bookingOwnerId != currentUserId) {
                return Forbid();
            }

            await _bookingService.DeleteUserBookingAsync(
                bookingId,
                cancellationToken);

            return NoContent();
        }

        [HttpPost("{keyId}/generate-pin")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> GeneratePIN(int keyId, [FromBody] int userId) {
            string pin = await _bookingService.GeneratePIN(keyId, userId);
            return Ok(await _userService.AddNotification(userId, $"Your key pickup PIN is: {pin}"));
        }

        [HttpPost("authenticate-pin")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> AuthenticatePIN([FromBody] AuthenticatePINDto authenticatePIN) {
            var result = await _bookingService.AuthenticatePIN(authenticatePIN.UserId, authenticatePIN.KeyId, authenticatePIN.PIN, authenticatePIN.BookingId);
            if (!result) {
                return BadRequest("Invalid or expired PIN.");
            }
            return Ok(result);
        }

        [HttpPost("return-key")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> ReturnKey([FromBody] KeyReturnDto keyReturn) {
            var result = await _bookingService.ReturnKey(keyReturn.UserId, keyReturn.KeyId, keyReturn.BookingId);
            return Ok(result);
        }

        private bool TryGetCurrentUserId(out int userId) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim?.Value, out userId);
        }


    }
}
