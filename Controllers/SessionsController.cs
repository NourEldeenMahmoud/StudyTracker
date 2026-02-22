using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public SessionsController(IStudySessionService sessionService, UserManager<ApplicationUser> userManager)
        {
            _sessionService = sessionService;
            _userManager = userManager;
        }

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
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError("", "Duration must be greater than 0.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            try
            {
                await _sessionService.AddSessionAsync(user.Id, model);
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Session added successfully!" });
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
                Notes = session.Notes
            };

            ViewBag.SessionId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddSessionViewModel model, Guid id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError("", "Duration must be greater than 0.");
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
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = User.IsInRole("Admin");
            var success = await _sessionService.DeleteSessionAsync(id, user.Id, isAdmin);

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
