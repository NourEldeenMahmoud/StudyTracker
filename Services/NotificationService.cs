using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Models;

namespace StudyTracker.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int count = 20)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task AddAsync(string userId, string type, string message, string? relatedEntityType = null, string? relatedEntityId = null)
        {
            var n = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();
        }

        public async Task AddIfNotExistsAsync(string userId, string type, string relatedEntityId, string message)
        {
            var exists = await _context.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.UserId == userId && n.Type == type && n.RelatedEntityId == relatedEntityId);
            if (exists) return;

            await AddAsync(userId, type, message, null, relatedEntityId);
        }

        public async Task MarkAsReadAsync(string userId, Guid id)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n == null) return;
            n.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var items = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            if (!items.Any()) return;
            foreach (var n in items) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}

