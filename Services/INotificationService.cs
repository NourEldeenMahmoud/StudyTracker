using StudyTracker.Models;

namespace StudyTracker.Services
{
    public interface INotificationService
    {
        Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int count = 20);
        Task<int> GetUnreadCountAsync(string userId);
        Task AddAsync(string userId, string type, string message, string? relatedEntityType = null, string? relatedEntityId = null);
        Task AddIfNotExistsAsync(string userId, string type, string relatedEntityId, string message);
        Task MarkAsReadAsync(string userId, Guid id);
        Task MarkAllAsReadAsync(string userId);
    }
}

