using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class BuyOfferDto
    {
        [Range(0.0001, (double)decimal.MaxValue, ErrorMessage = "Amount must be a positive number.")]
        public decimal Amount { get; set; }
    }
}
