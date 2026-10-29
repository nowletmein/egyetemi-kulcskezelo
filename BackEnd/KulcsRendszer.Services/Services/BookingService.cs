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
        Task<IReadOnlyList<Booking>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<Booking> AddUserBookingAsync(
            BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken);

        Task<bool> DeleteUserBookingAsync(
            int userId,
            int bookingId,
            CancellationToken cancellationToken);
    }

    public class BookingService : IBookingService {
        private readonly KulcsRendszerDbContext _context;

        public BookingService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<Booking>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken) {
            var userExists = await _context.Users.Include(user => user.Bookings).FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

            if (userExists == null) {
                throw new UserNotFoundException(
                    "User not found with this Id");
            }

            return userExists.Bookings.ToList();

        }

        public async Task<Booking> AddUserBookingAsync(
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

            return booking;

        }

        public async Task<bool> DeleteUserBookingAsync(
            int userId,
            int bookingId,
            CancellationToken cancellationToken) {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == bookingId &&
                        item.UserId == userId,
                    cancellationToken);

            if (booking == null) {
                throw new BookingNotFoundException(
                    "Booking not found for this user.");
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
