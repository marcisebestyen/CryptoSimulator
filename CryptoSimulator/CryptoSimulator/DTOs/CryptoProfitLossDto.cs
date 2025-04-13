namespace CryptoSimulator.DTOs
{
    public class CryptoProfitLossDto
    {
        public int CryptoId { get; set; }
        public string CryptoName { get; set; }
        public decimal AmountOwned { get; set; }
        public decimal AvgPurchasePrice { get; set; }
        public decimal CurrentExchangeRate { get; set; }
        public decimal TotalCostBasis { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLossAmount { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }

    public class PortfolioProfitLossDto
    {
        public int UserId { get; set; }
        public decimal TotalCurrentCryptoValue { get; set; }
        public decimal TotalCostBasis { get; set; }
        public decimal TotalProfitLossAmount { get; set; }
        public decimal TotalProfitLossPercentage { get; set; }
    }
}
