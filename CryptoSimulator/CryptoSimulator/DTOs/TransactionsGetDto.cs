namespace CryptoSimulator.DTOs
{
    public class TransactionsGetDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime Date { get; set; }
        public bool IsPurchase { get; set; }
        public string TransactionType => IsPurchase ? "Purchase" : "Sale";
    }
}
