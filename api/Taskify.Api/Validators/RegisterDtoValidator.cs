using FluentValidation;
using Taskify.Api.Dtos;
<<<<<<< HEAD
=======
using Taskify.Api.Validators;
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

namespace Taskify.Api.Validators
{
    public class RegisterDtoValidator : AbstractValidator<CreateUserDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit");
        }
    }
}
