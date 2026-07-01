using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudyTracker.Models;

namespace StudyTracker.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            try
            {
                // Don't create a new scope - use the provided service provider directly
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DataSeeder");

                // Create Roles
                string[] roleNames = { "Admin", "User", "SuperAdmin" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation($"Role '{roleName}' created successfully.");
                        }
                        else
                        {
                            logger.LogError($"Failed to create role '{roleName}'. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Role '{roleName}' already exists.");
                    }
                }

                // Create Admin User (also SuperAdmin)
                var adminEmail = "admin@studytracker.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "Admin User",
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin123!");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Admin user created successfully.");
                        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                        if (!roleResult.Succeeded)
                        {
                            logger.LogError($"Failed to assign Admin role. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }

                        var superAdminResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                        if (!superAdminResult.Succeeded)
                        {
                            logger.LogError($"Failed to assign SuperAdmin role. Errors: {string.Join(", ", superAdminResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        logger.LogError($"Failed to create admin user. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists.");

                    // Ensure roles are assigned (idempotent)
                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                        if (!roleResult.Succeeded)
                        {
                            logger.LogError($"Failed to ensure Admin role for admin user. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }

                    if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
                    {
                        var superAdminResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                        if (!superAdminResult.Succeeded)
                        {
                            logger.LogError($"Failed to ensure SuperAdmin role for admin user. Errors: {string.Join(", ", superAdminResult.Errors.Select(e => e.Description))}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DataSeeder");
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw; // Re-throw to be caught by Program.cs
            }
        }
    }
}
