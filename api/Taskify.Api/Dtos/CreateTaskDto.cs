using System;

namespace Taskify.Api.Dtos
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Priority { get; set; } // "Low", "Normal", "High"
        public string? Status { get; set; }   // "Todo", "InProgress", "Done"
        public int? AssignedToUserId { get; set; } // 🔹 add this
    }
}
