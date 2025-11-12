using System.ComponentModel.DataAnnotations;


namespace TodoApi.App.Attributes.Validators
{
    public class PastDateAttribute : ValidationAttribute
    {
        public int MinimumYearsInPast { get; set; } = 0;

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

            if (dateTimeValue > DateTime.Today)
            {
                return new ValidationResult($"{fieldName} must be a past date.");
            }

            if (MinimumYearsInPast > 0)
            {
                var age = DateTime.Today.Year - dateTimeValue.Year;
                if (dateTimeValue.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < MinimumYearsInPast)
                {
                    return new ValidationResult($"{fieldName} must be at least {MinimumYearsInPast} years in the past.");
                }
            }

            return ValidationResult.Success;
        }
    }

}
