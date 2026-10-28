using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {

    public interface IUserService {
        //User
        Task<UserDto?> Get(int userId);
        Task<UserInfoDto?> GetInfo(int userId);
        Task<UserDto> Create(UserCreateDto userCreateDto);
        Task<UserDto?> Update(int userId, UserUpdateDto userUpdateDto);
        Task<bool> ChangePassword(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> Delete(int userId);
        //Login
        Task<string> Login(string email, string password);
        //Role
        Task<int> CreateRole(RoleCreateDto roleCreateDto);
        Task<int> AddRoleToUser(int userId, int roleId);
        Task<RoleDto?> GetRole(int roleId);
        Task<int> DeleteRole(int roleId);
        Task<int> RemoveRoleFromUser(int userId, int roleId);
    }

    public class UserService : IUserService {
        private readonly KulcsRendszerDbContext _context;

        public UserService(KulcsRendszerDbContext context) {
            _context = context;
        }
        //Mappers
        //User
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
        private User MapToUser(UserCreateDto userCreateDto) {
            return new User {
                Name = userCreateDto.Name,
                Email = userCreateDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userCreateDto.Password)
            };
        }
        //Role
        private RoleDto MapToRoleDto(Role role) {
            return new RoleDto {
                Id = role.Id,
                Name = role.Name
            };
        }
        private Role MapToRole(RoleCreateDto roleCreateDto) {
            return new Role {
                Name = roleCreateDto.Name,
            };
        }


        //Methods
        //User
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
            var user = MapToUser(userCreateDto);
            if (_context.Users.Any(x => x.Email == user.Email) || _context.Users.Any(x => x.Name == user.Name)) {
                throw new UserAlreadyExistsException("User With this Email or Name already exists");
            }
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
                throw new UserNotFoundException("User not found with this Id");
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
                throw new UserNotFoundException("User not found with this Id");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        //User
        public async Task<string> GenerateToken(User user) {
            throw new NotImplementedException();
        }

        public async Task<ClaimsIdentity> GetClaimsIdentity(User user) {
            throw new NotImplementedException();
        }

        public async Task<string> Login(string email, string password) {
            throw new NotImplementedException();
        }

        //Role
        public async Task<int> CreateRole(RoleCreateDto roleCreateDto) {
            var role = MapToRole(roleCreateDto);
            if (_context.Roles.Any(x => x.Name == role.Name)) throw new RoleAlreadyExistsException("Role already Exists");

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role.Id;
        }

        public async Task<int> AddRoleToUser(int userId, int roleId) {
            throw new NotImplementedException();
        }

        public async Task<RoleDto?> GetRole(int roleId) {
            var role = _context.Roles.FirstOrDefault(x => x.Id == roleId);
            if (role == null) {
                throw new RoleNotFoundException("Role not found with this Id");
            }
            return MapToRoleDto(role);
        }

        public async Task<int> DeleteRole(int roleId) {
            throw new NotImplementedException();
        }

        public async Task<int> RemoveRoleFromUser(int userId, int roleId) {
            throw new NotImplementedException();
        }
    }
}
