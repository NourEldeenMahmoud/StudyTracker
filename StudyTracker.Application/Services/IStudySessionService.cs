using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IStudySessionService
    {
        Task<StudySession> AddSessionAsync(string userId, AddSessionViewModel model);
        Task<List<StudySessionViewModel>> GetUserSessionsAsync(string userId, DateOnly? startDate = null, DateOnly? endDate = null);
        Task<StudySession?> GetSessionByIdAsync(Guid sessionId);
        Task<bool> UpdateSessionAsync(Guid sessionId, string userId, AddSessionViewModel model, bool isAdmin = false);
        Task<bool> DeleteSessionAsync(Guid sessionId, string userId, bool isAdmin = false);
        Task<int> GetDailyTotalAsync(string userId, DateOnly date);
        Task<bool> CanEditSessionAsync(Guid sessionId, string userId, bool isAdmin = false);
    }
}
