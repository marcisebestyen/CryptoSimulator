using System.ComponentModel.DataAnnotations;

namespace CryptoSimulator.DTOs
{
    public class WalletPutDto
    {
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Balance cannot be negative.")]
        public decimal Balance { get; set; }
    }
}
