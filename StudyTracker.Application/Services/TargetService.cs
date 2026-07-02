using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class TargetService : ITargetService
    {
        private readonly IApplicationDbContext _context;

        public TargetService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserWeeklyTarget?> GetCurrentTargetAsync(string userId)
        {
            var today = TimeZoneHelper.GetTodayInCairo();
            var weekStart = DateHelper.GetWeekStartDate(today);

            return await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId && t.WeekStartDate == weekStart)
                .FirstOrDefaultAsync();
        }

        public async Task<UserWeeklyTarget> CreateOrUpdateTargetAsync(string userId, WeeklyTargetViewModel model)
        {
            var existing = await GetCurrentTargetAsync(userId);

            if (existing != null)
            {
                existing.DailyTargetMinutes = model.DailyTargetMinutes;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }

            var target = new UserWeeklyTarget
            {
                UserId = userId,
                WeekStartDate = model.WeekStartDate,
                DailyTargetMinutes = model.DailyTargetMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserWeeklyTargets.Add(target);
            await _context.SaveChangesAsync();

            return target;
        }

        public async Task<WeeklyProgressViewModel> GetWeeklyProgressAsync(string userId, DateOnly weekStartDate)
        {
            var target = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId && t.WeekStartDate == weekStartDate)
                .FirstOrDefaultAsync();

            if (target == null)
            {
                return new WeeklyProgressViewModel
                {
                    WeekStartDate = weekStartDate,
                    DailyTargetMinutes = 0
                };
            }

            var days = DateHelper.GetWeekDays(weekStartDate);
            var dayProgressList = new List<DayProgressViewModel>();

            foreach (var day in days)
            {
                var studiedMinutes = await _context.StudySessions
                    .Where(s => s.UserId == userId && s.Date == day)
                    .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

                dayProgressList.Add(new DayProgressViewModel
                {
                    Date = day,
                    DayName = day.ToString("dddd"),
                    StudiedMinutes = studiedMinutes,
                    TargetMinutes = target.DailyTargetMinutes
                });
            }

            var achievedDays = dayProgressList.Count(d => d.IsAchieved);
            var totalTargetMinutes = target.DailyTargetMinutes * 7;
            var totalStudiedMinutes = dayProgressList.Sum(d => d.StudiedMinutes);
            var completionPercentage = totalTargetMinutes > 0 ? (totalStudiedMinutes * 100.0 / totalTargetMinutes) : 0;

            return new WeeklyProgressViewModel
            {
                WeekStartDate = weekStartDate,
                DailyTargetMinutes = target.DailyTargetMinutes,
                Days = dayProgressList,
                AchievedDaysCount = achievedDays,
                WeeklyCompletionPercentage = completionPercentage
            };
        }

        public async Task<WeeklyTargetProgressViewModel> CalculateDailyProgressAsync(string userId, DateOnly date)
        {
            var weekStart = DateHelper.GetWeekStartDate(date);
            var target = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId && t.WeekStartDate == weekStart)
                .FirstOrDefaultAsync();

            var todayDone = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date == date)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

            var dailyTargetMinutes = target?.DailyTargetMinutes ?? 0;

            // Activity-based streak: consecutive days with at least one study session (any minutes)
            var sessionDates = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();
            var sessionDateSet = new HashSet<DateOnly>(sessionDates);
            var streakDays = 0;
            var startDate = sessionDateSet.Contains(date) ? date : date.AddDays(-1);
            var checkDate = startDate;
            while (sessionDateSet.Contains(checkDate))
            {
                streakDays++;
                checkDate = checkDate.AddDays(-1);
            }

            return new WeeklyTargetProgressViewModel
            {
                DailyTargetMinutes = dailyTargetMinutes,
                TodayDoneMinutes = todayDone,
                StreakDays = streakDays
            };
        }

        public async Task<int> GetStreakAsync(string userId)
        {
            var today = TimeZoneHelper.GetTodayInCairo();
            var sessionDates = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();
            var sessionDateSet = new HashSet<DateOnly>(sessionDates);
            var streakDays = 0;
            var startDate = sessionDateSet.Contains(today) ? today : today.AddDays(-1);
            var checkDate = startDate;
            while (sessionDateSet.Contains(checkDate))
            {
                streakDays++;
                checkDate = checkDate.AddDays(-1);
            }
            return streakDays;
        }
    }
}
