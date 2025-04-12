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

    public class PortfolioDto
    {
        public int UserId { get; set; }
        public decimal FiatBalance { get; set; }
        public List<PortfolioItemDto> Holdings { get; set; }
        public decimal TotalCryptoValue { get; set; }
        public decimal TotalPortfolioValue { get; set; }
    }
}
