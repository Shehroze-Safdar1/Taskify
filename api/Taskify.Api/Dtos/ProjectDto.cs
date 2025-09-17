using System;

namespace Taskify.Api.Dtos
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public string? OwnerUsername { get; set; }
        public int TaskCount { get; set; }
    }
}
