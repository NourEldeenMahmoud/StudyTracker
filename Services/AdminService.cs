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
        private readonly IImageService _imageService;

        public AdminService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILeaderboardService leaderboardService,
            IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _leaderboardService = leaderboardService;
            _imageService = imageService;
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

        public async Task<PagedResult<AdminSessionViewModel>> GetAllSessionsAsync(string? userId = null, DateOnly? startDate = null, DateOnly? endDate = null, int page = 1, int pageSize = 25)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;

            var query = _context.StudySessions
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(s => s.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(s => s.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.Date <= endDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedResult<AdminSessionViewModel>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> ResetCurrentWeekSessionsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
            var weekEnd = weekStart.AddDays(6);

            var sessions = await _context.StudySessions
                .Where(s => s.Date >= weekStart && s.Date <= weekEnd)
                .ToListAsync();

            if (!sessions.Any())
                return 0;

            _context.StudySessions.RemoveRange(sessions);
            var deletedCount = await _context.SaveChangesAsync();
            return deletedCount;
        }

        public async Task<bool> DeleteUserCompletelyAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.StudySessions)
                .Include(u => u.WeeklyTargets)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (user.StudySessions.Any())
                {
                    _context.StudySessions.RemoveRange(user.StudySessions);
                }

                if (user.WeeklyTargets.Any())
                {
                    _context.UserWeeklyTargets.RemoveRange(user.WeeklyTargets);
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    await _imageService.DeleteProfilePictureAsync(user.ProfilePictureUrl);
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
