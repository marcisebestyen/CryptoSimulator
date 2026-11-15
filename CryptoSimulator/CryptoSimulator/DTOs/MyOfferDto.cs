using CryptoSimulator.Entities;

namespace CryptoSimulator.DTOs
{
    public class MyOfferDto
    {
        public int Id { get; set; }
        public int CryptoId { get; set; }
        public string CryptoName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal SoldAmount { get; set; }
        public decimal AskingPrice { get; set; }
        public decimal CurrentRate { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
