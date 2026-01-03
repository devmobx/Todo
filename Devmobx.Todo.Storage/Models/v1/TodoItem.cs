using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Devmobx.Todo.Storage.Attributes.Validators;

namespace Devmobx.Todo.Storage.Models.v1
{
    public class TodoItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public bool IsCompleted { get; set; }
        public List<string> Tags { get; set; } = [];
        public DateTime? DueDate { get; set; }
        public List<Reminder> Reminders { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static void Build(EntityTypeBuilder<TodoItem> builder)
        {
            builder.ToContainer("TodoItems");
            builder.HasNoDiscriminator();

            builder.Property(x => x.Id).ToJsonProperty("id");
            builder.Property(x => x.UserId).ToJsonProperty("userId");
            builder.Property(x => x.Title).ToJsonProperty("title");
            builder.Property(x => x.Description).ToJsonProperty("description");
            builder.Property(x => x.Priority).ToJsonProperty("priority");
            builder.Property(x => x.IsCompleted).ToJsonProperty("isCompleted");
            builder.Property(x => x.DueDate).ToJsonProperty("dueDate");
            builder.Property(x => x.CreatedAt).ToJsonProperty("createdAt");
            builder.Property(x => x.UpdatedAt).ToJsonProperty("updatedAt");
            builder.Property(x => x.Tags).ToJsonProperty("tags");
            builder.OwnsMany(
                x => x.Reminders,
                ownedBuilder =>
                {
                    ownedBuilder.ToJsonProperty("reminders");
                    ownedBuilder.Property(r => r.Id).ToJsonProperty("id");
                    ownedBuilder.Property(r => r.Message).ToJsonProperty("message");
                    ownedBuilder.Property(r => r.IsSent).ToJsonProperty("isSent");
                    ownedBuilder.Property(r => r.ReminderDate).ToJsonProperty("reminderDate");
                });

            builder.HasPartitionKey(x => x.UserId);
            builder.HasKey(x => x.Id);
        }
    }

    public class Reminder
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public bool IsSent { get; set; }
        public DateTime ReminderDate { get; set; }
    }

    public class TodoItemCreateRequestBody
    {
        [Required]
        [MaxLength(50)]
        [MinLength(1)]
        public string Title { get; set; }

        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string Description { get; set; }

        [Required]
        [Range(1, 5)]
        public int Priority { get; set; }

        [FutureDate]
        public DateTime? DueDate { get; set; }

        [TagList(ListLength = 5, TagLength = 10)]
        public List<string> Tags { get; set; }
    }

    public class TodoItemUpdateRequestBody
    {
        [MaxLength(50)]
        [MinLength(1)]
        public string? Title { get; set; }

        [MaxLength(100)]
        [MinLength(1)]
        public string? Description { get; set; }

        [Range(1, 5)]
        public int? Priority { get; set; }

        [FutureDate]
        public DateTime? DueDate { get; set; }

        public bool? IsCompleted { get; set; }

        [TagList(ListLength = 5, TagLength = 10)]
        public List<string>? Tags { get; set; }
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
