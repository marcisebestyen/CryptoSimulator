namespace CryptoSimulator.Entities
{
    public enum CardNetwork
    {
        Visa,
        Mastercard,
        Amex
    }

    public class SavedCard
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string HolderName { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public CardNetwork Network { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
    }
}
