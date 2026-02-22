using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface ILeaderboardService
    {
        Task<List<LeaderboardEntryViewModel>> GetDailyLeaderboardAsync(DateOnly date, string? currentUserId = null);
        Task<List<LeaderboardEntryViewModel>> GetWeeklyLeaderboardAsync(DateOnly weekStartDate, string? currentUserId = null);
        Task<List<LeaderboardEntryViewModel>> GetMonthlyLeaderboardAsync(int year, int month, string? currentUserId = null);
        Task<int> GetUserRankAsync(string userId, string type, DateOnly date);
    }
}
