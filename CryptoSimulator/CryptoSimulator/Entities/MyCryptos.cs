namespace CryptoSimulator.Entities
{
    public class MyCryptos
    {
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }

        public Wallet Wallet { get; set; }
        public Crypto Crypto { get; set; } 
    }
}
