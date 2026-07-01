using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Helpers;
using StudyTracker.Models;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class TodoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TodoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Todo — dedicated mobile todo page
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Todo/Today — returns all active todos as JSON (used by Dashboard AJAX)
        [HttpGet]
        public async Task<IActionResult> Today()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var today = TimeZoneHelper.GetTodayInCairo();
            var items = await _context.TodoItems
                .Where(t => t.UserId == user.Id)
                .OrderBy(t => t.Order == 0 ? 1 : 0) // items with explicit order first
                .ThenBy(t => t.Order)
                .ThenBy(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.IsCompleted,
                    t.CreatedAt,
                    t.Date
                })
                .ToListAsync();

            return Json(items);
        }

        // POST: /Todo/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromBody] AddTodoRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request?.Title))
                return BadRequest(new { error = "Title is required." });

            var item = new TodoItem
            {
                UserId = user.Id,
                Title = request.Title.Trim(),
                IsCompleted = false,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                Order = 0
            };

            _context.TodoItems.Add(item);
            await _context.SaveChangesAsync();

            return Json(new { item.Id, item.Title, item.IsCompleted });
        }

        // POST: /Todo/Toggle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle([FromBody] IdRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == user.Id);

            if (item == null) return NotFound();

            item.IsCompleted = !item.IsCompleted;
            await _context.SaveChangesAsync();

            return Json(new { item.Id, item.IsCompleted });
        }

        // POST: /Todo/ClearAll — deletes all todos for the current user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var items = await _context.TodoItems
                .Where(t => t.UserId == user.Id)
                .ToListAsync();

            _context.TodoItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true, removed = items.Count });
        }

        // POST: /Todo/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] IdRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == user.Id);

            if (item == null) return NotFound();

            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Todo/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (request == null || request.Order == null || request.Order.Count == 0)
                return BadRequest();

            var ids = request.Order;
            var items = await _context.TodoItems
                .Where(t => t.UserId == user.Id && ids.Contains(t.Id))
                .ToListAsync();

            var position = 1;
            foreach (var id in ids)
            {
                var item = items.FirstOrDefault(t => t.Id == id);
                if (item != null)
                {
                    item.Order = position++;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    public class AddTodoRequest
    {
        public string Title { get; set; } = string.Empty;
    }

    public class IdRequest
    {
        public Guid Id { get; set; }
    }

    public class ReorderRequest
    {
        public List<Guid> Order { get; set; } = new();
    }
}
