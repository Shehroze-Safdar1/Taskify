using FluentValidation;
using Taskify.Api.Dtos;

<<<<<<< HEAD
=======

>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
namespace Taskify.Api.Validators
{
    public class ProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
<<<<<<< HEAD
        public ProjectDtoValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name should not be empty");

=======
        public ProjectDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Task name is required")
                .MaximumLength(100).WithMessage("Name must be less than 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must be less than 500 characters");
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
        }
    }
}
