using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Services;
using Microsoft.Extensions.Options;

namespace KulcsRendszer.Tests {
    public class JwtTokenServiceTests {
        [Fact]
        public void CreateTokenIncludesUserAndRoleClaims() {
            var service = CreateService();
            var token = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(CreateUser()));

            Assert.Equal("42", token.Claims.First(item => item.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("Test User", token.Claims.First(item => item.Type == ClaimTypes.Name).Value);
            Assert.Equal("user@example.com", token.Claims.First(item => item.Type == ClaimTypes.Email).Value);
            Assert.Equal(UserRole.Instructor.ToString(), token.Claims.First(item => item.Type == ClaimTypes.Role).Value);
        }

        [Fact]
        public void CreateTokenSetsConfiguredExpiry() {
            var service = CreateService();
            var before = DateTime.UtcNow.AddMinutes(59);
            var after = DateTime.UtcNow.AddMinutes(61);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(CreateUser()));

            Assert.True(token.ValidTo > before);
            Assert.True(token.ValidTo < after);
        }

        private static JwtTokenService CreateService() {
            return new JwtTokenService(Options.Create(new JwtSettings {
                Key = "development-test-key-with-at-least-32-characters",
                Issuer = "KulcsRendszer.API",
                Audience = "KulcsRendszer.Client",
                ExpirationMinutes = 60
            }));
        }

        private static User CreateUser() {
            return new User {
                Id = 42,
                Name = "Test User",
                Email = "user@example.com",
                Role = new Role { Id = 4, Name = UserRole.Instructor }
            };
        }
    }
}
