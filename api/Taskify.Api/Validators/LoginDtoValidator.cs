using FluentValidation;
using Taskify.Api.Dtos;
<<<<<<< HEAD
=======

>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
namespace Taskify.Api.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
