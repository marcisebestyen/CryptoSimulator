using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class DepositCreateDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public int? SavedCardId { get; set; }
        public string? HolderName { get; set; }
        public string? CardNumber { get; set; }
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }

        [Required]
        public string Cvv { get; set; } = string.Empty;

        public bool SaveCard { get; set; }
    }
}
