using AutoMapper;
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
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public TasksController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/tasks  -> returns tasks for current user; admin sees all
        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

                var query = _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .AsQueryable();

                if (!isAdmin)
                    query = query.Where(t => t.CreatedByUserId == userId);

                var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
                var dto = _mapper.Map<IEnumerable<TaskDto>>(list);

                // 🔹 Log activity
                await LogActivity(userId, "Viewed tasks list");

                return Ok(dto);
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

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            try
            {
                var task = await _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && task.CreatedByUserId != userId)
                    return NotFound(); // don't reveal existence

                // 🔹 Log activity
                await LogActivity(userId, $"Viewed task {id}");

                return Ok(_mapper.Map<TaskDto>(task));
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

        // POST: api/tasks
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Task data is required");

                if (string.IsNullOrWhiteSpace(dto.Title))
                    return BadRequest("Task title is required");

                var userId = GetCurrentUserId();

                // Validate ProjectId if provided
                if (dto.ProjectId.HasValue)
                {
                    var projectExists = await _db.Projects.AnyAsync(p => p.Id == dto.ProjectId.Value);
                    if (!projectExists)
                        return BadRequest($"Project with ID {dto.ProjectId.Value} does not exist");
                }

                var task = _mapper.Map<TaskItem>(dto);
                task.CreatedByUserId = userId;
                task.CreatedAt = DateTime.UtcNow;

                _db.Tasks.Add(task);
                await _db.SaveChangesAsync();

                var createdTask = await _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);

                if (createdTask == null)
                    return StatusCode(500, "Failed to retrieve created task");

                // 🔹 Log activity
                await LogActivity(userId, $"Created task {task.Id}");

                var result = _mapper.Map<TaskDto>(createdTask);
                return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating task: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] CreateTaskDto dto)
        {
            try
            {
                var task = await _db.Tasks.FindAsync(id);
                if (task == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && task.CreatedByUserId != userId)
                    return Forbid();

                _mapper.Map(dto, task);
                _db.Tasks.Update(task);
                await _db.SaveChangesAsync();

                // 🔹 Log activity
                await LogActivity(userId, $"Updated task {id}");

                return NoContent();
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

        // DELETE: api/tasks/{id}  -> only Admins
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,admin")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _db.Tasks.FindAsync(id);
                if (task == null) return NotFound();

                _db.Tasks.Remove(task);
                await _db.SaveChangesAsync();

                // 🔹 Log activity
                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Deleted task {id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            throw new UnauthorizedAccessException("Invalid user claim");
        }

        // POST: api/tasks/{taskId}/tags
        [HttpPost("{taskId}/tags")]
        public async Task<IActionResult> AssignTagsToTask(int taskId, [FromBody] List<int> tagIds)
        {
            try
            {
                var task = await _db.Tasks
                    .Include(t => t.TaskTags)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && task.CreatedByUserId != userId)
                    return Forbid();

                // Validate that all tag IDs exist
                if (tagIds != null && tagIds.Any())
                {
                    var existingTagIds = await _db.Tags
                        .Where(t => tagIds.Contains(t.Id))
                        .Select(t => t.Id)
                        .ToListAsync();

                    var invalidTagIds = tagIds.Except(existingTagIds).ToList();
                    if (invalidTagIds.Any())
                        return BadRequest($"Invalid tag IDs: {string.Join(", ", invalidTagIds)}");
                }

                // Clear existing tags
                task.TaskTags.Clear();

                // Add new ones
                if (tagIds != null && tagIds.Any())
                {
                    foreach (var tagId in tagIds)
                    {
                        task.TaskTags.Add(new TaskTag { TaskId = taskId, TagId = tagId });
                    }
                }

                await _db.SaveChangesAsync();

                // Log activity
                var tagNames = tagIds != null && tagIds.Any() 
                    ? string.Join(", ", await _db.Tags.Where(t => tagIds.Contains(t.Id)).Select(t => t.Name).ToListAsync())
                    : "none";
                await LogActivity(userId, $"Assigned tags to task {taskId}: {tagNames}");

                return NoContent();
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

        // 🔹 Shared logging helper
        private async Task LogActivity(int userId, string action)
        {
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
