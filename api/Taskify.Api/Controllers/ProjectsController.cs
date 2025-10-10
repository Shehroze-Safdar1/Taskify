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
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public ProjectsController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/projects -> returns projects for current user; admin sees all
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

                var query = _db.Projects
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks)
                    .AsQueryable();

                if (!isAdmin)
                    query = query.Where(p => p.OwnerId == userId);

                var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
                var dto = _mapper.Map<IEnumerable<ProjectDto>>(list);

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, "Viewed projects list");
=======
                await LogActivity(userId, "Viewed projects list", "Project");
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

        // GET: api/projects/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var project = await _db.Projects
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && project.OwnerId != userId)
                    return NotFound(); // don't reveal existence

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Viewed project {id}");
=======
                await LogActivity(userId, $"Viewed project {id}", "Project", id);
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

                return Ok(_mapper.Map<ProjectDto>(project));
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

        // POST: api/projects
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var project = _mapper.Map<Project>(dto);
                project.OwnerId = userId;
                project.CreatedAt = DateTime.UtcNow;

                _db.Projects.Add(project);
                await _db.SaveChangesAsync();

                // reload with Owner for mapping
                await _db.Entry(project).Reference(p => p.Owner).LoadAsync();

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Created project {project.Id}");
=======
                await LogActivity(userId, $"Created project {project.Id}", "Project", project.Id);
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4

                var result = _mapper.Map<ProjectDto>(project);
                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, result);
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

        // PUT: api/projects/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] CreateProjectDto dto)
        {
            try
            {
                var project = await _db.Projects.FindAsync(id);
                if (project == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && project.OwnerId != userId)
                    return Forbid();

                _mapper.Map(dto, project);
                _db.Projects.Update(project);
                await _db.SaveChangesAsync();

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Updated project {id}");
=======
                await LogActivity(userId, $"Updated project {id}", "Project", id);
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

        // DELETE: api/projects/{id} -> only Admins or project owner
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _db.Projects.FindAsync(id);
                if (project == null) return NotFound();

                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                if (!isAdmin && project.OwnerId != userId)
                    return Forbid();

                _db.Projects.Remove(project);
                await _db.SaveChangesAsync();

<<<<<<< HEAD
                // 🔹 Log activity
                await LogActivity(userId, $"Deleted project {id}");
=======
                await LogActivity(userId, $"Deleted project {id}", "Project", id);
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

        // 🔹 Reusable logging helper
<<<<<<< HEAD
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
