namespace CryptoSimulator.DTOs
{
    public class PortfolioDto
    {
        public int UserId { get; set; }
        public decimal FiatBalance { get; set; }
        public List<PortfolioItemDto> Holdings { get; set; }
        public decimal TotalCryptoValue { get; set; }
        public decimal TotalPortfolioValue { get; set; }
    }
}
