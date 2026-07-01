using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Models;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class BadgeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBadgeService _badgeService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BadgeController(ApplicationDbContext context, IBadgeService badgeService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _badgeService = badgeService;
            _userManager = userManager;
        }

        /// <summary>
        /// Returns unnotified badges for the current user and marks them as notified.
        /// Called automatically on every page load via JS in _UserLayout.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PendingNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { badges = Array.Empty<object>() });

            List<StudyTracker.Models.UserBadge> pending;
            try
            {
                pending = await _context.UserBadges
                    .Where(b => b.UserId == user.Id && !b.IsNotified)
                    .ToListAsync();
            }
            catch
            {
                return Json(new { badges = Array.Empty<object>() });
            }

            if (pending.Count == 0)
                return Json(new { badges = Array.Empty<object>() });

            var definitions = _badgeService.GetAllDefinitions();
            List<StudyTracker.Models.CustomBadgeDefinition> customDefs;
            try { customDefs = await _badgeService.GetCustomDefinitionsAsync(); }
            catch { customDefs = new List<StudyTracker.Models.CustomBadgeDefinition>(); }

            var payload = pending
                .Select(b =>
                {
                    // Try predefined first, then custom
                    var def = definitions.FirstOrDefault(d => d.Key == b.BadgeKey);
                    if (def is not null)
                        return new { key = b.BadgeKey, name = def.Name, icon = def.Icon, description = def.Description };

                    if (Guid.TryParse(b.BadgeKey, out var gid))
                    {
                        var cd = customDefs.FirstOrDefault(d => d.Id == gid);
                        if (cd is not null)
                            return new { key = b.BadgeKey, name = cd.Name, icon = cd.Icon, description = cd.Description };
                    }

                    return new { key = b.BadgeKey, name = b.BadgeKey, icon = "military_tech", description = string.Empty };
                })
                .ToList();

            foreach (var b in pending)
                b.IsNotified = true;

            try { await _context.SaveChangesAsync(); } catch { /* ignore */ }

            return Json(new { badges = payload });
        }
    }
}
