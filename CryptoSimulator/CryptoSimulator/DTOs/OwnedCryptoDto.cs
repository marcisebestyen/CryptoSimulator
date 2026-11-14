namespace CryptoSimulator.DTOs
{
    public class OwnedCryptoDto
    {
        public int CryptoId { get; set; }
        public string CryptoName { get; set; }
        public decimal Amount { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
    }
}
