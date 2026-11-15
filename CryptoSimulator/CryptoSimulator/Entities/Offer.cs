namespace CryptoSimulator.Entities
{
    public class Offer
    {
        public int Id { get; set; }
        public int SellerWalletId { get; set; }
        public Wallet SellerWallet { get; set; } = null!;
        public int CryptoId { get; set; }
        public Crypto Crypto { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AskingPrice { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
