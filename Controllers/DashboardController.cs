using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public DashboardController(IDashboardService dashboardService, ILeaderboardService leaderboardService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _leaderboardService = leaderboardService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var targetProgress = await _dashboardService.GetWeeklyTargetProgressAsync(user.Id);

            // Get full weekly leaderboard for the table
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
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
                    profilePictureUrl = e.ProfilePictureUrl
                }).ToList(),
                dailyLeaderboard = dailyLeaderboard.Select(e => new
                {
                    rank = e.Rank,
                    userId = e.UserId,
                    fullName = e.FullName,
                    totalHours = e.TotalHours,
                    sessionsCount = e.SessionsCount,
                    isCurrentUser = e.IsCurrentUser,
                    profilePictureUrl = e.ProfilePictureUrl
                }).ToList(),
                currentUserRank = currentUserRank,
                currentUserHours = currentUserHours
            });
        }
    }
}
