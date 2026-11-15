namespace CryptoSimulator.DTOs
{
    public class MarketOfferDto
    {
        public int Id { get; set; }
        public string SellerUsername { get; set; } = string.Empty;
        public decimal? SellerCompletionRate { get; set; }
        public int SellerTrades { get; set; }
        public int CryptoId { get; set; }
        public string CryptoName { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
        public decimal AskingPrice { get; set; }
        public decimal CurrentRate { get; set; }
    }
}
