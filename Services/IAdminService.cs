using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsViewModel> GetDashboardStatsAsync();
        Task<List<AdminUserViewModel>> GetAllUsersAsync(string? searchTerm = null);
        Task<bool> SuspendUserAsync(string userId);
        Task<bool> UnsuspendUserAsync(string userId);
        Task<bool> MakeAdminAsync(string userId);
        Task<List<AdminSessionViewModel>> GetAllSessionsAsync(string? userId = null, DateOnly? startDate = null, DateOnly? endDate = null);
    }
}
