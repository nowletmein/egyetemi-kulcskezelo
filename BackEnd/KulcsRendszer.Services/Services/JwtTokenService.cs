using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KulcsRendszer.DataContext.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KulcsRendszer.Services.Services {
    public class JwtSettings {
        public string Key { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpirationMinutes { get; set; } = 60;
    }

    public interface IJwtTokenService {
        string CreateToken(User user);
    }

    public class JwtTokenService : IJwtTokenService {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> settings) {
            _settings = settings.Value;
        }

        public string CreateToken(User user) {
            if (string.IsNullOrWhiteSpace(_settings.Key)) {
                throw new InvalidOperationException("JWT signing key is not configured.");
            }

            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email)
            };

            if (user.Role != null) {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name.ToString()));
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
