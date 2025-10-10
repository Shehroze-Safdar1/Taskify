using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Taskify.Api.Data;
using Taskify.Api.Dtos;
using Taskify.Api.Models;

namespace Taskify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // must be authenticated
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/stats/overview
        [HttpGet("overview")]
        public async Task<ActionResult<StatsDto>> GetOverviewStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

                // Base query for tasks - admin sees all, regular users see only their tasks
                var tasksQuery = _context.Tasks
                    .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                    .AsQueryable();

                if (!isAdmin)
                    tasksQuery = tasksQuery.Where(t => t.CreatedByUserId == userId);

                var tasks = await tasksQuery.ToListAsync();

                // Calculate basic stats
                var totalTasks = tasks.Count;
                var completedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Done);
                var pendingTasks = tasks.Count(t => t.Status == Models.TaskStatus.Todo);
                var inProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress);

                // Calculate completion rate
                var completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

                // Get most used tag
                var mostUsedTag = await GetMostUsedTag(tasksQuery);

                // Get project stats
                var projectsQuery = _context.Projects.AsQueryable();
                if (!isAdmin)
                    projectsQuery = projectsQuery.Where(p => p.OwnerId == userId);

                var totalProjects = await projectsQuery.CountAsync();
                var activeProjects = await projectsQuery
                    .Where(p => _context.Tasks.Any(t => t.ProjectId == p.Id && t.Status != Models.TaskStatus.Done))
                    .CountAsync();

                // Tasks by priority
                var tasksByPriority = tasks
                    .GroupBy(t => t.Priority.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // Tasks by status
                var tasksByStatus = tasks
                    .GroupBy(t => t.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                var stats = new StatsDto
                {
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    PendingTasks = pendingTasks,
                    InProgressTasks = inProgressTasks,
                    MostUsedTag = mostUsedTag,
                    TotalProjects = totalProjects,
                    ActiveProjects = activeProjects,
                    CompletionRate = Math.Round(completionRate, 2),
                    TasksByPriority = tasksByPriority,
                    TasksByStatus = tasksByStatus
                };

                // Log activity
                await LogActivity(userId, "Viewed dashboard overview stats");

                return Ok(stats);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/stats/project/{id}
        [HttpGet("project/{id}")]
        public async Task<ActionResult<ProjectStatsDto>> GetProjectStats(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

                // Get project
                var projectQuery = _context.Projects.AsQueryable();
                if (!isAdmin)
                    projectQuery = projectQuery.Where(p => p.OwnerId == userId);

                var project = await projectQuery.FirstOrDefaultAsync(p => p.Id == id);
                if (project == null)
                    return NotFound();

                // Get tasks for this project
                var tasksQuery = _context.Tasks
                    .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                    .Where(t => t.ProjectId == id)
                    .AsQueryable();

                if (!isAdmin)
                    tasksQuery = tasksQuery.Where(t => t.CreatedByUserId == userId);

                var tasks = await tasksQuery.ToListAsync();

                // Calculate project stats
                var totalTasks = tasks.Count;
                var completedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Done);
                var pendingTasks = tasks.Count(t => t.Status == Models.TaskStatus.Todo);
                var inProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress);

                var completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

                // Get most used tag for this project
                var mostUsedTag = await GetMostUsedTagForProject(tasksQuery);

                // Get last task created date
                var lastTaskCreated = tasks.Any() ? tasks.Max(t => t.CreatedAt) : (DateTime?)null;

                var projectStats = new ProjectStatsDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    PendingTasks = pendingTasks,
                    InProgressTasks = inProgressTasks,
                    CompletionRate = Math.Round(completionRate, 2),
                    MostUsedTag = mostUsedTag,
                    ProjectCreatedAt = project.CreatedAt,
                    LastTaskCreated = lastTaskCreated
                };

                // Log activity
                await LogActivity(userId, $"Viewed stats for project {project.Name}");

                return Ok(projectStats);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<string?> GetMostUsedTag(IQueryable<TaskItem> tasksQuery)
        {
            var tagUsage = await _context.TaskTags
                .Where(tt => tasksQuery.Any(t => t.Id == tt.TaskId))
                .GroupBy(tt => tt.Tag.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            return tagUsage;
        }

        private async Task<string?> GetMostUsedTagForProject(IQueryable<TaskItem> tasksQuery)
        {
            var tagUsage = await _context.TaskTags
                .Where(tt => tasksQuery.Any(t => t.Id == tt.TaskId))
                .GroupBy(tt => tt.Tag.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            return tagUsage;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            throw new UnauthorizedAccessException("Invalid user claim");
        }

        // Shared logging helper
        private async Task LogActivity(int userId, string action)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
