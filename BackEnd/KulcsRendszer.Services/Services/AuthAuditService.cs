using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;

namespace KulcsRendszer.Services.Services {
    public static class AuthAuditEvent {
        public const string RegistrationSucceeded = "RegistrationSucceeded";
        public const string RegistrationFailed = "RegistrationFailed";
        public const string LoginSucceeded = "LoginSucceeded";
        public const string LoginFailed = "LoginFailed";
    }

    public interface IAuthAuditService {
        Task LogAsync(string eventType, string email, string? ipAddress, CancellationToken cancellationToken);
    }

    public sealed class AuthAuditService : IAuthAuditService {
        private readonly KulcsRendszerDbContext _context;

        public AuthAuditService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task LogAsync(
            string eventType,
            string email,
            string? ipAddress,
            CancellationToken cancellationToken) {
            _context.AuthAuditLogs.Add(new AuthAuditLog {
                EventType = eventType,
                EmailIdentifier = MaskEmail(email),
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string MaskEmail(string email) {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var separatorIndex = normalizedEmail.IndexOf('@');

            if (separatorIndex <= 0 || separatorIndex == normalizedEmail.Length - 1) {
                return "***";
            }

            var localPart = normalizedEmail[..separatorIndex];
            var domain = normalizedEmail[(separatorIndex + 1)..];
            return $"{localPart[0]}***@{domain}";
        }
    }
}
