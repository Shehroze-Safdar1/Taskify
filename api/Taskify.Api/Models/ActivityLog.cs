using System;

namespace Taskify.Api.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        // What entity was affected (User, Project, Task, etc.)
        public string EntityType { get; set; } = string.Empty;

        // The primary key of the affected entity
        public int EntityId { get; set; }

        // Action performed (Create, Update, Delete)
        public string Action { get; set; } = string.Empty;

        // Who performed the action
        public int UserId { get; set; }
        public Users User { get; set; }

        // When it happened
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
