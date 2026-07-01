using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IBadgeService _badgeService;
        private readonly IWeeklyLeaderboardArchiveService _archiveService;

        public DashboardController(IDashboardService dashboardService, ILeaderboardService leaderboardService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IBadgeService badgeService, IWeeklyLeaderboardArchiveService archiveService)
        {
            _dashboardService = dashboardService;
            _leaderboardService = leaderboardService;
            _userManager = userManager;
            _context = context;
            _badgeService = badgeService;
            _archiveService = archiveService;
        }

        // Attaches earned badge display info to each leaderboard entry
        private async Task EnrichWithBadgesAsync(IEnumerable<LeaderboardEntryViewModel> entries)
        {
            var userIds = entries.Select(e => e.UserId).ToList();
            if (!userIds.Any()) return;

            List<UserBadge> allBadges;
            try
            {
                allBadges = await _context.UserBadges
                    .Where(b => userIds.Contains(b.UserId))
                    .ToListAsync();
            }
            catch { return; }

            var predefined = _badgeService.GetAllDefinitions();
            List<CustomBadgeDefinition> customDefs;
            try { customDefs = await _badgeService.GetCustomDefinitionsAsync(); }
            catch { customDefs = new List<CustomBadgeDefinition>(); }

            foreach (var entry in entries)
            {
                var userBadges = allBadges.Where(b => b.UserId == entry.UserId);
                entry.Badges = userBadges
                    .Select(b =>
                    {
                        var pre = predefined.FirstOrDefault(d => d.Key == b.BadgeKey);
                        if (pre != null)
                        {
                            var cat = string.IsNullOrEmpty(pre.CategoryId) ? null : pre.CategoryId;
                            var chipColor = pre.TierLevel > 0 && cat != null
                                ? BadgeTierUi.IconBadgeSurfaceClasses(pre.TierLevel, cat)
                                : pre.Color;
                            return new LeaderboardBadgeInfo
                            {
                                Icon = pre.Icon,
                                Color = chipColor,
                                Name = pre.Name,
                                Description = pre.Description,
                                TierLevel = pre.TierLevel > 0 ? pre.TierLevel : null,
                                CategoryId = cat
                            };
                        }

                        if (Guid.TryParse(b.BadgeKey, out var gid))
                        {
                            var cd = customDefs.FirstOrDefault(d => d.Id == gid);
                            if (cd != null)
                            {
                                return new LeaderboardBadgeInfo
                                {
                                    Icon = cd.Icon,
                                    Color = cd.Color,
                                    Name = cd.Name,
                                    Description = cd.Description
                                };
                            }
                        }

                        return null;
                    })
                    .Where(x => x != null)
                    .Cast<LeaderboardBadgeInfo>()
                    .ToList();
            }
        }

        public async Task<IActionResult> Index()
        {
            try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var targetProgress = await _dashboardService.GetWeeklyTargetProgressAsync(user.Id);

            // Get full weekly leaderboard for the table (today = Cairo)
            var today = TimeZoneHelper.GetTodayInCairo();
            var weekStart = DateHelper.GetWeekStartDate(today);

            // Ensure last completed week is archived (Saturday-based weeks)
            var previousWeekStart = weekStart.AddDays(-7);
            await _archiveService.ArchiveWeekIfMissingAsync(previousWeekStart);

            var weeklyLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart, user.Id);

            // Get daily leaderboard for today - but use weekly leaderboard users and show their total weekly hours
            var dailyLeaderboardRaw = await _leaderboardService.GetDailyLeaderboardAsync(today, user.Id);
            
            // If we have weekly leaderboard entries, use them but show total weekly hours
            // This ensures all active weekly users appear in daily leaderboard with their total hours
            var dailyLeaderboard = new List<LeaderboardEntryViewModel>();
            if (weeklyLeaderboard.Any())
            {
                foreach (var weeklyEntry in weeklyLeaderboard.Take(3))
                {
                    // Use weekly total hours instead of daily hours
                    dailyLeaderboard.Add(new LeaderboardEntryViewModel
                    {
                        UserId = weeklyEntry.UserId,
                        FullName = weeklyEntry.FullName,
                        TotalHours = weeklyEntry.TotalHours, // Use weekly total hours
                        SessionsCount = weeklyEntry.SessionsCount,
                        IsCurrentUser = weeklyEntry.IsCurrentUser,
                        Rank = weeklyEntry.Rank,
                        ProfilePictureUrl = weeklyEntry.ProfilePictureUrl // Include profile picture URL
                    });
                }
                
                // Re-assign ranks sequentially based on total hours
                int rank = 1;
                foreach (var entry in dailyLeaderboard.OrderByDescending(e => e.TotalHours).ThenByDescending(e => e.SessionsCount))
                {
                    entry.Rank = rank;
                    rank++;
                }
            }
            else
            {
                dailyLeaderboard = dailyLeaderboardRaw;
            }

            // Get current user entry from weekly leaderboard
            var currentUserEntry = weeklyLeaderboard.FirstOrDefault(e => e.IsCurrentUser);
            var currentUserRank = currentUserEntry?.Rank ?? 0;
            var currentUserHours = currentUserEntry?.TotalHours ?? 0;
            
            // If user is not in weekly leaderboard or hours is 0, calculate their hours manually
            if (currentUserEntry == null || currentUserHours == 0)
            {
                var userSessions = await _context.StudySessions
                    .Where(s => s.UserId == user.Id && s.Date >= weekStart && s.Date <= weekStart.AddDays(6))
                    .SumAsync(s => (int?)s.DurationMinutes) ?? 0;
                currentUserHours = userSessions / 60.0;
                
                // If user has sessions but not in leaderboard, calculate rank
                if (currentUserHours > 0 && currentUserEntry == null)
                {
                    var usersAbove = weeklyLeaderboard.Count(e => e.TotalHours > currentUserHours);
                    currentUserRank = usersAbove + 1;
                }
            }

            await EnrichWithBadgesAsync(weeklyLeaderboard);
            await EnrichWithBadgesAsync(dailyLeaderboard);

            var streakDays = await _dashboardService.GetActivityStreakAsync(user.Id);
            ViewBag.StreakDays = streakDays;
            ViewBag.TargetProgress = targetProgress;
            ViewBag.WeeklyLeaderboard = weeklyLeaderboard;
            ViewBag.DailyLeaderboard = dailyLeaderboard;
            ViewBag.TodayDate = today;
            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekStart.AddDays(6);
            ViewBag.CurrentUserRank = currentUserRank;
            ViewBag.CurrentUserHours = currentUserHours;
            ViewBag.UserName = user.FullName;
            ViewBag.CurrentUserId = user.Id;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;

            return View();
            }
            catch (Exception ex)
            {
                // Log the error
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<DashboardController>>();
                logger.LogError(ex, "Error in Dashboard/Index. User: {UserId}", User?.Identity?.Name);
                
                // Write to console for stdout logs
                Console.WriteLine($"DASHBOARD ERROR: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                // Return error view
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var targetProgress = await _dashboardService.GetWeeklyTargetProgressAsync(user.Id);

            // Get full weekly leaderboard for the table
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
            var weeklyLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart, user.Id);

            // Get daily leaderboard for today
            var dailyLeaderboardRaw = await _leaderboardService.GetDailyLeaderboardAsync(today, user.Id);
            
            // If we have weekly leaderboard entries, use them but show total weekly hours
            var dailyLeaderboard = new List<LeaderboardEntryViewModel>();
            if (weeklyLeaderboard.Any())
            {
                foreach (var weeklyEntry in weeklyLeaderboard.Take(3))
                {
                    dailyLeaderboard.Add(new LeaderboardEntryViewModel
                    {
                        UserId = weeklyEntry.UserId,
                        FullName = weeklyEntry.FullName,
                        TotalHours = weeklyEntry.TotalHours,
                        SessionsCount = weeklyEntry.SessionsCount,
                        IsCurrentUser = weeklyEntry.IsCurrentUser,
                        Rank = weeklyEntry.Rank,
                        ProfilePictureUrl = weeklyEntry.ProfilePictureUrl
                    });
                }
                
                // Re-assign ranks sequentially
                int rank = 1;
                foreach (var entry in dailyLeaderboard.OrderByDescending(e => e.TotalHours).ThenByDescending(e => e.SessionsCount))
                {
                    entry.Rank = rank;
                    rank++;
                }
            }
            else
            {
                dailyLeaderboard = dailyLeaderboardRaw;
            }

            // Get current user entry from weekly leaderboard
            var currentUserEntry = weeklyLeaderboard.FirstOrDefault(e => e.IsCurrentUser);
            var currentUserRank = currentUserEntry?.Rank ?? 0;
            var currentUserHours = currentUserEntry?.TotalHours ?? 0;
            
            // If user is not in weekly leaderboard or hours is 0, calculate their hours manually
            if (currentUserEntry == null || currentUserHours == 0)
            {
                var userSessions = await _context.StudySessions
                    .Where(s => s.UserId == user.Id && s.Date >= weekStart && s.Date <= weekStart.AddDays(6))
                    .SumAsync(s => (int?)s.DurationMinutes) ?? 0;
                currentUserHours = userSessions / 60.0;
                
                // If user has sessions but not in leaderboard, calculate rank
                if (currentUserHours > 0 && currentUserEntry == null)
                {
                    var usersAbove = weeklyLeaderboard.Count(e => e.TotalHours > currentUserHours);
                    currentUserRank = usersAbove + 1;
                }
            }

            await EnrichWithBadgesAsync(weeklyLeaderboard);
            await EnrichWithBadgesAsync(dailyLeaderboard);

            return Json(new
            {
                success = true,
                targetProgress = targetProgress != null ? new
                {
                    progressPercentage = targetProgress.ProgressPercentage,
                    todayDoneMinutes = targetProgress.TodayDoneMinutes,
                    dailyTargetMinutes = targetProgress.DailyTargetMinutes,
                    streakDays = targetProgress.StreakDays,
                    isAchieved = targetProgress.IsAchieved
                } : null,
                weeklyLeaderboard = weeklyLeaderboard.Select(e => new
                {
                    rank = e.Rank,
                    userId = e.UserId,
                    fullName = e.FullName,
                    totalHours = e.TotalHours,
                    sessionsCount = e.SessionsCount,
                    isCurrentUser = e.IsCurrentUser,
                    profilePictureUrl = e.ProfilePictureUrl,
                    badges = e.Badges.Select(b => new { b.Icon, b.Color, b.Name, b.Description }).ToList()
                }).ToList(),
                dailyLeaderboard = dailyLeaderboard.Select(e => new
                {
                    rank = e.Rank,
                    userId = e.UserId,
                    fullName = e.FullName,
                    totalHours = e.TotalHours,
                    sessionsCount = e.SessionsCount,
                    isCurrentUser = e.IsCurrentUser,
                    profilePictureUrl = e.ProfilePictureUrl,
                    badges = e.Badges.Select(b => new { b.Icon, b.Color, b.Name, b.Description }).ToList()
                }).ToList(),
                currentUserRank = currentUserRank,
                currentUserHours = currentUserHours
            });
        }

        [HttpGet]
        public async Task<IActionResult> WeeklyHistory(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var archives = await _archiveService.GetArchivesAsync(page, 20);
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            ViewBag.Page = page;
            return View(archives);
        }
    }
}
