using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class UserForgotPasswordDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string RecoveryCode { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
