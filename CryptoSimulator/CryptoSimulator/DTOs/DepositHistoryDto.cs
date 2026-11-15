namespace CryptoSimulator.DTOs
{
    public class DepositHistoryDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Last4 { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
