namespace CryptoSimulator.Entities
{
    public class Transactions
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsPurchase { get; set; }

        public Wallet Wallet { get; set; } 
        public Crypto Crypto { get; set; } 
    }
}
