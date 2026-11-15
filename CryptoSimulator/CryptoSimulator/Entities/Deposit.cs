namespace CryptoSimulator.Entities
{
    public class Deposit
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; } = null!;
        public int? SavedCardId { get; set; }
        public SavedCard? SavedCard { get; set; }
        public string Last4 { get; set; } = string.Empty;
        public CardNetwork Network { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
