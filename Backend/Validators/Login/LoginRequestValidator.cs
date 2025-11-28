// using Backend.DTOs.Login;
// using FluentValidation;

// namespace Backend.Validators.Login
// {
//     public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
//     {
//         public LoginRequestValidator()
//         {
//             RuleFor(x => x.Email)
//                 .NotEmpty().WithMessage("Email is required.")
//                 .EmailAddress().WithMessage("Invalid email format.");
                
//             // Note: Password validation is commented out for passkey-only authentication
//             // RuleFor(x => x.Password)
//             //     .NotEmpty().WithMessage("Password is required.");
//         }
//     }
// }
