using System;
using System.Threading.Tasks;
using Taskify.Api.Data;
using Taskify.Api.Models;

namespace Taskify.Api.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string entityType, int entityId, string action, int userId);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;

        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string entityType, int entityId, string action, int userId)
        {
            var log = new ActivityLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
