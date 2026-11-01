using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KulcsRendszer.Services.Services {

    public interface IUserService {
        //User
        Task<UserDto?> Get(int userId);
        Task<UserInfoDto?> GetInfo(int userId);
        Task<string> Create(UserCreateDto userCreateDto);
        Task<UserDto?> Update(int userId, UserUpdateDto userUpdateDto);
        Task<bool> ChangePassword(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> Delete(int userId);
        //Login
        Task<string> Login(string email, string password);
        //Role
        Task<int> CreateRole(RoleCreateDto roleCreateDto);
        Task<bool> AddRoleToUser(int userId, int roleId);
        Task<RoleDto?> GetRole(int roleId);
        Task<bool> DeleteRole(int roleId);
        Task<bool> RemoveRoleFromUser(int userId);
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

        public async Task<string> Create(UserCreateDto userCreateDto) {
            var user = MapToUser(userCreateDto);
            if (_context.Users.Any(x => x.Email == user.Email) || _context.Users.Any(x => x.Name == user.Name)) {
                throw new UserAlreadyExistsException("User With this Email or Name already exists");
            }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GenerateToken(user);
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecretKeyThatIsAtLeast32CharactersLong!!ADDSFHsfgsdgksgkaowaefagnamfaklfaijg"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(5));
            var id = await GetClaimsIdentity(user);
            var token = new JwtSecurityToken("http://localhost:5000", "http://localhost:3000", id.Claims, expires: expires, signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ClaimsIdentity> GetClaimsIdentity(User user) {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Sid, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.AuthTime, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))
            };

            if (user.Role != null) {
                claims.Add(new Claim("roleId", user.Role.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name.ToString()));
            }

            return await Task.FromResult(new ClaimsIdentity(claims, "Token"));
        }

        public async Task<string> Login(string email, string password) {
            var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) {
                throw new UserNotFoundException("Bad email or password");
            }
            //return MapToUserDto(user);
            return await GenerateToken(user);
        }

        //Role
        public async Task<int> CreateRole(RoleCreateDto roleCreateDto) {
            var role = MapToRole(roleCreateDto);
            if (_context.Roles.Any(x => x.Name == role.Name)) throw new RoleAlreadyExistsException("Role already Exists");

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role.Id;
        }

        public async Task<bool> AddRoleToUser(int userId, int roleId) {
            var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(u => u.Id == userId);
            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == roleId);

            if (user == null) {
                throw new UserNotFoundException("User not found with this Id: " + userId);
            }
            if (role == null) {
                throw new RoleNotFoundException("Role not found with this Id: " + roleId);
            }
            if (user.Role != null) {
                throw new RoleAlreadyExistsException("User already has role: " + user.Role.Name);
            }

            user.Role = role;
            user.RoleId = roleId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<RoleDto?> GetRole(int roleId) {
            var role = _context.Roles.FirstOrDefault(x => x.Id == roleId);
            if (role == null) {
                throw new RoleNotFoundException("Role not found with this Id");
            }
            return MapToRoleDto(role);
        }

        public async Task<bool> DeleteRole(int roleId) {
            var role = _context.Roles.FirstOrDefault(x => x.Id == roleId);
            if (role == null) {
                throw new RoleNotFoundException("Role not found with this Id");
            }
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromUser(int userId) {
            var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) {
                throw new UserNotFoundException("User not found with this Id: " + userId);
            }

            if (user.Role == null) {
                throw new RoleAlreadyExistsException("User doesent has role: ");
            }
            user.Role = null;
            user.RoleId = userId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
