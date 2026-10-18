using KulcsRendszer.DataContext.Validation;

namespace KulcsRendszer.Tests {
    public class PasswordValidatorTests {
        private readonly PasswordValidator _validator = new();

        [Fact]
        public void ValidateReturnsNoErrorsForValidPassword() {
            var errors = _validator.Validate("ValidPassword1!");

            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateReturnsMinimumLengthMessage() {
            var errors = _validator.Validate("Aa1!");

            Assert.Contains("Password must be at least 12 characters long.", errors);
        }

        [Fact]
        public void ValidateReturnsMaximumLengthMessage() {
            var errors = _validator.Validate(new string('A', 129) + "a1!");

            Assert.Contains("Password must be no more than 128 characters long.", errors);
        }

        [Fact]
        public void ValidateReturnsLowercaseMessage() {
            var errors = _validator.Validate("VALIDPASSWORD1!");

            Assert.Contains("Password must contain at least one lowercase letter.", errors);
        }

        [Fact]
        public void ValidateReturnsUppercaseMessage() {
            var errors = _validator.Validate("validpassword1!");

            Assert.Contains("Password must contain at least one uppercase letter.", errors);
        }

        [Fact]
        public void ValidateReturnsDigitMessage() {
            var errors = _validator.Validate("ValidPassword!");

            Assert.Contains("Password must contain at least one digit.", errors);
        }

        [Fact]
        public void ValidateReturnsSpecialCharacterMessage() {
            var errors = _validator.Validate("ValidPassword1");

            Assert.Contains("Password must contain at least one special character.", errors);
        }
    }
}
