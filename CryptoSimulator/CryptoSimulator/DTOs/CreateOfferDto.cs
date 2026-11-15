using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class CreateOfferDto
    {
        public int CryptoId { get; set; }
        [Range(0.0001, (double)decimal.MaxValue, ErrorMessage = "Amount must be a positive number.")]
        public decimal Amount { get; set; }
        [Range(0.00000001, 9999999999.99999999, ErrorMessage = "Asking price must be a positive number.")]
        public decimal AskingPrice { get; set; }
    }
}
