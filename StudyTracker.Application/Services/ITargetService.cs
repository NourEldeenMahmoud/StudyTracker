using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface ITargetService
    {
        Task<UserWeeklyTarget?> GetCurrentTargetAsync(string userId);
        Task<UserWeeklyTarget> CreateOrUpdateTargetAsync(string userId, WeeklyTargetViewModel model);
        Task<WeeklyProgressViewModel> GetWeeklyProgressAsync(string userId, DateOnly weekStartDate);
        Task<WeeklyTargetProgressViewModel> CalculateDailyProgressAsync(string userId, DateOnly date);
        Task<int> GetStreakAsync(string userId);
    }
}
