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
            var today = TimeZoneHelper.GetTodayInCairo();
            var weekStart = DateHelper.GetWeekStartDate(today);

            var target = await _context.UserWeeklyTargets
                .Where(t => t.UserId == userId && t.WeekStartDate == weekStart)
                .FirstOrDefaultAsync();

            if (target == null)
                return null;

            var todayDone = await _context.StudySessions
                .Where(s => s.UserId == userId && s.Date == today)
                .SumAsync(s => (int?)s.DurationMinutes) ?? 0;

            // Calculate streak based on consecutive days with any session logged
            // Fetch all distinct session dates for this user at once for performance
            var sessionDates = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();

            var sessionDateSet = new HashSet<DateOnly>(sessionDates);

            var streakDays = 0;

            // If no session today, start counting from yesterday
            var startDate = sessionDateSet.Contains(today) ? today : today.AddDays(-1);
            var checkDate = startDate;

            while (true)
            {
                if (sessionDateSet.Contains(checkDate))
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

        public async Task<int> GetActivityStreakAsync(string userId, DateOnly? asOfDate = null)
        {
            var today = asOfDate ?? TimeZoneHelper.GetTodayInCairo();
            var sessionDates = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => s.Date)
                .Distinct()
                .ToListAsync();
            var sessionDateSet = new HashSet<DateOnly>(sessionDates);
            var streakDays = 0;
            var startDate = sessionDateSet.Contains(today) ? today : today.AddDays(-1);
            var checkDate = startDate;
            while (true)
            {
                if (sessionDateSet.Contains(checkDate))
                {
                    streakDays++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                    break;
            }
            return streakDays;
        }
    }
}
