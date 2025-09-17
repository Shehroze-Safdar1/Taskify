using System.Linq;
using Taskify.Api.Models;

namespace Taskify.Api.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            // Check if we already have users, if so, add a new admin if needed
            var existingAdmin = context.Users.FirstOrDefault(u => u.Role == "Admin");
            
            if (existingAdmin == null)
            {
                var admin = new Users
                {
                    Username = "superadmin",
                    Email = "superadmin@taskify.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123!"),
                    Role = "Admin"
                };

                context.Users.Add(admin);
                context.SaveChanges();

                // Create sample task for the new admin
                var sampleTask = new TaskItem
                {
                    Title = "Welcome: seeded task",
                    Description = "This task was created by the seeder.",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow
                };

                context.Tasks.Add(sampleTask);
                context.SaveChanges();
            }
        }
    }
}
