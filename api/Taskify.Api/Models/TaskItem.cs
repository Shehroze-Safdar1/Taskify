using System;
using System.Collections.Generic;

namespace Taskify.Api.Models
{
    public enum TaskStatus { Todo, InProgress, Done }
    public enum TaskPriority { Low, Normal, High }

    public class TaskItem
    {
        public int Id { get; set; }

        // Project relation
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        // Task details
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relations with users
        public int CreatedByUserId { get; set; }
        public Users CreatedByUser { get; set; } = null!;

        public int? AssignedToUserId { get; set; }
        public Users? AssignedToUser { get; set; }

        // Many-to-many & attachments
        public ICollection<TaskTag>? TaskTags { get; set; }
        public ICollection<Attachment>? Attachments { get; set; }
    }
}
