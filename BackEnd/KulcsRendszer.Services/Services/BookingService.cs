using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IBookingService {
        Task<IReadOnlyList<BookingDto>> GetAllBookingsAsync(
            CancellationToken cancellationToken);

        Task<IReadOnlyList<BookingDto>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<BookingDto> AddUserBookingAsync(
            BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken);

        Task<bool> DeleteUserBookingAsync(
            int bookingId,
            CancellationToken cancellationToken);

        Task<int?> GetBookingOwnerIdAsync(
            int bookingId,
            CancellationToken cancellationToken);
    }

    public class BookingService : IBookingService {
        private readonly KulcsRendszerDbContext _context;

        public BookingService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<BookingDto>> GetAllBookingsAsync(
            CancellationToken cancellationToken) {
            var bookings = await _context.Bookings
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return bookings.Select(ToDto).ToList();
        }

        public async Task<IReadOnlyList<BookingDto>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken) {
            var userExists = await _context.Users.Include(user => user.Bookings).FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

            if (userExists == null) {
                throw new UserNotFoundException(
                    "User not found with this Id");
            }

            if (!userExists.Bookings.Any()) {
                throw new BookingNotFoundException(
                    "No bookings found for this user.");
            }

            return userExists.Bookings.Select(ToDto).ToList();
        }

        public async Task<BookingDto> AddUserBookingAsync(
            BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken) {
            if (bookingCreateDto.StartTime >= bookingCreateDto.EndTime) {
                throw new ArgumentException(
                    "The booking start time must be earlier than the end time.");
            }

            var userExists = await _context.Users.Include(user => user.Bookings).FirstOrDefaultAsync(
                user => user.Id == bookingCreateDto.UserId,
                cancellationToken);

            if (userExists == null) {
                throw new UserNotFoundException(
                    "User not found with this Id");
            }

            var roomExists = await _context.ClassRooms.Include(room => room.Bookings).FirstOrDefaultAsync(
                room => room.Id == bookingCreateDto.RoomId,
                cancellationToken);

            if (roomExists == null) {
                throw new ClassRoomNotFoundException(
                    "Classroom not found with this Id");
            }

            var hasOverlappingBooking = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(
                    booking =>
                        booking.RoomId == bookingCreateDto.RoomId &&
                        booking.StartTime < bookingCreateDto.EndTime &&
                        bookingCreateDto.StartTime < booking.EndTime,
                    cancellationToken);

            if (hasOverlappingBooking) {
                throw new BookingAlreadyExistsException(
                    "The classroom is already booked for the selected time.");
            }

            var booking = new Booking {
                UserId = bookingCreateDto.UserId,
                RoomId = bookingCreateDto.RoomId,
                StartTime = bookingCreateDto.StartTime,
                EndTime = bookingCreateDto.EndTime,
                Status = "Pending",
                SpecialRequest = bookingCreateDto.SpecialRequest
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            return ToDto(booking);
        }

        public async Task<bool> DeleteUserBookingAsync(
            int bookingId,
            CancellationToken cancellationToken) {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    item => item.Id == bookingId,
                    cancellationToken);

            if (booking == null) {
                throw new BookingNotFoundException(
                    "Booking not found for this user.");
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<int?> GetBookingOwnerIdAsync(
            int bookingId,
            CancellationToken cancellationToken) {
            return await _context.Bookings
                .Where(booking => booking.Id == bookingId)
                .Select(booking => (int?)booking.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static BookingDto ToDto(Booking booking) => new BookingDto {
            Id = booking.Id,
            UserId = booking.UserId,
            RoomId = booking.RoomId,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            SpecialRequest = booking.SpecialRequest
        };
    }
}
