namespace CryptoSimulator.DTOs
{
    public class CryptoLogDto // used for Get, Put, Push as well
    {
        public int CryptoId { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
