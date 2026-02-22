using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IDashboardService
    {
        Task<WeeklyTargetProgressViewModel?> GetWeeklyTargetProgressAsync(string userId);
    }
}
