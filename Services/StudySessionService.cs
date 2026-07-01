using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class StudySessionService : IStudySessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public StudySessionService(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        public async Task<StudySession> AddSessionAsync(string userId, AddSessionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Notes))
                throw new InvalidOperationException("Please describe what you studied.");

            var source = string.IsNullOrWhiteSpace(model.Source) ? "manual" : model.Source.Trim();
            var isTimer = string.Equals(source, "timer", StringComparison.OrdinalIgnoreCase);

            // Timer sessions always use Cairo "today" so Date matches CreatedAt in local display and daily totals.
            var sessionDate = isTimer ? TimeZoneHelper.GetTodayInCairo() : model.Date;

            if (isTimer && !string.IsNullOrWhiteSpace(model.TimerRunId))
            {
                var dedupeKey = $"StudyTimerLog:{userId}:{model.TimerRunId.Trim()}";
                if (_memoryCache.TryGetValue(dedupeKey, out StudySession? cachedSession) && cachedSession != null)
                    return cachedSession;
            }

            // Validate: Max 16 hours per session
            if (model.DurationMinutes > 16 * 60)
                throw new InvalidOperationException("Session duration cannot exceed 16 hours.");

            // Validate: Max 16 hours per day
            var dailyTotal = await GetDailyTotalAsync(userId, sessionDate);
            if (dailyTotal + model.DurationMinutes > 16 * 60)
            {
                throw new InvalidOperationException("Cannot exceed 16 hours per day.");
            }

            // Validate: No future dates (use Cairo today)
            var today = TimeZoneHelper.GetTodayInCairo();
            if (sessionDate > today)
            {
                throw new InvalidOperationException("Cannot add sessions for future dates.");
            }

            var session = new StudySession
            {
                UserId = userId,
                Date = sessionDate,
                DurationMinutes = model.DurationMinutes,
                Notes = model.Notes,
                Source = source,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();

            if (isTimer && !string.IsNullOrWhiteSpace(model.TimerRunId))
            {
                var dedupeKey = $"StudyTimerLog:{userId}:{model.TimerRunId.Trim()}";
                _memoryCache.Set(dedupeKey, session, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48)
                });
            }

            return session;
        }

        public async Task<List<StudySessionViewModel>> GetUserSessionsAsync(string userId, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            var query = _context.StudySessions.Where(s => s.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.Date <= endDate.Value);
            }

            return await query
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new StudySessionViewModel
                {
                    Id = s.Id,
                    Date = s.Date,
                    DurationMinutes = s.DurationMinutes,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<StudySession?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _context.StudySessions.FindAsync(sessionId);
        }

        public async Task<bool> UpdateSessionAsync(Guid sessionId, string userId, AddSessionViewModel model, bool isAdmin = false)
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
                return false;

            if (!isAdmin && session.UserId != userId)
                return false;

            // Validate: Max 16 hours per session
            if (model.DurationMinutes > 16 * 60)
                throw new InvalidOperationException("Session duration cannot exceed 16 hours.");

            // Validate: Max 16 hours per day (excluding current session)
            // Use session.UserId for validation, not the parameter userId (important for admin edits)
            var dailyTotal = await GetDailyTotalAsync(session.UserId, model.Date);
            if (session.Date == model.Date)
            {
                dailyTotal -= session.DurationMinutes;
            }
            if (dailyTotal + model.DurationMinutes > 16 * 60)
            {
                throw new InvalidOperationException("Cannot exceed 16 hours per day.");
            }

            session.Date = model.Date;
            session.DurationMinutes = model.DurationMinutes;
            session.Notes = model.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSessionAsync(Guid sessionId, string userId, bool isAdmin = false)
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
                return false;

            if (!isAdmin && session.UserId != userId)
                return false;

            _context.StudySessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetDailyTotalAsync(string userId, DateOnly date)
        {
            return await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date == date)
                .SumAsync(s => s.DurationMinutes);
        }

        public async Task<bool> CanEditSessionAsync(Guid sessionId, string userId, bool isAdmin = false)
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
                return false;

            if (isAdmin)
                return true;

            if (session.UserId != userId)
                return false;

            // Optional: Prevent editing after 24 hours
            var hoursSinceCreation = (DateTime.UtcNow - session.CreatedAt).TotalHours;
            return hoursSinceCreation <= 24;
        }
    }
}
