using System;

namespace Taskify.Api.Dtos
{
    public class TaskDto
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;   // from enum
        public string Priority { get; set; } = string.Empty; // from enum

        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public int CreatedByUserId { get; set; }
        public string? CreatedByUsername { get; set; }
    }
}
