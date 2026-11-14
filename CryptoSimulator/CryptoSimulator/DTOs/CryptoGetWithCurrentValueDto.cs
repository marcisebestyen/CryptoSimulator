namespace CryptoSimulator.DTOs
{
    public class CryptoGetWithCurrentValueDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal CurrentExchangeRate { get; set; }
    }
}
