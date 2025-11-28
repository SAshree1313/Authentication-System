using Backend.DTOs.Passkey;
using FluentValidation;

namespace Backend.Validators.Register
{
    public class PasskeyRegisterRequestValidator : AbstractValidator<PasskeyRegisterBeginRequestDto>
    {
        public PasskeyRegisterRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}
