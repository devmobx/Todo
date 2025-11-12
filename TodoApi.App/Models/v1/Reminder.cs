using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.App.Attributes.Validators;

namespace TodoApi.App.Models.v1
{
    public class Reminder
    {
        public required string Id { get; set; }
        public required string TodoItemId { get; set; }
        public required string Message { get; set; }
        public required bool IsSent { get; set; }
        public required DateTime ReminderDate { get; set; }

        public static void Build(EntityTypeBuilder<Reminder> builder)
        {
            builder.ToContainer("Reminders");
            builder.HasNoDiscriminator();

            builder.Property(x => x.Id).ToJsonProperty("id");
            builder.Property(x => x.TodoItemId).ToJsonProperty("todoItemId");
            builder.Property(x => x.Message).ToJsonProperty("message");
            builder.Property(x => x.IsSent).ToJsonProperty("isSent");
            builder.Property(x => x.ReminderDate).ToJsonProperty("reminderDate");

            builder.HasPartitionKey(x => x.TodoItemId);
            builder.HasKey(x => x.TodoItemId);
        }

    }

    public class ReminderCreateRequestBody
    {
        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string Message { get; set; } = "";

        [Required]
        [FutureDate]
        public DateTime ReminderDate { get; set; }
    }

    public class ReminderUpdateRequestBody
    {
        [MaxLength(500)]
        [MinLength(1)]
        public string? Message { get; set; }

        [FutureDate]
        public DateTime? ReminderDate { get; set; }
    }
}