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
    public class TargetController : Controller
    {
        private readonly ITargetService _targetService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TargetController(ITargetService targetService, UserManager<ApplicationUser> userManager)
        {
            _targetService = targetService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var target = await _targetService.GetCurrentTargetAsync(user.Id);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dailyProgress = await _targetService.CalculateDailyProgressAsync(user.Id, today);

            ViewBag.Target = target;
            ViewBag.DailyProgress = dailyProgress;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
            var existing = await _targetService.GetCurrentTargetAsync(user.Id);

            var model = new WeeklyTargetViewModel
            {
                WeekStartDate = weekStart,
                DailyTargetHours = existing != null ? existing.DailyTargetMinutes / 60.0 : 3.0
            };

            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(WeeklyTargetViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserName = user.FullName;
                ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            }
            
            // Calculate week start date automatically (Saturday to Saturday)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            model.WeekStartDate = DateHelper.GetWeekStartDate(today);
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (user == null)
                return RedirectToAction("Login", "Account");

            await _targetService.CreateOrUpdateTargetAsync(user.Id, model);
            TempData["SuccessMessage"] = "Weekly target set successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> WeeklyProgress()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekStart = DateHelper.GetWeekStartDate(today);
            var progress = await _targetService.GetWeeklyProgressAsync(user.Id, weekStart);

            ViewBag.Progress = progress;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View();
        }
    }
}
