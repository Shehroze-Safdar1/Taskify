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

                await LogActivity(userId, "Viewed tasks list", "Task");

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
                if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
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

                await _db.Entry(task).Reference(t => t.CreatedByUser).LoadAsync();
                if (task.AssignedToUserId.HasValue)
                    await _db.Entry(task).Reference(t => t.AssignedToUser).LoadAsync();

                await LogActivity(userId, $"Created task {task.Id}", "Task", task.Id);

                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, _mapper.Map<TaskDto>(task));
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

                if (dto.AssignedToUserId.HasValue && !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId.Value))
                    return BadRequest($"Assigned user with ID {dto.AssignedToUserId.Value} does not exist");

                _mapper.Map(dto, task);
                await _db.SaveChangesAsync();

                await LogActivity(userId, $"Updated task {id}", "Task", id);

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

                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Deleted task {id}", "Task", id);

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
            await _db.SaveChangesAsync();
        }
    }
}
