using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IDashboardService
    {
        Task<WeeklyTargetProgressViewModel?> GetWeeklyTargetProgressAsync(string userId);
        /// <summary>Activity-based streak: consecutive days with at least one study session. Does not depend on weekly target.</summary>
        Task<int> GetActivityStreakAsync(string userId, DateOnly? asOfDate = null);
    }
}
