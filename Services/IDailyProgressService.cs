using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IDailyProgressService
    {
        Task<DailyProgressViewModel> GetDailyProgressAsync(string userId, DateOnly date);
    }
}

