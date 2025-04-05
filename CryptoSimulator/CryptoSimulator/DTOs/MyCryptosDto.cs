namespace CryptoSimulator.DTOs
{
    public class MyCryptosDto // used for Get, Put, Push as well
    {
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
    }
}
