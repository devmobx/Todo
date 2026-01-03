using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Devmobx.Todo.Storage.Attributes.Validators;

namespace Devmobx.Todo.Storage.Models.v1
{
    public class User
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public string PasswordHash { get; set; }
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static void Build(EntityTypeBuilder<User> builder)
        {
            builder.ToContainer("Users");
            builder.HasNoDiscriminator();

            builder.Property(x => x.Id).ToJsonProperty("id");
            builder.Property(x => x.FirstName).ToJsonProperty("firstName");
            builder.Property(x => x.LastName).ToJsonProperty("lastName");
            builder.Property(x => x.Email).ToJsonProperty("email");
            builder.Property(x => x.BirthDate).ToJsonProperty("birthDate");
            builder.Property(x => x.PasswordHash).ToJsonProperty("passwordHash");
            builder.Property(x => x.ResetToken).ToJsonProperty("resetToken");
            builder.Property(x => x.ResetTokenExpiry).ToJsonProperty("resetTokenExpiry");
            builder.Property(x => x.IsEmailConfirmed).ToJsonProperty("isEmailConfirmed");
            builder.Property(x => x.CreatedAt).ToJsonProperty("createdAt");

            builder.HasPartitionKey(x => x.Id);
            builder.HasKey(x => x.Id);
        }
    }

    class CreateUser
    {
        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [PastDate(MinimumYearsInPast = 12)]
        public DateTime BirthDate { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }
    }

    class UpdateUser
    {
        [MaxLength(100)]
        [MinLength(1)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        [MinLength(1)]
        public string LastName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [PastDate(MinimumYearsInPast = 12)]
        public DateTime BirthDate { get; set; }
    }
}
