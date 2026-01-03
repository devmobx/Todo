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
                Tags = item.Tags,
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
            foundItem.DueDate = item.DueDate != default ? item.DueDate : foundItem.DueDate;
            foundItem.Tags = item.Tags ?? foundItem.Tags;
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

            var reminders = _db.ReminderV1.Where(r => r.TodoItemId == todoId);

            _db.ReminderV1.RemoveRange(reminders);
            _db.TodoItemV1.Remove(foundItem);
            await _db.SaveChangesAsync();

            return Ok("Todo item deleted successfully.");
        }
    }
}
