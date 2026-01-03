using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Storage;
using TodoApi.Storage.Models.v1;
using TodoApi.Core.Attributes;

namespace TodoApi.App.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiRoute("")]
    public class TodoItemsController(DataBaseContext db) : ControllerBase
    {
        private readonly DataBaseContext _db = db;

        [Authorize]
        [HttpPost("item")]
        public async Task<ActionResult<TodoItem>> CreateTodoItem([FromBody] TodoItemCreateRequestBody item)
        {
            if (await _db.TodoItemV1.AnyAsync(i => i.Title == item.Title && i.UserId == ""))
            {
                return Conflict("A todo item with the same title already exists.");
            }

            var newItem = new TodoItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "",
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

            return Ok(newItem);
        }

        [Authorize]
        [HttpGet("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem([FromRoute] string todoId)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            return Ok(foundItem);
        }

        [HttpGet("items")]
        public async Task<IEnumerable<TodoItem>> GetAllTodoItems()
        {
            return await _db.TodoItemV1.Where(i => i.UserId == "").ToListAsync();
        }

        [Authorize]
        [HttpPut("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> UpdateTodoItem([FromRoute] string todoId, [FromBody] TodoItemUpdateRequestBody item)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
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

            return Ok(foundItem);
        }

        [Authorize]
        [HttpDelete("item/{todoId}")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem([FromRoute] string todoId)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            _db.TodoItemV1.Remove(foundItem);
            await _db.SaveChangesAsync();

            return Ok("Todo item deleted successfully.");
        }

        // ============= REMINDER ENDPOINTS =============

        [Authorize]
        [HttpPost("item/{todoId}/reminder")]
        public async Task<ActionResult<Reminder>> CreateReminder([FromRoute] string todoId, [FromBody] ReminderCreateRequestBody reminder)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
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

            return Ok(newReminder);
        }

        [Authorize]
        [HttpGet("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> GetReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
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
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
            }

            return Ok(foundItem.Reminders);
        }

        [Authorize]
        [HttpPut("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> UpdateReminder([FromRoute] string todoId, [FromRoute] string reminderId, [FromBody] ReminderUpdateRequestBody reminder)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
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

            return Ok(foundReminder);
        }

        [Authorize]
        [HttpDelete("item/{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult> DeleteReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var foundItem = await _db.TodoItemV1.FindAsync(todoId);

            if (foundItem == null)
            {
                return NotFound("Todo item not found.");
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

            return Ok("Reminder deleted successfully.");
        }
    }
}
