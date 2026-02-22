using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ApplicationDbContext _context;

        public LeaderboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaderboardEntryViewModel>> GetDailyLeaderboardAsync(DateOnly date, string? currentUserId = null)
        {
            var entries = await _context.StudySessions
                .Where(s => s.Date == date)
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalMinutes = g.Sum(s => s.DurationMinutes),
                    SessionsCount = g.Count()
                })
                .OrderByDescending(x => x.TotalMinutes)
                .ThenByDescending(x => x.SessionsCount)
                .Join(
                    _context.Users,
                    stats => stats.UserId,
                    user => user.Id,
                    (stats, user) => new LeaderboardEntryViewModel
                    {
                        UserId = stats.UserId,
                        FullName = user.FullName,
                        TotalHours = stats.TotalMinutes / 60.0,
                        SessionsCount = stats.SessionsCount,
                        IsCurrentUser = currentUserId != null && stats.UserId == currentUserId,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    }
                )
                .ToListAsync();

            // Assign ranks - sequential ranking (1, 2, 3...) even if hours are equal
            int rank = 1;
            foreach (var entry in entries)
            {
                entry.Rank = rank;
                rank++;
            }

            return entries;
        }

        public async Task<List<LeaderboardEntryViewModel>> GetWeeklyLeaderboardAsync(DateOnly weekStartDate, string? currentUserId = null)
        {
            var weekEnd = weekStartDate.AddDays(6);

            var entries = await _context.StudySessions
                .Where(s => s.Date >= weekStartDate && s.Date <= weekEnd)
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalMinutes = g.Sum(s => s.DurationMinutes),
                    SessionsCount = g.Count()
                })
                .OrderByDescending(x => x.TotalMinutes)
                .ThenByDescending(x => x.SessionsCount)
                .Join(
                    _context.Users,
                    stats => stats.UserId,
                    user => user.Id,
                    (stats, user) => new LeaderboardEntryViewModel
                    {
                        UserId = stats.UserId,
                        FullName = user.FullName,
                        TotalHours = stats.TotalMinutes / 60.0,
                        SessionsCount = stats.SessionsCount,
                        IsCurrentUser = currentUserId != null && stats.UserId == currentUserId,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    }
                )
                .ToListAsync();

            // Assign ranks - sequential ranking (1, 2, 3...) even if hours are equal
            int rank = 1;
            foreach (var entry in entries)
            {
                entry.Rank = rank;
                rank++;
            }

            return entries;
        }

        public async Task<List<LeaderboardEntryViewModel>> GetMonthlyLeaderboardAsync(int year, int month, string? currentUserId = null)
        {
            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var entries = await _context.StudySessions
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalMinutes = g.Sum(s => s.DurationMinutes),
                    SessionsCount = g.Count()
                })
                .OrderByDescending(x => x.TotalMinutes)
                .ThenByDescending(x => x.SessionsCount)
                .Join(
                    _context.Users,
                    stats => stats.UserId,
                    user => user.Id,
                    (stats, user) => new LeaderboardEntryViewModel
                    {
                        UserId = stats.UserId,
                        FullName = user.FullName,
                        TotalHours = stats.TotalMinutes / 60.0,
                        SessionsCount = stats.SessionsCount,
                        IsCurrentUser = currentUserId != null && stats.UserId == currentUserId,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    }
                )
                .ToListAsync();

            // Assign ranks - sequential ranking (1, 2, 3...) even if hours are equal
            int rank = 1;
            foreach (var entry in entries)
            {
                entry.Rank = rank;
                rank++;
            }

            return entries;
        }

        public async Task<int> GetUserRankAsync(string userId, string type, DateOnly date)
        {
            List<LeaderboardEntryViewModel> entries;

            switch (type.ToLower())
            {
                case "daily":
                    entries = await GetDailyLeaderboardAsync(date);
                    break;
                case "weekly":
                    var weekStart = DateHelper.GetWeekStartDate(date);
                    entries = await GetWeeklyLeaderboardAsync(weekStart);
                    break;
                case "monthly":
                    entries = await GetMonthlyLeaderboardAsync(date.Year, date.Month);
                    break;
                default:
                    return 0;
            }

            var userEntry = entries.FirstOrDefault(e => e.UserId == userId);
            return userEntry?.Rank ?? 0;
        }
    }
}
