namespace CryptoSimulator.DTOs
{
    public class PortfolioProfitLossDto
    {
        public int UserId { get; set; }
        public decimal TotalCurrentCryptoValue { get; set; }
        public decimal TotalCostBasis { get; set; }
        public decimal TotalProfitLossAmount { get; set; }
        public decimal TotalProfitLossPercentage { get; set; }
    }
}
