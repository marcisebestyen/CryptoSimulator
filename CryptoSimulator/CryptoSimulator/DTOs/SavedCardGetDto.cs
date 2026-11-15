namespace CryptoSimulator.DTOs
{
    public class SavedCardGetDto
    {
        public int Id { get; set; }
        public string HolderName { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
    }
}
