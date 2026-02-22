using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class StudySessionService : IStudySessionService
    {
        private readonly ApplicationDbContext _context;

        public StudySessionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudySession> AddSessionAsync(string userId, AddSessionViewModel model)
        {
            // Validate: Max 16 hours per day
            var dailyTotal = await GetDailyTotalAsync(userId, model.Date);
            if (dailyTotal + model.DurationMinutes > 16 * 60)
            {
                throw new InvalidOperationException("Cannot exceed 16 hours per day.");
            }

            // Validate: No future dates
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (model.Date > today)
            {
                throw new InvalidOperationException("Cannot add sessions for future dates.");
            }

            var session = new StudySession
            {
                UserId = userId,
                Date = model.Date,
                DurationMinutes = model.DurationMinutes,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();

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
