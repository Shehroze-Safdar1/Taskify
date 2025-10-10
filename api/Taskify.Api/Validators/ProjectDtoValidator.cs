using FluentValidation;
using Taskify.Api.Dtos;

namespace Taskify.Api.Validators
{
    public class ProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
        public ProjectDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MaximumLength(100).WithMessage("Name must be less than 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must be less than 500 characters");
        }
    }
}
