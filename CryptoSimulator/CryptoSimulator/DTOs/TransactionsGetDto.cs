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
    }

    public class TransactionsPutDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime Date { get; set; }
        public bool IsPurchase { get; set; }
    }

    public class TransactionsPostDto
    {
        public int WalletId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
        public decimal ExchangeRate { get; set; }
        public bool IsPurchase { get; set; }
    }
}
