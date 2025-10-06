using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Taskify.Api.Data;
using Taskify.Api.Models;

namespace Taskify.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // must be authenticated
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tags
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetTags()
        {
            try
            {
                var tags = await _context.Tags.ToListAsync();
                
                // Log activity
                var userId = GetCurrentUserId();
                await LogActivity(userId, "Viewed tags list");
                
                return Ok(tags);
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

        // POST: api/tags
        [HttpPost]
        public async Task<ActionResult<Tag>> CreateTag([FromBody] Tag tag)
        {
            try
            {
                if (tag == null)
                    return BadRequest("Tag data is required");

                if (string.IsNullOrWhiteSpace(tag.Name))
                    return BadRequest("Tag name is required");

                // Check if tag with same name already exists
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tag.Name.ToLower());
                
                if (existingTag != null)
                    return BadRequest("A tag with this name already exists");

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                // Log activity
                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Created tag '{tag.Name}'");

                return CreatedAtAction(nameof(GetTags), new { id = tag.Id }, tag);
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

        // DELETE: api/tags/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null) 
                    return NotFound();

                // Check if tag is being used by any tasks
                var isUsed = await _context.TaskTags.AnyAsync(tt => tt.TagId == id);
                if (isUsed)
                    return BadRequest("Cannot delete tag that is assigned to tasks");

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                // Log activity
                var userId = GetCurrentUserId();
                await LogActivity(userId, $"Deleted tag '{tag.Name}'");

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
