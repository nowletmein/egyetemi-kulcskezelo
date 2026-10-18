using System.Text.Json;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Enums;

namespace KulcsRendszer.Tests {
    public class AuthResponseDtoTests {
        [Fact]
        public void AuthResponseDtoSerializesWithoutEntityProperties() {
            var response = new AuthResponseDto {
                Token = "token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfoDto {
                    Id = 1,
                    Name = "Test User",
                    Email = "user@example.com",
                    Role = UserRole.Administrator.ToString()
                }
            };

            var json = JsonSerializer.Serialize(response);

            Assert.Contains("\"Token\":\"token\"", json);
            Assert.Contains("\"Role\":\"Administrator\"", json);
            Assert.DoesNotContain("PasswordHash", json);
            Assert.DoesNotContain("Users", json);
            Assert.DoesNotContain("RoleId", json);
        }

        [Fact]
        public void AuthResponseDtoSupportsNullRole() {
            var response = new AuthResponseDto {
                Token = "token",
                User = new UserInfoDto {
                    Id = 1,
                    Name = "Test User",
                    Email = "user@example.com"
                }
            };

            var exception = Record.Exception(() => JsonSerializer.Serialize(response));

            Assert.Null(exception);
        }
    }
}
