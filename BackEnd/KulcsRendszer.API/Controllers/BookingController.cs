using System.Security.Claims;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingController : ControllerBase {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService) {
            _bookingService = bookingService;
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

        private bool TryGetCurrentUserId(out int userId) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim?.Value, out userId);
        }
    }
}
