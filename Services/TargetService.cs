using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class TargetService : ITargetService
    {
        private readonly ApplicationDbContext _context;

        public TargetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserWeeklyTarget?> GetCurrentTargetAsync(string userId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
            var target = await GetCurrentTargetAsync(userId);

            if (target == null || target.WeekStartDate != weekStart)
            {
                return new WeeklyTargetProgressViewModel
                {
                    DailyTargetMinutes = 0,
                    TodayDoneMinutes = 0,
                    StreakDays = 0
                };
            }

            var todayDone = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date == date)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

            // Calculate streak - continues across weeks
            // Pre-fetch all targets for better performance
            var allTargets = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.WeekStartDate)
                .ToListAsync();
            
            var streakDays = 0;
            var checkDate = date;
            var maxDaysBack = 365; // Limit to 1 year to prevent infinite loops
            
            while (streakDays < maxDaysBack)
            {
                var dayDone = await _context.StudySessions
                    .Where(s => s.UserId == userId && s.Date == checkDate)
                    .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

                // Get the target for this day's week (or use current target if no target found)
                var dayWeekStart = DateHelper.GetWeekStartDate(checkDate);
                var dayTarget = allTargets.FirstOrDefault(t => t.WeekStartDate == dayWeekStart);

                // Use current target if no target found for that week, or use the found target
                var requiredMinutes = dayTarget?.DailyTargetMinutes ?? target.DailyTargetMinutes;

                if (dayDone >= requiredMinutes)
                {
                    streakDays++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return new WeeklyTargetProgressViewModel
            {
                DailyTargetMinutes = target.DailyTargetMinutes,
                TodayDoneMinutes = todayDone,
                StreakDays = streakDays
            };
        }

        public async Task<int> GetStreakAsync(string userId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var target = await GetCurrentTargetAsync(userId);

            if (target == null)
                return 0;

            // Calculate streak - continues across weeks
            // Pre-fetch all targets for better performance
            var allTargets = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.WeekStartDate)
                .ToListAsync();
            
            var streakDays = 0;
            var checkDate = today;
            var maxDaysBack = 365; // Limit to 1 year to prevent infinite loops
            
            while (streakDays < maxDaysBack)
            {
                var dayDone = await _context.StudySessions
                    .Where(s => s.UserId == userId && s.Date == checkDate)
                    .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

                // Get the target for this day's week (or use current target if no target found)
                var dayWeekStart = DateHelper.GetWeekStartDate(checkDate);
                var dayTarget = allTargets.FirstOrDefault(t => t.WeekStartDate == dayWeekStart);

                // Use current target if no target found for that week, or use the found target
                var requiredMinutes = dayTarget?.DailyTargetMinutes ?? target.DailyTargetMinutes;

                if (dayDone >= requiredMinutes)
                {
                    streakDays++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streakDays;
        }
    }
}
