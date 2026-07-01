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
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IImageService _imageService;
        private readonly IBadgeService _badgeService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly IStudySessionService _studySessionService;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IImageService imageService,
            IBadgeService badgeService,
            ILeaderboardService leaderboardService,
            IStudySessionService studySessionService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _imageService = imageService;
            _badgeService = badgeService;
            _leaderboardService = leaderboardService;
            _studySessionService = studySessionService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            List<StudyTracker.Models.CustomBadgeDefinition> customDefs;
            try { customDefs = await _badgeService.GetCustomDefinitionsAsync(); }
            catch { customDefs = new List<StudyTracker.Models.CustomBadgeDefinition>(); }

            var stats = await ComputeProfileStatsAsync(user.Id);

            ViewBag.User = user;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            ViewBag.BadgeGroups = await _badgeService.GetProfileBadgeGroupsAsync(user.Id);

            List<StudyTracker.Models.UserBadge> earnedBadges;
            try
            {
                earnedBadges = await _badgeService.GetUserBadgesAsync(user.Id);
            }
            catch
            {
                earnedBadges = new List<StudyTracker.Models.UserBadge>();
            }

            ViewBag.EarnedBadges = earnedBadges;
            ViewBag.CustomDefs = customDefs;
            ViewBag.ProfileStats = stats;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                CurrentProfilePictureUrl = user.ProfilePictureUrl
            };

            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserName = user.FullName;
                ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            }
            
            if (!ModelState.IsValid)
            {
                if (user != null)
                {
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                }
                return View(model);
            }

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Handle profile picture upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                try
                {
                    // Validate image using ImageService
                    if (!await _imageService.ValidateImageAsync(model.ProfilePicture))
                {
                        ModelState.AddModelError("ProfilePicture", "Invalid image file. Only JPG, PNG, and GIF files up to 5MB are allowed.");
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                        await _imageService.DeleteProfilePictureAsync(user.ProfilePictureUrl);
                }

                    // Upload and process new profile picture
                    user.ProfilePictureUrl = await _imageService.UploadProfilePictureAsync(model.ProfilePicture, user.Id);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ProfilePicture", ex.Message);
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("ProfilePicture", "An error occurred while uploading the image. Please try again.");
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }
            }

            user.FullName = model.FullName;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "Password changed successfully.";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> MiniProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var today = TimeZoneHelper.GetTodayInCairo();
            var weekStart = DateHelper.GetWeekStartDate(today);

            var dailyLeaderboard = await _leaderboardService.GetDailyLeaderboardAsync(today, user.Id);
            var weeklyLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart, user.Id);

            var dailyRank = dailyLeaderboard.FirstOrDefault(e => e.UserId == user.Id)?.Rank ?? 0;
            var weeklyRank = weeklyLeaderboard.FirstOrDefault(e => e.UserId == user.Id)?.Rank ?? 0;

            var totalMinutesToday = await _studySessionService.GetDailyTotalAsync(user.Id, today);
            var allSessions = await _studySessionService.GetUserSessionsAsync(user.Id, null, null);
            var totalAllMinutes = allSessions.Sum(s => s.DurationMinutes);

            var streak = 0;

            List<UserBadge> earnedBadges;
            try
            {
                earnedBadges = await _badgeService.GetUserBadgesAsync(user.Id);
            }
            catch
            {
                earnedBadges = new List<UserBadge>();
            }

            var definitions = _badgeService.GetAllDefinitions();
            List<CustomBadgeDefinition> customDefs;
            try { customDefs = await _badgeService.GetCustomDefinitionsAsync(); }
            catch { customDefs = new List<CustomBadgeDefinition>(); }

            var topBadges = earnedBadges
                .OrderByDescending(b => b.EarnedAt)
                .Select(b =>
                {
                    var def = definitions.FirstOrDefault(d => d.Key == b.BadgeKey);
                    if (def != null)
                    {
                        return new
                        {
                            icon = def.Icon,
                            color = def.Color,
                            name = def.Name
                        };
                    }

                    if (Guid.TryParse(b.BadgeKey, out var gid))
                    {
                        var cd = customDefs.FirstOrDefault(d => d.Id == gid);
                        if (cd != null)
                        {
                            return new
                            {
                                icon = cd.Icon,
                                color = cd.Color,
                                name = cd.Name
                            };
                        }
                    }

                    return null;
                })
                .Where(x => x != null)
                .Take(3)
                .ToList();

            return Json(new
            {
                fullName = user.FullName,
                profilePictureUrl = user.ProfilePictureUrl,
                dailyRank,
                weeklyRank,
                totalHours = totalAllMinutes / 60.0,
                todayMinutes = totalMinutesToday,
                streak,
                badges = topBadges
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Public(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = Helpers.DateHelper.GetWeekStartDate(today);

            var weeklyLeaderboard = await _leaderboardService.GetWeeklyLeaderboardAsync(weekStart, user.Id);
            var weeklyEntry = weeklyLeaderboard.FirstOrDefault(e => e.UserId == user.Id);

            var allSessions = await _studySessionService.GetUserSessionsAsync(user.Id, null, null);
            var totalAllMinutes = allSessions.Sum(s => s.DurationMinutes);
            var totalTodayMinutes = await _studySessionService.GetDailyTotalAsync(user.Id, today);

            List<CustomBadgeDefinition> customDefs;
            try { customDefs = await _badgeService.GetCustomDefinitionsAsync(); }
            catch { customDefs = new List<CustomBadgeDefinition>(); }

            var stats = await ComputeProfileStatsAsync(user.Id);

            ViewBag.PublicUser = user;
            ViewBag.TotalHours = totalAllMinutes / 60.0;
            ViewBag.TodayMinutes = totalTodayMinutes;
            ViewBag.WeeklyRank = weeklyEntry?.Rank ?? 0;
            ViewBag.WeeklyHours = weeklyEntry?.TotalHours ?? 0;
            ViewBag.BadgeGroups = await _badgeService.GetProfileBadgeGroupsAsync(user.Id);

            List<UserBadge> earnedBadges;
            try
            {
                earnedBadges = await _badgeService.GetUserBadgesAsync(user.Id);
            }
            catch
            {
                earnedBadges = new List<UserBadge>();
            }

            ViewBag.EarnedBadges = earnedBadges;
            ViewBag.CustomDefs = customDefs;
            ViewBag.ProfileStats = stats;

            return View();
        }

        private async Task<ProfileStatsDto> ComputeProfileStatsAsync(string userId)
        {
            var sessions = await _context.StudySessions
                .Where(s => s.UserId == userId)
                .Select(s => new { s.Date, s.DurationMinutes })
                .ToListAsync();

            var totalMinutes = sessions.Sum(s => s.DurationMinutes);
            var distinctStudyDays = sessions.Select(s => s.Date).Distinct().Count();
            var avgHoursPerDay = distinctStudyDays > 0 ? (totalMinutes / 60.0) / distinctStudyDays : 0;
            var firstSessionDate = sessions.Any() ? sessions.Min(s => s.Date) : (DateOnly?)null;

            var weeklyTotals = sessions
                .GroupBy(s => DateHelper.GetWeekStartDate(s.Date))
                .Select(g => new { Week = g.Key, Total = g.Sum(x => x.DurationMinutes) })
                .ToList();

            var bestWeekMinutes = weeklyTotals.Any() ? weeklyTotals.Max(w => w.Total) : 0;
            var bestWeekStart = weeklyTotals.FirstOrDefault(w => w.Total == bestWeekMinutes)?.Week;
            var totalWeeks = weeklyTotals.Count;
            var avgMinutesPerWeek = totalWeeks > 0 ? totalMinutes / (double)totalWeeks : 0;

            var sessionDates = new HashSet<DateOnly>(sessions.Select(s => s.Date));
            var today = TimeZoneHelper.GetTodayInCairo();
            var streakDays = 0;
            var check = sessionDates.Contains(today) ? today : today.AddDays(-1);
            while (sessionDates.Contains(check))
            {
                streakDays++;
                check = check.AddDays(-1);
            }

            return new ProfileStatsDto
            {
                TotalHours = totalMinutes / 60.0,
                AvgHoursPerDay = avgHoursPerDay,
                FirstSessionDate = firstSessionDate,
                BestWeekHours = bestWeekMinutes / 60.0,
                BestWeekStart = bestWeekStart,
                AvgHoursPerWeek = avgMinutesPerWeek / 60.0,
                StreakDays = streakDays
            };
        }
    }

    public class ProfileStatsDto
    {
        public double TotalHours { get; set; }
        /// <summary>Average study hours per calendar day that has at least one logged session.</summary>
        public double AvgHoursPerDay { get; set; }
        public DateOnly? FirstSessionDate { get; set; }
        public double BestWeekHours { get; set; }
        public DateOnly? BestWeekStart { get; set; }
        public double AvgHoursPerWeek { get; set; }
        public int StreakDays { get; set; }
    }
}
