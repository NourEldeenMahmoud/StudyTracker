using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Models;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityController(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var discordUrl = _configuration["Community:DiscordInviteUrl"]?.Trim() ?? "";
            var configuredBugUrl = _configuration["Community:BugReportUrl"]?.Trim() ?? "";
            ViewBag.DiscordInviteUrl = discordUrl;
            ViewBag.BugReportUrl = !string.IsNullOrEmpty(configuredBugUrl) ? configuredBugUrl : discordUrl;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;

            return View();
        }
    }
}
