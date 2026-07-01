using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class DailyProgressService : IDailyProgressService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeaderboardService _leaderboardService;

        public DailyProgressService(ApplicationDbContext context, ILeaderboardService leaderboardService)
        {
            _context = context;
            _leaderboardService = leaderboardService;
        }

        public async Task<DailyProgressViewModel> GetDailyProgressAsync(string userId, DateOnly date)
        {
            var weekStart = DateHelper.GetWeekStartDate(date);
            var weekEnd = weekStart.AddDays(6);

            var allTargets = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.WeekStartDate)
                .Select(t => new { t.WeekStartDate, t.DailyTargetMinutes })
                .ToListAsync();

            int ResolveTargetMinutes(DateOnly forDate)
            {
                var ws = DateHelper.GetWeekStartDate(forDate);
                var t = allTargets.FirstOrDefault(x => x.WeekStartDate == ws);
                if (t != null) return t.DailyTargetMinutes;

                var current = allTargets.FirstOrDefault(x => x.WeekStartDate == weekStart);
                return current?.DailyTargetMinutes ?? 0;
            }

            var dailyTargetMinutes = ResolveTargetMinutes(date);

            var weekSessions = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date >= weekStart && s.Date <= weekEnd)
                .Select(s => new { s.Id, s.Date, s.CreatedAt, s.DurationMinutes, s.Notes })
                .ToListAsync();

            var daySessions = weekSessions
                .Where(s => s.Date == date)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            var studiedMinutes = daySessions.Sum(s => s.DurationMinutes);
            var sessionsCount = daySessions.Count;

            var weeklyTotalMinutes = weekSessions.Sum(s => s.DurationMinutes);
            var weeklyAverageMinutesPerDay = weeklyTotalMinutes / 7.0;

            var prevWeekStart = weekStart.AddDays(-7);
            var prevWeekEnd = prevWeekStart.AddDays(6);
            var previousWeekTotalMinutes = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date >= prevWeekStart && s.Date <= prevWeekEnd)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

            var studiedByDate = weekSessions
                .GroupBy(s => s.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.DurationMinutes));

            var weekDays = DateHelper.GetWeekDays(weekStart)
                .Select(d => new DailyProgressDaySummaryViewModel
                {
                    Date = d,
                    DayName = d.ToString("ddd"),
                    StudiedMinutes = studiedByDate.TryGetValue(d, out var mins) ? mins : 0,
                    TargetMinutes = ResolveTargetMinutes(d)
                })
                .ToList();

            var leaderboard = await _leaderboardService.GetDailyLeaderboardAsync(date, userId);
            var meEntry = leaderboard.FirstOrDefault(e => e.UserId == userId);
            var currentRank = meEntry?.Rank ?? 0;

            // Productive hours (approx.): bucket by CreatedAt hour (server local time)
            var productiveMinutesByHour = new Dictionary<int, int>();
            for (int h = 0; h < 24; h++) productiveMinutesByHour[h] = 0;
            foreach (var s in daySessions)
            {
                var cairoTime = TimeZoneHelper.ToCairo(s.CreatedAt);
                var hour = cairoTime.Hour;
                productiveMinutesByHour[hour] = productiveMinutesByHour.GetValueOrDefault(hour) + s.DurationMinutes;
            }

            // Activity-based streak as of this date: consecutive days with at least one study session
            var sessionDatesForStreak = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();
            var sessionDateSetForStreak = new HashSet<DateOnly>(sessionDatesForStreak);
            var streakDays = 0;
            var startDateStreak = sessionDateSetForStreak.Contains(date) ? date : date.AddDays(-1);
            var checkDateStreak = startDateStreak;
            while (sessionDateSetForStreak.Contains(checkDateStreak))
            {
                streakDays++;
                checkDateStreak = checkDateStreak.AddDays(-1);
            }

            var todos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.Date == date)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new DailyProgressTodoItemViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    IsCompleted = t.IsCompleted,
                    CreatedAtUtc = t.CreatedAt
                })
                .ToListAsync();

            return new DailyProgressViewModel
            {
                SelectedDate = date,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                DailyTargetMinutes = dailyTargetMinutes,
                StudiedMinutes = studiedMinutes,
                SessionsCount = sessionsCount,
                CurrentRank = currentRank,
                StreakDays = streakDays,
                WeeklyTotalMinutes = weeklyTotalMinutes,
                WeeklyAverageMinutesPerDay = weeklyAverageMinutesPerDay,
                PreviousWeekTotalMinutes = previousWeekTotalMinutes,
                WeekDays = weekDays,
                Sessions = daySessions.Select(s => new DailyProgressSessionViewModel
                {
                    Id = s.Id,
                    CreatedAtUtc = s.CreatedAt,
                    DurationMinutes = s.DurationMinutes,
                    Notes = s.Notes
                }).ToList(),
                Todos = todos,
                ProductiveMinutesByHour = productiveMinutesByHour
            };
        }
    }
}

