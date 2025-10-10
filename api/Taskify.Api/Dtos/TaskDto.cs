public class TaskDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUsername { get; set; }

    // ✅ Add these for assigned user
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUsername { get; set; }
}
