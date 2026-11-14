using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class CryptoPostDto
    {
        public string Name { get; set; }

        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal InitialPrice { get; set; }
    }
}
