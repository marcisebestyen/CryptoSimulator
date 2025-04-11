namespace CryptoSimulator.Entities
{
    public class CryptoLog
    {
        public int CryptoId { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime From { get; set; } 
        public DateTime To { get; set; }

        public Crypto Crypto { get; set; } 
    }
}
