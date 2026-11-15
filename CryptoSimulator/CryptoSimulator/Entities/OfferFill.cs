namespace CryptoSimulator.Entities
{
    public class OfferFill
    {
        public int Id { get; set; }
        public int OfferId { get; set; }
        public Offer Offer { get; set; } = null!;
        public int BuyerWalletId { get; set; }
        public Wallet BuyerWallet { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
    }
}
