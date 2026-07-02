using System.Security.Claims;

namespace StudyTracker.Services
{
    public interface ISecurityLogService
    {
        Task LogUnauthorizedDeleteAsync(ClaimsPrincipal user, string action, string targetType, string? targetId, string? ipAddress);
    }
}

