using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {

    public interface IUserService {
        Task<User?> Register(RegisterDto registerDto, CancellationToken cancellationToken);
        Task<User?> Login(LoginDto loginDto, CancellationToken cancellationToken);
        Task<UserDto?> Get(int userId);
        Task<UserInfoDto?> GetInfo(int userId);
        Task<UserDto> Create(UserCreateDto userCreateDto);
        Task<UserDto?> Update(int userId, UserUpdateDto userUpdateDto);
        Task<bool> ChangePassword(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> Delete(int userId);
    }

    public sealed class DuplicateEmailException : Exception {
        public DuplicateEmailException() : base("A user with this email already exists.") {
        }
    }

    public class UserService : IUserService {
        private readonly KulcsRendszerDbContext _context;

        public UserService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<User?> Register(RegisterDto registerDto, CancellationToken cancellationToken) {
            var email = NormalizeEmail(registerDto.Email);
            var exists = await _context.Users.AnyAsync(user => user.Email == email, cancellationToken);
            if (exists) {
                return null;
            }

            var user = new User {
                Name = registerDto.Name.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            _context.Users.Add(user);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsDuplicateEmailException(exception)) {
                throw new DuplicateEmailException();
            }

            return user;
        }

        public async Task<User?> Login(LoginDto loginDto, CancellationToken cancellationToken) {
            var email = NormalizeEmail(loginDto.Email);
            var user = await _context.Users
                .Include(item => item.Role)
                .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash)) {
                return null;
            }

            return user;
        }

        private static string NormalizeEmail(string email) {
            return email.Trim().ToLowerInvariant();
        }

        private static bool IsDuplicateEmailException(DbUpdateException exception) {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        private UserDto MapToUserDto(User user) {
            return new UserDto {
                Id = user.Id,
                RoleId = user.RoleId,
                Name = user.Name,
                Email = user.Email,
                Bookings = user.Bookings ?? new List<Booking>(),
                KeyMovements = user.KeyMovements ?? new List<KeyMovement>(),
                ReportedTickets = user.ReportedTickets ?? new List<IssueTicket>(),
                OrderedMaintenances = user.OrderedMaintenances ?? new List<Maintenance>()
            };
        }

        public async Task<UserDto?> Get(int userId) {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Bookings)
                .Include(u => u.KeyMovements)
                .Include(u => u.ReportedTickets)
                .Include(u => u.OrderedMaintenances)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                return null;
            }

            return MapToUserDto(user);
        }

        public async Task<UserInfoDto?> GetInfo(int userId) {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                return null;
            }

            return new UserInfoDto {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role?.Name.ToString()
            };
        }

        public async Task<UserDto> Create(UserCreateDto userCreateDto) {
            var user = new User {
                Name = userCreateDto.Name,
                Email = userCreateDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userCreateDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<UserDto?> Update(int userId, UserUpdateDto userUpdateDto) {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                return null;
            }

            user.Name = userUpdateDto.Name;
            user.Email = userUpdateDto.Email;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Password)) {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.Password);
            }

            await _context.SaveChangesAsync();
            return MapToUserDto(user);
        }

        public async Task<bool> ChangePassword(int userId, ChangePasswordDto changePasswordDto) {
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword) {
                throw new ArgumentException("The new passwords do not match!");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                return false;
            }
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.OldPassword, user.PasswordHash)) {
                throw new UnauthorizedAccessException("Incorrect old password!");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Delete(int userId) {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
