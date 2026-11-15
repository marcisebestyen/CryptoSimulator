using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class SellCryptoDto
    {
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        [Range(0.0001, (double)decimal.MaxValue, ErrorMessage = "Amount must be a positive number.")]
        public decimal Amount { get; set; }
    }
}
