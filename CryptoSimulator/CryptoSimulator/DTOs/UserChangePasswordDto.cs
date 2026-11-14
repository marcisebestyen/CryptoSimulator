using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class UserChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmedNewPassword { get; set; }
    }
}
