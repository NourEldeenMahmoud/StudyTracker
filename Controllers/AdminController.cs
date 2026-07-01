using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ITargetService _targetService;
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;
        private readonly IImageService _imageService;
        private readonly ISecurityLogService _securityLogService;

        public AdminController(
            IAdminService adminService,
            ITargetService targetService,
            IDashboardService dashboardService,
            UserManager<ApplicationUser> userManager,
            IBadgeService badgeService,
            IImageService imageService,
            ISecurityLogService securityLogService)
        {
            _adminService = adminService;
            _targetService = targetService;
            _dashboardService = dashboardService;
            _userManager = userManager;
            _badgeService = badgeService;
            _imageService = imageService;
            _securityLogService = securityLogService;
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
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            return View();
        }

        [HttpGet]
        public IActionResult Students(string? searchTerm)
        {
            return RedirectToActionPermanent("Users", new { searchTerm });
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

        public async Task<IActionResult> Sessions(string? userId, DateOnly? startDate, DateOnly? endDate, int page = 1)
        {
            var pagedSessions = await _adminService.GetAllSessionsAsync(userId, startDate, endDate, page);
            var users = await _adminService.GetAllUsersAsync();

            ViewBag.PagedSessions = pagedSessions;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetCurrentWeekLeaderboard()
        {
            if (!User.IsInRole("SuperAdmin"))
            {
                await _securityLogService.LogUnauthorizedDeleteAsync(
                    User,
                    "ResetCurrentWeekLeaderboard",
                    "StudySession",
                    null,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Forbid();
            }

            var deletedCount = await _adminService.ResetCurrentWeekSessionsAsync();

            TempData[deletedCount > 0 ? "SuccessMessage" : "ErrorMessage"] =
                deletedCount > 0
                    ? $"Weekly leaderboard reset successfully for {deletedCount} session(s) this week."
                    : "No sessions found for the current week to reset.";

            return RedirectToAction("Sessions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (!User.IsInRole("SuperAdmin"))
            {
                await _securityLogService.LogUnauthorizedDeleteAsync(
                    User,
                    "DeleteUser",
                    "ApplicationUser",
                    userId,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Forbid();
            }

            var success = await _adminService.DeleteUserCompletelyAsync(userId);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "User deleted successfully." : "Failed to delete user.";

            return RedirectToAction("Users");
        }

        // ── Badges Management ──────────────────────────────────────────────

        public async Task<IActionResult> Badges()
        {
            var summaries = await _badgeService.GetAllUserBadgeSummariesAsync();
            var customDefs = await _badgeService.GetCustomDefinitionsAsync();
            var definitions = await _badgeService.GetAllDefinitionsAsync();
            ViewBag.Summaries = summaries;
            ViewBag.Definitions = definitions;
            ViewBag.CustomDefs = customDefs;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSystemBadge(string badgeKey, string name, string description, string icon, string color)
        {
            if (string.IsNullOrWhiteSpace(badgeKey) || string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Badge key and name are required.";
                return RedirectToAction("Badges");
            }
            await _badgeService.SaveSystemBadgeOverrideAsync(badgeKey.Trim(), name.Trim(), description?.Trim() ?? "", icon?.Trim() ?? "", color?.Trim() ?? "");
            TempData["SuccessMessage"] = "System badge updated successfully.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignBadge(string userId, string badgeKey)
        {
            var success = await _badgeService.AssignBadgeAsync(userId, badgeKey);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Badge assigned successfully."
                : "User already has this badge.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCustomBadge(string userId, Guid customBadgeId)
        {
            var success = await _badgeService.AssignCustomBadgeAsync(userId, customBadgeId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Custom badge assigned successfully."
                : "User already has this badge.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeBadge(string userId, string badgeKey)
        {
            var success = await _badgeService.RevokeBadgeAsync(userId, badgeKey);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Badge revoked."
                : "Badge not found.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomBadge(string name, string description, string icon, string color, string? adminNote, IFormFile? iconFile)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Badge name is required.";
                return RedirectToAction("Badges");
            }

            var adminUser = await _userManager.GetUserAsync(User);

            if (iconFile != null && iconFile.Length > 0)
            {
                try
                {
                    icon = await _imageService.UploadBadgeIconAsync(iconFile, Guid.NewGuid().ToString("N"));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Image upload failed: {ex.Message}";
                    return RedirectToAction("Badges");
                }
            }

            if (string.IsNullOrWhiteSpace(icon))
                icon = "military_tech";

            await _badgeService.CreateCustomBadgeAsync(name.Trim(), description ?? string.Empty, icon, color, adminNote, adminUser!.Id);
            TempData["SuccessMessage"] = $"Custom badge \"{name}\" created successfully.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomBadge(Guid id)
        {
            if (!User.IsInRole("SuperAdmin"))
            {
                await _securityLogService.LogUnauthorizedDeleteAsync(
                    User,
                    "DeleteCustomBadge",
                    "CustomBadgeDefinition",
                    id.ToString(),
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Forbid();
            }

            var customs = await _badgeService.GetCustomDefinitionsAsync();
            var badge = customs.FirstOrDefault(b => b.Id == id);
            if (badge != null && badge.Icon.StartsWith("/images/badges/"))
                await _imageService.DeleteBadgeIconAsync(badge.Icon);

            var success = await _badgeService.DeleteCustomBadgeAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Custom badge deleted (and revoked from all users)."
                : "Badge not found.";
            return RedirectToAction("Badges");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomBadge(Guid id, string name, string description, string icon, string color, string? adminNote, IFormFile? iconFile)
        {
            if (!User.IsInRole("SuperAdmin"))
            {
                await _securityLogService.LogUnauthorizedDeleteAsync(
                    User,
                    "EditCustomBadge",
                    "CustomBadgeDefinition",
                    id.ToString(),
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Badge name is required.";
                return RedirectToAction("Badges");
            }

            var customs = await _badgeService.GetCustomDefinitionsAsync();
            var badge = customs.FirstOrDefault(b => b.Id == id);
            if (badge == null)
            {
                TempData["ErrorMessage"] = "Badge not found.";
                return RedirectToAction("Badges");
            }

            var finalIcon = badge.Icon;

            if (iconFile != null && iconFile.Length > 0)
            {
                try
                {
                    var newIcon = await _imageService.UploadBadgeIconAsync(iconFile, id.ToString("N"));
                    if (!string.IsNullOrEmpty(badge.Icon) && badge.Icon.StartsWith("/images/badges/") && !string.Equals(badge.Icon, newIcon, StringComparison.OrdinalIgnoreCase))
                    {
                        await _imageService.DeleteBadgeIconAsync(badge.Icon);
                    }
                    finalIcon = newIcon;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Image upload failed: {ex.Message}";
                    return RedirectToAction("Badges");
                }
            }
            else if (!string.IsNullOrWhiteSpace(icon))
            {
                finalIcon = icon;
            }

            var updated = await _badgeService.UpdateCustomBadgeAsync(
                id,
                name.Trim(),
                description ?? string.Empty,
                finalIcon,
                color,
                adminNote
            );

            if (updated == null)
            {
                TempData["ErrorMessage"] = "Badge not found.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Custom badge \"{name}\" updated successfully.";
            }

            return RedirectToAction("Badges");
        }
    }
}
