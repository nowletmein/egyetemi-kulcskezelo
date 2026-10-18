using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KulcsRendszer.Services.Services {
    public sealed class InitialAdminSettings {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public interface IInitialAdminService {
        Task EnsureAdministratorAsync(InitialAdminSettings settings, CancellationToken cancellationToken);
    }

    public sealed class InitialAdminService : IInitialAdminService {
        private readonly KulcsRendszerDbContext _context;
        private readonly ILogger<InitialAdminService> _logger;

        public InitialAdminService(
            KulcsRendszerDbContext context,
            ILogger<InitialAdminService> logger) {
            _context = context;
            _logger = logger;
        }

        public async Task EnsureAdministratorAsync(
            InitialAdminSettings settings,
            CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password)) {
                _logger.LogWarning("Initial administrator configuration is missing. No administrator was created.");
                return;
            }

            var administratorRole = await _context.Roles
                .FirstOrDefaultAsync(role => role.Name == UserRole.Administrator, cancellationToken);

            if (administratorRole == null) {
                _logger.LogWarning("The Administrator role is not configured. No administrator was created.");
                return;
            }

            var administratorExists = await _context.Users
                .AnyAsync(user => user.RoleId == administratorRole.Id, cancellationToken);

            if (administratorExists) {
                return;
            }

            _context.Users.Add(new User {
                Name = "Initial Administrator",
                Email = NormalizeEmail(settings.Email),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.Password),
                RoleId = administratorRole.Id
            });

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("The initial administrator was created.");
        }

        private static string NormalizeEmail(string email) {
            return email.Trim().ToLowerInvariant();
        }
    }
}
