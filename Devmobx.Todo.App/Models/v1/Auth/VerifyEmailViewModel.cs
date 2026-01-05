using System.ComponentModel.DataAnnotations;

namespace Devmobx.Todo.App.Models.v1.Auth
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Invalid code format")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}
