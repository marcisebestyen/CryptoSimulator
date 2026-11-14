using CryptoSimulator.Entities;

namespace CryptoSimulator.DTOs
{
    public class PriceAlertGetDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        public string? CryptoName { get; set; }
        public decimal TargetPrice { get; set; }
        public PriceAlertType AlertType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }
}
