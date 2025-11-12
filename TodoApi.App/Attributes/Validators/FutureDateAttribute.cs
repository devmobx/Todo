using System.ComponentModel.DataAnnotations;

namespace TodoApi.App.Attributes.Validators
{
    class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var fieldName = validationContext.MemberName;

            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (value is not DateTime dateTimeValue)
            {
                return new ValidationResult($"Invalid date format for {fieldName}");
            }

            if (dateTimeValue > DateTime.Now)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult($"{fieldName} must be a future date.");

        }
    }
}

