using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WeeklyTargetProgressViewModel?> GetWeeklyTargetProgressAsync(string userId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);

            var target = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId && t.WeekStartDate == weekStart)
                .FirstOrDefaultAsync();

            if (target == null)
                return null;

            var todayDone = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date == today)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

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

            return new WeeklyTargetProgressViewModel
            {
                DailyTargetMinutes = target.DailyTargetMinutes,
                TodayDoneMinutes = todayDone,
                StreakDays = streakDays
            };
        }
    }
}
