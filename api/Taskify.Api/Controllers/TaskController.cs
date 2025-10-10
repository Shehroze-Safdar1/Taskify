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
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public TasksController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

                var query = _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .AsQueryable();

                if (!isAdmin)
                    query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);

                var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
                var dto = _mapper.Map<IEnumerable<TaskDto>>(list);

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, "Viewed tasks list");
=======
                await LogActivity(userId, "Viewed tasks list", "Task");
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            try
            {
                var task = await _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
                    return NotFound();

                await LogActivity(userId, $"Viewed task {id}", "Task", id);

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

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            try
            {
<<<<<<< HEAD
                if (dto == null)
=======
                if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
                    return BadRequest("Task data is required");

                var userId = GetCurrentUserId();

                if (dto.ProjectId.HasValue && !await _db.Projects.AnyAsync(p => p.Id == dto.ProjectId.Value))
                    return BadRequest($"Project with ID {dto.ProjectId.Value} does not exist");

                if (dto.AssignedToUserId.HasValue && !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId.Value))
                    return BadRequest($"Assigned user with ID {dto.AssignedToUserId.Value} does not exist");

                var task = _mapper.Map<TaskItem>(dto);
                task.CreatedByUserId = userId;
                task.CreatedAt = DateTime.UtcNow;

                _db.Tasks.Add(task);
                await _db.SaveChangesAsync();

<<<<<<< HEAD
                var createdTask = await _db.Tasks
                    .Include(t => t.CreatedByUser)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);
=======
                await _db.Entry(task).Reference(t => t.CreatedByUser).LoadAsync();
                if (task.AssignedToUserId.HasValue)
                    await _db.Entry(task).Reference(t => t.AssignedToUser).LoadAsync();
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

                await LogActivity(userId, $"Created task {task.Id}", "Task", task.Id);

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Created task {task.Id}");

                var result = _mapper.Map<TaskDto>(createdTask);
                return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, result);
=======
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, _mapper.Map<TaskDto>(task));
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Console.WriteLine($"Error creating task: {ex}");
=======
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] CreateTaskDto dto)
        {
            try
            {
                var task = await _db.Tasks.FindAsync(id);
                if (task == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
                    return Forbid();

<<<<<<< HEAD
=======
                if (dto.AssignedToUserId.HasValue && !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId.Value))
                    return BadRequest($"Assigned user with ID {dto.AssignedToUserId.Value} does not exist");

>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
                _mapper.Map(dto, task);
                await _db.SaveChangesAsync();

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Updated task {id}");
=======
                await LogActivity(userId, $"Updated task {id}", "Task", id);
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

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

<<<<<<< HEAD
                // 🔹 Log activity
                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Deleted task {id}");
=======
                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Deleted task {id}", "Task", id);
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

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

<<<<<<< HEAD
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
=======
        private async Task LogActivity(int userId, string action, string entityType = "", int entityId = 0)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow
            };

            _db.ActivityLogs.Add(log);
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
            await _db.SaveChangesAsync();
        }
    }
}
