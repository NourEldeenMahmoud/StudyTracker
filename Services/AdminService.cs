using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;

namespace StudyTracker.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILeaderboardService _leaderboardService;

        public AdminService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILeaderboardService leaderboardService)
        {
            _context = context;
            _userManager = userManager;
            _leaderboardService = leaderboardService;
        }

        public async Task<AdminDashboardStatsViewModel> GetDashboardStatsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);

            var totalStudents = await _context.Users.CountAsync();
            var activeSessionsToday = await _context.StudySessions
                .Where(s => s.Date == today)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
            var totalHoursLogged = await _context.StudySessions
                .SumAsync(s => (double?)s.DurationMinutes) ?? 0;

            var weeklyLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart);
            var topUser = weeklyLeaderboard.FirstOrDefault();

            return new AdminDashboardStatsViewModel
            {
                TotalStudents = totalStudents,
                ActiveSessionsToday = activeSessionsToday,
                TotalHoursLogged = totalHoursLogged / 60.0,
                TopUserWeeklyName = topUser?.FullName,
                TopUserWeeklyHours = topUser?.TotalHours ?? 0,
                WeeklyTop5 = weeklyLeaderboard.Take(5).ToList()
            };
        }

        public async Task<List<AdminUserViewModel>> GetAllUsersAsync(string? searchTerm = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => 
                    u.FullName.Contains(searchTerm) || 
                    u.Email!.Contains(searchTerm));
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new List<AdminUserViewModel>();
            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                result.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    CreatedAt = user.CreatedAt,
                    IsSuspended = user.IsSuspended,
                    IsAdmin = isAdmin
                });
            }

            return result;
        }

        public async Task<bool> SuspendUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.IsSuspended = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnsuspendUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.IsSuspended = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MakeAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            await _userManager.AddToRoleAsync(user, "Admin");
            return true;
        }

        public async Task<List<AdminSessionViewModel>> GetAllSessionsAsync(string? userId = null, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            var query = _context.StudySessions
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.UserId == userId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.Date <= endDate.Value);
            }

            return await query
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new AdminSessionViewModel
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User.FullName,
                    Date = s.Date,
                    DurationMinutes = s.DurationMinutes,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }
    }
}
