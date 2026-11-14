namespace CryptoSimulator.DTOs
{
    public class PortfolioItemDto
    {
        public int CryptoId { get; set; }
        public string CryptoName { get; set; }
        public decimal AmountOwned { get; set; }
        public decimal CurrentExchangeRate { get; set; }
        public decimal CurrentValue { get; set; }
    }
}
