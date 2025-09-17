using AutoMapper;
using Taskify.Api.Models;
using Taskify.Api.Dtos;

namespace Taskify.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<CreateUserDto, Users>()
                .ForMember(d => d.PasswordHash, opt => opt.Ignore()) // hashed later
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Projects, opt => opt.Ignore())
                .ForMember(d => d.CreatedTasks, opt => opt.Ignore())
                .ForMember(d => d.AssignedTasks, opt => opt.Ignore());

            CreateMap<Users, UserDto>();

            // Task mappings
            CreateMap<CreateTaskDto, TaskItem>()
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedByUser, opt => opt.Ignore())
                .ForMember(d => d.AssignedToUserId, opt => opt.Ignore())
                .ForMember(d => d.AssignedToUser, opt => opt.Ignore())
                .ForMember(d => d.TaskTags, opt => opt.Ignore())
                .ForMember(d => d.Attachments, opt => opt.Ignore())
                .ForMember(d => d.Priority, opt => opt.MapFrom(src => ParsePriority(src.Priority)))
                .ForMember(d => d.Status, opt => opt.MapFrom(src => ParseStatus(src.Status)));

            CreateMap<TaskItem, TaskDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(d => d.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(d => d.CreatedByUsername, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Username : null));

            // Project mappings
            CreateMap<CreateProjectDto, Project>()
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.OwnerId, opt => opt.Ignore())
                .ForMember(d => d.Owner, opt => opt.Ignore())
                .ForMember(d => d.Tasks, opt => opt.Ignore());

            CreateMap<Project, ProjectDto>()
                .ForMember(d => d.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : null))
                .ForMember(d => d.TaskCount, opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count : 0));
        }

        // ✅ Safe enum parser (fallback = Normal)
        private static Models.TaskPriority ParsePriority(string? value)
        {
            if (Enum.TryParse<Models.TaskPriority>(value, true, out var parsed))
                return parsed;

            return Models.TaskPriority.Normal;
        }

        // ✅ Safe enum parser (fallback = Todo)
        private static Models.TaskStatus ParseStatus(string? value)
        {
            if (Enum.TryParse<Models.TaskStatus>(value, true, out var parsed))
                return parsed;

            return Models.TaskStatus.Todo;
        }
    }
}
