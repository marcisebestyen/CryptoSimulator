using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class ManualPriceUpdateDto
    {
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal NewPrice { get; set; }
    }
}
