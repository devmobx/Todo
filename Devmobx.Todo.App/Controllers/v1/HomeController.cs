using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Devmobx.Todo.Storage;
using Devmobx.Todo.Storage.Models.v1;

namespace Devmobx.Todo.App.Controllers.v1
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DataBaseContext _db;

        public HomeController(DataBaseContext db)
        {
            _db = db;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("oid")
                ?? User.FindFirstValue("sub")
                ?? "";
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var todos = await _db.TodoItemV1
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(todos);
        }

        // CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return RedirectToAction("Index");
            }

            var userId = GetUserId();

            var todo = new TodoItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Title = title,
                IsCompleted = false,
                Priority = 1,
                Tags = new List<string>(),
                Reminders = new List<Reminder>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TodoItemV1.Add(todo);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // TOGGLE COMPLETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string id)
        {
            var userId = GetUserId();
            var todo = await _db.TodoItemV1.FindAsync(id);

            if (todo != null && todo.UserId == userId)
            {
                todo.IsCompleted = !todo.IsCompleted;
                todo.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = GetUserId();
            var todo = await _db.TodoItemV1.FindAsync(id);

            if (todo != null && todo.UserId == userId)
            {
                _db.TodoItemV1.Remove(todo);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
