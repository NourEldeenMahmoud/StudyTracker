using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Models;

namespace StudyTracker.Services
{
    public class SecurityLogService : ISecurityLogService
    {
        private readonly IApplicationDbContext _context;

        public SecurityLogService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogUnauthorizedDeleteAsync(ClaimsPrincipal user, string action, string targetType, string? targetId, string? ipAddress)
        {
            string? userId = null;
            string? email = null;

            if (user.Identity?.IsAuthenticated == true)
            {
                userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity.Name;
            }

            var entry = new SecurityLog
            {
                UserId = userId,
                Email = email,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                IpAddress = ipAddress,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                _context.SecurityLogs.Add(entry);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Swallow logging failures – app behaviour should not depend on audit log.
            }
        }
    }
}

