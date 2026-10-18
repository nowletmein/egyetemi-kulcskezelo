using System.ComponentModel.DataAnnotations;
using KulcsRendszer.DataContext.Validation;

namespace KulcsRendszer.DataContext.Dtos {
    public class RegisterDto : IValidatableObject {
        [Required]
        public string Name { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            var validator = new PasswordValidator();

            foreach (var error in validator.Validate(Password)) {
                yield return new ValidationResult(error, new[] { nameof(Password) });
            }
        }
    }

    public class LoginDto {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public class AuthResponseDto {
        public string Token { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = new();
    }
}
