using Backend.DTOs.Passkey;
using FluentValidation;

namespace Backend.Validators.Login
{
    public class PasskeyLoginRequestValidator : AbstractValidator<PasskeyLoginBeginRequestDto>
    {
        public PasskeyLoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}
