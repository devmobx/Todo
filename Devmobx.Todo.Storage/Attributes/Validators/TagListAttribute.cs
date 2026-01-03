using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Devmobx.Todo.Storage.Attributes.Validators
{
    public class TagListAttribute : ValidationAttribute
    {
        public int ListLength { get; set; }
        public int TagLength { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            if (value is not List<string> tagList)
                return ValidationResult.Success;

            if (tagList.Count > TagLength)
            {
                return new ValidationResult($"The length of the tags list must be no longer than {TagLength}.");
            }

            var distinctCount = tagList.Select(t => t?.Trim().ToLowerInvariant()).Distinct().Count();

            if (distinctCount != tagList.Count)
            {
                return new ValidationResult("All tags must be unique.");
            }

            foreach (var item in tagList)
            {
                if (item is not string tag)
                {
                    return new ValidationResult("Each tag must be a string.");
                }

                var length = tag.Length;
                if (length > TagLength)
                {
                    return new ValidationResult(
                        $"Each tag must be no longer than {TagLength} characters.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
