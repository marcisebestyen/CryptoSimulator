using CryptoSimulator.Entities;
using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class PriceAlertCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int CryptoId { get; set; }

        [Required]
        [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "Target price must be a positive number.")]
        public decimal TargetPrice { get; set; }

        [Required]
        // Validation in service layer ("above" or "below")
        public string AlertType { get; set; }
    }

    public class PriceAlertGetDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        public string? CryptoName { get; set; } 
        public decimal TargetPrice { get; set; }
        public PriceAlertType AlertType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; } 
    }
}
