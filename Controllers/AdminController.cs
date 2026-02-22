using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ITargetService _targetService;
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IAdminService adminService, 
            ITargetService targetService,
            IDashboardService dashboardService,
            UserManager<ApplicationUser> userManager)
        {
            _adminService = adminService;
            _targetService = targetService;
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            ViewBag.Stats = stats;
            return View();
        }

        public async Task<IActionResult> Users(string? searchTerm)
        {
            var users = await _adminService.GetAllUsersAsync(searchTerm);
            ViewBag.Users = users;
            ViewBag.SearchTerm = searchTerm;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(string userId)
        {
            var success = await _adminService.SuspendUserAsync(userId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success 
                ? "User suspended successfully." 
                : "Failed to suspend user.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsuspendUser(string userId)
        {
            var success = await _adminService.UnsuspendUserAsync(userId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success 
                ? "User unsuspended successfully." 
                : "Failed to unsuspend user.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string userId)
        {
            var success = await _adminService.MakeAdminAsync(userId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success 
                ? "User promoted to admin successfully." 
                : "Failed to promote user.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Sessions(string? userId, DateOnly? startDate, DateOnly? endDate)
        {
            var sessions = await _adminService.GetAllSessionsAsync(userId, startDate, endDate);
            var users = await _adminService.GetAllUsersAsync();
            
            ViewBag.Sessions = sessions;
            ViewBag.Users = users;
            ViewBag.SelectedUserId = userId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            
            return View();
        }

        public async Task<IActionResult> Targets(string? userId)
        {
            var users = await _adminService.GetAllUsersAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
            
            var userTargets = new List<AdminUserTargetViewModel>();
            
            if (!string.IsNullOrEmpty(userId))
            {
                var target = await _targetService.GetCurrentTargetAsync(userId);
                var progress = await _targetService.GetWeeklyProgressAsync(userId, weekStart);
                var dailyProgress = await _targetService.CalculateDailyProgressAsync(userId, today);
                var streak = await _targetService.GetStreakAsync(userId);
                
                var user = await _userManager.FindByIdAsync(userId);
                
                // Calculate weekly totals from progress
                int? weeklyTotalMinutes = null;
                int? weeklyTargetMinutes = null;
                double? weeklyProgressPercentage = null;
                
                if (progress != null && progress.Days != null && progress.Days.Any())
                {
                    weeklyTotalMinutes = progress.Days.Sum(d => d.StudiedMinutes);
                    weeklyTargetMinutes = progress.Days.Sum(d => d.TargetMinutes);
                    weeklyProgressPercentage = progress.WeeklyCompletionPercentage;
                }
                
                userTargets.Add(new AdminUserTargetViewModel
                {
                    UserId = userId,
                    UserName = user?.FullName ?? "Unknown",
                    Email = user?.Email ?? "",
                    DailyTargetMinutes = target?.DailyTargetMinutes,
                    WeekStartDate = target?.WeekStartDate,
                    WeeklyTotalMinutes = weeklyTotalMinutes,
                    WeeklyTargetMinutes = weeklyTargetMinutes,
                    WeeklyProgressPercentage = weeklyProgressPercentage,
                    TodayDoneMinutes = dailyProgress?.TodayDoneMinutes ?? 0,
                    DailyTargetMinutesForToday = dailyProgress?.DailyTargetMinutes,
                    TodayProgressPercentage = dailyProgress?.ProgressPercentage ?? 0,
                    IsTodayAchieved = dailyProgress?.IsAchieved ?? false,
                    StreakDays = streak
                });
            }
            else
            {
                // Get all users with targets
                foreach (var user in users)
                {
                    var target = await _targetService.GetCurrentTargetAsync(user.Id);
                    if (target != null)
                    {
                        var progress = await _targetService.GetWeeklyProgressAsync(user.Id, weekStart);
                        var dailyProgress = await _targetService.CalculateDailyProgressAsync(user.Id, today);
                        var streak = await _targetService.GetStreakAsync(user.Id);
                        
                        // Calculate weekly totals from progress
                        int? weeklyTotalMinutes = null;
                        int? weeklyTargetMinutes = null;
                        double? weeklyProgressPercentage = null;
                        
                        if (progress != null && progress.Days != null && progress.Days.Any())
                        {
                            weeklyTotalMinutes = progress.Days.Sum(d => d.StudiedMinutes);
                            weeklyTargetMinutes = progress.Days.Sum(d => d.TargetMinutes);
                            weeklyProgressPercentage = progress.WeeklyCompletionPercentage;
                        }
                        
                        userTargets.Add(new AdminUserTargetViewModel
                        {
                            UserId = user.Id,
                            UserName = user.FullName,
                            Email = user.Email,
                            DailyTargetMinutes = target.DailyTargetMinutes,
                            WeekStartDate = target.WeekStartDate,
                            WeeklyTotalMinutes = weeklyTotalMinutes,
                            WeeklyTargetMinutes = weeklyTargetMinutes,
                            WeeklyProgressPercentage = weeklyProgressPercentage,
                            TodayDoneMinutes = dailyProgress?.TodayDoneMinutes ?? 0,
                            DailyTargetMinutesForToday = dailyProgress?.DailyTargetMinutes,
                            TodayProgressPercentage = dailyProgress?.ProgressPercentage ?? 0,
                            IsTodayAchieved = dailyProgress?.IsAchieved ?? false,
                            StreakDays = streak
                        });
                    }
                }
            }
            
            ViewBag.Users = users;
            ViewBag.UserTargets = userTargets;
            ViewBag.SelectedUserId = userId;
            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekStart.AddDays(6);
            
            return View();
        }
    }
}
