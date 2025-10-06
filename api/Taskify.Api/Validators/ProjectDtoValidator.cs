using FluentValidation;
using Taskify.Api.Dtos;

namespace Taskify.Api.Validators
{
    public class ProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
        public ProjectDtoValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name should not be empty");

        }
    }
}
