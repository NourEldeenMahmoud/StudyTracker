using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IWeeklyLeaderboardArchiveService
    {
        Task ArchiveWeekIfMissingAsync(DateOnly weekStart);
        Task<List<WeeklyLeaderboardArchive>> GetArchivesAsync(int page, int pageSize);
        Task<WeeklyLeaderboardArchive?> GetByWeekAsync(DateOnly weekStart);
        Task<List<LeaderboardEntryViewModel>> GetEntriesForArchiveAsync(WeeklyLeaderboardArchive archive);

        /// <summary>
        /// True if the user has ever been #1 on any completed weekly leaderboard
        /// (only archived weeks are considered).
        /// </summary>
        Task<bool> WasUserEverRankOneAsync(string userId);

        /// <summary>True if the user finished with this exact rank (2 or 3, etc.) in any archived weekly leaderboard.</summary>
        Task<bool> WasUserEverRankExactlyAsync(string userId, int rank);

        /// <summary>Maximum number of consecutive weeks the user was in the top N (e.g. top 3).</summary>
        Task<int> GetMaxConsecutiveWeeksInTopAsync(string userId, int topN);
    }
}

