using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Storage.Attributes.Validators;

namespace TodoApi.Storage.Models.v1
{
    public class Reminder
    {
        public string Id { get; set; }
        public string TodoItemId { get; set; }
        public string Message { get; set; }
        public bool IsSent { get; set; }
        public DateTime ReminderDate { get; set; }

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
        public string Message { get; set; }

        [FutureDate]
        public DateTime? ReminderDate { get; set; }
    }
}