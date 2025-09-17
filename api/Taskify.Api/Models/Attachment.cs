using System;

namespace Taskify.Api.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
    }
}
