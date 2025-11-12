using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.App.Attributes.Validators;

namespace TodoApi.App.Models.v1
{
    public class User
    {
        public required string Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required DateTime BirthDate { get; set; }

        public static void Build(EntityTypeBuilder<User> builder)
        {
            builder.ToContainer("Users");
            builder.HasNoDiscriminator();

            builder.Property(x => x.Id).ToJsonProperty("id");
            builder.Property(x => x.FirstName).ToJsonProperty("firstName");
            builder.Property(x => x.LastName).ToJsonProperty("lastName");
            builder.Property(x => x.Email).ToJsonProperty("email");
            builder.Property(x => x.BirthDate).ToJsonProperty("birthDate");

            builder.HasPartitionKey(x => x.Id);
            builder.HasKey(x => x.Id);
        }
    }

    class CreateUser
    {
        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [MinLength(1)]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [PastDate(MinimumYearsInPast = 12)]
        public DateTime BirthDate { get; set; }
    }

    class UpdateUser
    {
        [MaxLength(100)]
        [MinLength(1)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        [MinLength(1)]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [PastDate(MinimumYearsInPast = 12)]
        public DateTime? BirthDate { get; set; }
    }
}