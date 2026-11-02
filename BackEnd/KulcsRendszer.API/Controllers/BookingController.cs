using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/bookings")]
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
            await _bookingService.DeleteUserBookingAsync(
                bookingId,
                cancellationToken);

            return NoContent();
        }
    }
}
