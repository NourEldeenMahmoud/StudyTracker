using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Models;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var count = await _notificationService.GetUnreadCountAsync(user.Id);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var items = await _notificationService.GetRecentAsync(user.Id, 20);
            var payload = items.Select(n => new
            {
                id = n.Id,
                type = n.Type,
                message = n.Message,
                isRead = n.IsRead,
                createdAtUtc = n.CreatedAtUtc
            });
            return Json(new { success = true, notifications = payload });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead([FromBody] Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            await _notificationService.MarkAsReadAsync(user.Id, id);
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return Json(new { success = true });
        }
    }
}

