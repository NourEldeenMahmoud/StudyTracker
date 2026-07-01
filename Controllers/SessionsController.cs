using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Helpers;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class SessionsController : Controller
    {
        private readonly IStudySessionService _sessionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;
        private readonly ISecurityLogService _securityLogService;
        private readonly INotificationService _notificationService;

        public SessionsController(IStudySessionService sessionService, UserManager<ApplicationUser> userManager, IBadgeService badgeService, ISecurityLogService securityLogService, INotificationService notificationService)
        {
            _sessionService = sessionService;
            _userManager = userManager;
            _badgeService = badgeService;
            _securityLogService = securityLogService;
            _notificationService = notificationService;
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Index(DateOnly? startDate, DateOnly? endDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var sessions = await _sessionService.GetUserSessionsAsync(user.Id, startDate, endDate);
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(sessions);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View(new AddSessionViewModel
            {
                Date = TimeZoneHelper.GetTodayInCairo()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Please check your input.";
                    return Json(new { success = false, message = firstError });
                }
                return View(model);
            }

            if (model.DurationMinutes < 10)
            {
                ModelState.AddModelError("", "Session duration must be at least 10 minutes.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Notes))
            {
                ModelState.AddModelError(nameof(model.Notes), "Please describe what you studied.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Please describe what you studied." });
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Dashboard quick log: always attribute to Cairo "today" (hidden date can be stale if the tab stayed open past midnight).
            if (Request.HasFormContentType && string.Equals(Request.Form["DashboardQuickLog"].ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                model.Date = TimeZoneHelper.GetTodayInCairo();
            }

            try
            {
                await _sessionService.AddSessionAsync(user.Id, model);

                List<StudyTracker.Models.UserBadge> newBadges;
                try
                {
                    newBadges = await _badgeService.CheckAndAwardAsync(user.Id);
                }
                catch
                {
                    newBadges = new List<StudyTracker.Models.UserBadge>();
                }
                var badgeDefs = _badgeService.GetAllDefinitions();

                if (newBadges.Any())
                {
                    foreach (var b in newBadges)
                    {
                        var def = badgeDefs.FirstOrDefault(d => d.Key == b.BadgeKey);
                        var name = def?.Name ?? b.BadgeKey;
                        await _notificationService.AddAsync(user.Id, "BadgeEarned", $"You earned a new badge: {name}");
                    }
                }

                // Milestone notifications based on total study hours
                var totalMinutes = await _sessionService.GetDailyTotalAsync(user.Id, DateOnly.MinValue); // reuse service later if extended

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var badgePayload = newBadges.Select(b =>
                    {
                        var def = badgeDefs.FirstOrDefault(d => d.Key == b.BadgeKey);
                        return new { key = b.BadgeKey, name = def?.Name ?? b.BadgeKey, icon = def?.Icon ?? "emoji_events", description = def?.Description ?? string.Empty };
                    }).ToList();

                    // Mark as notified — they will be shown inline by dashboard JS
                    if (newBadges.Any())
                    {
                        try { await _badgeService.MarkAsNotifiedAsync(newBadges.Select(b => b.Id)); }
                        catch { /* ignore */ }
                    }

                    return Json(new { success = true, message = "Session added successfully!", newBadges = badgePayload });
                }

                TempData["SuccessMessage"] = "Session added successfully!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (InvalidOperationException ex)
            {
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }
                
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = User.IsInRole("Admin");
            var session = await _sessionService.GetSessionByIdAsync(id);
            
            if (session == null)
                return NotFound();

            if (!isAdmin && session.UserId != user.Id)
                return Forbid();

            var canEdit = await _sessionService.CanEditSessionAsync(id, user.Id, isAdmin);
            if (!canEdit)
            {
                TempData["ErrorMessage"] = "Cannot edit session after 24 hours.";
                return RedirectToAction("Index");
            }

            var model = new AddSessionViewModel
            {
                Date = session.Date,
                Hours = session.DurationMinutes / 60,
                Minutes = session.DurationMinutes % 60,
                Notes = session.Notes ?? string.Empty
            };

            ViewBag.SessionId = id;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(AddSessionViewModel model, Guid id)
        {
            if (!ModelState.IsValid)
            {
                var userForView = await _userManager.GetUserAsync(User);
                if (userForView != null)
                {
                    ViewBag.UserName = userForView.FullName;
                    ViewBag.ProfilePictureUrl = userForView.ProfilePictureUrl;
                }
                ViewBag.SessionId = id;
                return View(model);
            }

            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError("", "Duration must be greater than 0.");
                var userForView = await _userManager.GetUserAsync(User);
                if (userForView != null)
                {
                    ViewBag.UserName = userForView.FullName;
                    ViewBag.ProfilePictureUrl = userForView.ProfilePictureUrl;
                }
                ViewBag.SessionId = id;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = User.IsInRole("Admin");

            try
            {
                var success = await _sessionService.UpdateSessionAsync(id, user.Id, model, isAdmin);
                if (!success)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Session updated successfully!";
                
                // Redirect based on user role
                if (isAdmin)
                {
                    return RedirectToAction("Sessions", "Admin");
                }
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.UserName = user.FullName;
                ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
                ViewBag.SessionId = id;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isAdmin = User.IsInRole("Admin");

            if (isAdmin && !isSuperAdmin)
            {
                await _securityLogService.LogUnauthorizedDeleteAsync(
                    User,
                    "DeleteSession",
                    "StudySession",
                    id.ToString(),
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Forbid();
            }

            var success = await _sessionService.DeleteSessionAsync(id, user.Id, isSuperAdmin);

            if (!success)
            {
                TempData["ErrorMessage"] = "Failed to delete session.";
            }
            else
            {
                TempData["SuccessMessage"] = "Session deleted successfully!";
            }

            // Redirect based on user role
            if (isAdmin)
            {
                return RedirectToAction("Sessions", "Admin");
            }
            return RedirectToAction("Index");
        }
    }
}
