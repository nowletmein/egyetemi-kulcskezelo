namespace KulcsRendszer.DataContext.Validation {
    public sealed class PasswordValidator {
        public IReadOnlyList<string> Validate(string password) {
            var errors = new List<string>();

            if (password.Length < 12) {
                errors.Add("Password must be at least 12 characters long.");
            }

            if (password.Length > 128) {
                errors.Add("Password must be no more than 128 characters long.");
            }

            if (!password.Any(char.IsLower)) {
                errors.Add("Password must contain at least one lowercase letter.");
            }

            if (!password.Any(char.IsUpper)) {
                errors.Add("Password must contain at least one uppercase letter.");
            }

            if (!password.Any(char.IsDigit)) {
                errors.Add("Password must contain at least one digit.");
            }

            if (!password.Any(character => !char.IsLetterOrDigit(character))) {
                errors.Add("Password must contain at least one special character.");
            }

            return errors;
        }
    }
}
