using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services;
using KulcsRendszer.Services.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IBookingService {
        Task<IReadOnlyList<BookingDto>> GetAllBookingsAsync(
            CancellationToken cancellationToken);
        Task<bool> ReturnKey(int userId, int keyId, int bookingId);
        Task<IReadOnlyList<BookingDto>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken);

        Task<BookingDto> AddUserBookingAsync(
            BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken);
        Task<string> GeneratePIN(int keyId, int userId);

        Task<bool> AuthenticatePIN(int userId, int keyId, string pin, int bookingId);
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
            return await _context.Bookings
                .AsNoTracking()
                .OrderBy(booking => booking.StartTime)
                .Select(booking => new BookingDto {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    RoomId = booking.RoomId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status,
                    SpecialRequest = booking.SpecialRequest
                })
                .ToListAsync(cancellationToken);
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
                return Array.Empty<BookingDto>();
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
                        booking.Status != BookingStatuses.NoShow &&
                        booking.StartTime < bookingCreateDto.EndTime &&
                        bookingCreateDto.StartTime < booking.EndTime,
                    cancellationToken);

            if (hasOverlappingBooking) {
                throw new BookingAlreadyExistsException(
                    "The classroom is already booked for the selected time.");
            }

            var hasOverlappingMaintenance = await _context.Maintenances
                .AsNoTracking()
                .AnyAsync(
                    maintenance =>
                        maintenance.RoomId == bookingCreateDto.RoomId &&
                        maintenance.StartTime < bookingCreateDto.EndTime &&
                        bookingCreateDto.StartTime < maintenance.EndTime,
                    cancellationToken);

            if (hasOverlappingMaintenance) {
                throw new BookingAlreadyExistsException(
                    "The classroom is under maintenance during the selected time.");
            }

            var booking = new Booking {
                UserId = bookingCreateDto.UserId,
                RoomId = bookingCreateDto.RoomId,
                StartTime = bookingCreateDto.StartTime,
                EndTime = bookingCreateDto.EndTime,
                Status = BookingStatuses.Active,
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

        public async Task<string> GeneratePIN(int keyId, int userId) {
            var key = await _context.Keys.FirstOrDefaultAsync(x => x.Id == keyId);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (key == null) {
                throw new KeyNotFoundException("Key with this Id doesent exixt");
            }
            if (user == null) {
                throw new UserNotFoundException("User with this Id desent exist");
            }
            string pin = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
            key.PickupPIN = pin;
            key.PinGeneratedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return pin;

        }

        public async Task<bool> AuthenticatePIN(int userId, int keyId, string pin, int bookingId) {
            var key = await _context.Keys.FirstOrDefaultAsync(x => x.Id == keyId);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
            if (user == null) {
                throw new UserNotFoundException("User with this Id desent exist");
            }
            if (key == null) {
                throw new KeyNotFoundException("Key with this Id doesent exixt");
            }
            if (booking == null) {
                throw new BookingNotFoundException("Booking with this Id doesent exixt");
            }
            if (key.PickupPIN == pin && pin != null) {
                key.PickupPIN = null;
                key.PinGeneratedAt = null;

                _context.KeyMovements.Add(new KeyMovement {
                    KeyId = key.Id,
                    UserId = user.Id,
                    IdentificationMethod = "PIN",
                    Timestamp = DateTime.UtcNow,
                    BookingId = booking.Id,
                    Status = "given out",
                });
                await _context.SaveChangesAsync();
                return true;
            }
            else {
                return false;
            }
        }
        public async Task<bool> ReturnKey(int userId, int keyId, int bookingId) {
            var keymovement = await _context.KeyMovements.FirstOrDefaultAsync(x => x.BookingId == bookingId && x.UserId == userId && x.KeyId == keyId && x.Status == "given out" || x.Status == "unreturned");
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
            if (booking == null) {
                throw new BookingNotFoundException("Booking not found with this Id");
            }
            if (keymovement == null) {
                throw new KeyNotFoundException("This user doesent have a unreturned key");
            }
            keymovement.Status = "returned";
            booking.Status = "closed";
            await _context.SaveChangesAsync();
            return true;
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
