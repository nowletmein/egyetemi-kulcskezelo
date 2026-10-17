using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {

    public interface IUserService {
        Task<UserDto?> Get(int userId);
        Task<UserInfoDto?> GetInfo(int userId);
        Task<UserDto> Create(UserCreateDto userCreateDto);
        Task<UserDto?> Update(int userId, UserUpdateDto userUpdateDto);
        Task<bool> ChangePassword(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> Delete(int userId);
    }

    public class UserService : IUserService {
        private readonly KulcsRendszerDbContext _context;

        public UserService(KulcsRendszerDbContext context) {
            _context = context;
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
                RoleId = user.RoleId,
                Role = user.Role,
                Name = user.Name,
                Email = user.Email
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