using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.App.Attributes;
using TodoApi.App.Storage;
using TodoApi.App.Models.v1;
using Microsoft.AspNetCore.Authorization;

namespace TodoApi.App.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiRoute("item")]
    public class RemindersController(TodoApiDbContext db) : ControllerBase
    {
        private readonly TodoApiDbContext _db = db;

        [Authorize]
        [HttpPost("{todoId}/reminder")]
        public async Task<ActionResult<Reminder>> CreateReminder([FromRoute] string todoId, [FromBody] ReminderCreateRequestBody reminder)
        {
            if (_db.ReminderV1.Any(r => r.TodoItemId == todoId && r.ReminderDate == reminder.ReminderDate))
            {
                return Conflict("A reminder for this todo item at the specified date and time already exists.");
            }

            var newReminder = new Reminder
            {
                Id = Guid.NewGuid().ToString(),
                TodoItemId = todoId,
                Message = reminder.Message,
                IsSent = false,
                ReminderDate = reminder.ReminderDate,
            };

            _db.ReminderV1.Add(newReminder);
            await _db.SaveChangesAsync();

            return Ok(newReminder);
        }

        [Authorize]
        [HttpGet("{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> GetReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var foundReminder = await _db.ReminderV1.Where(i => i.TodoItemId == todoId && i.Id == reminderId).FirstOrDefaultAsync();

            if (foundReminder == null)
            {
                return NotFound("Reminder not found.");
            }

            return Ok(foundReminder);
        }
        [Authorize]
        [HttpGet("{todoId}/reminders")]
        public async Task<IEnumerable<Reminder>> GetAllReminders([FromRoute] string todoId)
        {
            return await _db.ReminderV1.Where(i => i.TodoItemId == todoId).ToListAsync();
        }
        [Authorize]
        [HttpPut("{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> UpdateReminder([FromRoute] string todoId, [FromRoute] string reminderId, [FromBody] ReminderUpdateRequestBody reminder)
        {
            var foundReminder = await _db.ReminderV1.Where(i => i.TodoItemId == todoId && i.Id == reminderId).FirstOrDefaultAsync();

            if (foundReminder == null)
            {
                return NotFound("Reminder not found.");
            }

            foundReminder.Message = reminder.Message ?? foundReminder.Message;
            foundReminder.ReminderDate = reminder.ReminderDate ?? foundReminder.ReminderDate;

            _db.ReminderV1.Update(foundReminder);
            await _db.SaveChangesAsync();

            return Ok(foundReminder);
        }
        [Authorize]
        [HttpDelete("{todoId}/reminder/{reminderId}")]
        public async Task<ActionResult<Reminder>> DeleteReminder([FromRoute] string todoId, [FromRoute] string reminderId)
        {
            var foundReminder = await _db.ReminderV1.Where(i => i.TodoItemId == todoId && i.Id == reminderId).FirstOrDefaultAsync();

            if (foundReminder == null)
            {
                return NotFound("Reminder not found.");
            }

            _db.ReminderV1.Remove(foundReminder);
            await _db.SaveChangesAsync();

            return Ok("Reminder deleted successfully.");
        }

    }

}