using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Devmobx.Todo.Storage;
using Devmobx.Todo.Storage.Models.v1;
using Devmobx.Todo.Core.Attributes;
using System.Security.Claims;

namespace Devmobx.Todo.App.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiRoute("")]
    [Authorize]
    public class TodoItemsController(DataBaseContext db, ILogger<TodoItemsController> logger) : ControllerBase
    {
        private readonly DataBaseContext _db = db;
        private readonly ILogger<TodoItemsController> _logger = logger;

        /// <summary>
        /// Get the current user's ID from JWT token
        /// </summary>
        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("email")?.Value
                ?? "";
        }

        [Authorize]
        [HttpPost("item")]
        public async Task<ActionResult<TodoItem>> CreateTodoItem([FromBody] TodoItemCreateRequestBody item)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            if (await _db.TodoItemV1.AnyAsync(i => i.Title == item.Title && i.UserId == userId))
            {
                return Conflict("A todo item with the same title already exists.");
            }

            var newItem = new TodoItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Title = item.Title,
                Description = item.Description,
                DueDate = item.DueDate,
                IsCompleted = false,
                Priority = item.Priority,
                Tags = item.Tags ?? [],
                Reminders = [],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TodoItemV1.Add(newItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Todo item created: {TodoId} for user: {UserId}", newItem.Id, userId);
            return Ok(newItem);
        }

        [Authorize]
        [HttpGet("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem([FromRoute] string todoId)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to access this item");
            }

            return Ok(foundItem);
        }

        [Authorize]
        [HttpGet("items")]
        public async Task<IEnumerable<TodoItem>> GetAllTodoItems()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return [];
            }

            return await _db.TodoItemV1.Where(i => i.UserId == userId).ToListAsync();
        }

        [Authorize]
        [HttpPut("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> UpdateTodoItem([FromRoute] string todoId, [FromBody] TodoItemUpdateRequestBody item)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to update this item");
            }

            foundItem.Title = item.Title ?? foundItem.Title;
            foundItem.Description = item.Description ?? foundItem.Description;
            foundItem.Priority = item.Priority ?? foundItem.Priority;
            foundItem.DueDate = item.DueDate != default ? item.DueDate : foundItem.DueDate;
            foundItem.Tags = item.Tags ?? foundItem.Tags;
            foundItem.IsCompleted = item.IsCompleted ?? foundItem.IsCompleted;
            foundItem.UpdatedAt = DateTime.UtcNow;

            _db.TodoItemV1.Update(foundItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Todo item updated: {TodoId}", todoId);
            return Ok(foundItem);
        }

        [Authorize]
        [HttpDelete("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem([FromRoute] string todoId)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to delete this item");
            }

            _db.TodoItemV1.Remove(foundItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Todo item deleted: {TodoId}", todoId);
            return Ok("Todo item deleted successfully.");
        }

        // ============= REMINDER ENDPOINTS =============

        [Authorize]
        [HttpPost("item/{todoId}/reminder")]
        public async Task<ActionResult<Reminder>> CreateReminder([FromRoute] string todoId, [FromBody] ReminderCreateRequestBody reminder)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to modify this item");
            }

            if (foundItem.Reminders.Any(r => r.ReminderDate == reminder.ReminderDate))
            {
                return Conflict("A reminder for this todo item at the specified date and time already exists.");
            }

            var newReminder = new Reminder
            {
                Id = Guid.NewGuid().ToString(),
                Message = reminder.Message,
                IsSent = false,
                ReminderDate = reminder.ReminderDate
            };

            foundItem.Reminders.Add(newReminder);
            foundItem.UpdatedAt = DateTime.UtcNow;

            _db.TodoItemV1.Update(foundItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Reminder created: {ReminderId} for todo: {TodoId}", newReminder.Id, todoId);
            return Ok(newReminder);
        }

        [Authorize]
        [HttpGet("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> GetReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to access this item");
            }

            var reminder = foundItem.Reminders.FirstOrDefault(r => r.Id == reminderId);

            if (reminder == null)
            {
                return NotFound("Reminder not found.");
            }

            return Ok(reminder);
        }

        [Authorize]
        [HttpGet("item/{todoId}/reminders")]
        public async Task<ActionResult<IEnumerable<Reminder>>> GetAllReminders([FromRoute] string todoId)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to access this item");
            }

            return Ok(foundItem.Reminders);
        }

        [Authorize]
        [HttpPut("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> UpdateReminder([FromRoute] string todoId, [FromRoute] string reminderId, [FromBody] ReminderUpdateRequestBody reminder)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to modify this item");
            }

            var foundReminder = foundItem.Reminders.FirstOrDefault(r => r.Id == reminderId);

            if (foundReminder == null)
            {
                return NotFound("Reminder not found.");
            }

            foundReminder.Message = reminder.Message ?? foundReminder.Message;
            foundReminder.ReminderDate = reminder.ReminderDate ?? foundReminder.ReminderDate;

            foundItem.UpdatedAt = DateTime.UtcNow;

            _db.TodoItemV1.Update(foundItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Reminder updated: {ReminderId}", reminderId);
            return Ok(foundReminder);
        }

        [Authorize]
        [HttpDelete("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult> DeleteReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var userId = GetCurrentUserId();

            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            // Verify user owns this item
            if (foundItem.UserId != userId)
            {
                return Forbid("You don't have permission to modify this item");
            }

            var reminder = foundItem.Reminders.FirstOrDefault(r => r.Id == reminderId);

            if (reminder == null)
            {
                return NotFound("Reminder not found.");
            }

            foundItem.Reminders.Remove(reminder);
            foundItem.UpdatedAt = DateTime.UtcNow;

            _db.TodoItemV1.Update(foundItem);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Reminder deleted: {ReminderId}", reminderId);
            return Ok("Reminder deleted successfully.");
        }
    }
}
