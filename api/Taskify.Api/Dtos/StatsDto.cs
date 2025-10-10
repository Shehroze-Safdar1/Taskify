namespace Taskify.Api.Dtos
{
    public class StatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public string? MostUsedTag { get; set; }
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
    }

    public class ProjectStatsDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public double CompletionRate { get; set; }
        public string? MostUsedTag { get; set; }
        public DateTime? ProjectCreatedAt { get; set; }
        public DateTime? LastTaskCreated { get; set; }
    }
}
