using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;

namespace StudyTracker.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var recentLogs = await _context.SecurityLogs
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(50)
                .ToListAsync();

            ViewBag.RecentLogs = recentLogs;
            return View();
        }
    }
}

