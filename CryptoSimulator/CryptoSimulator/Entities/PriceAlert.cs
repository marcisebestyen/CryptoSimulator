using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoSimulator.Entities
{
    public enum PriceAlertType
    {
        Above,
        Below
    }

    public class PriceAlert
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int CryptoId { get; set; }
        public Crypto Crypto { get; set; } = null!;

        [Column(TypeName = "decimal(18, 8)")]
        public decimal TargetPrice { get; set; }
        public PriceAlertType AlertType { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasBeenNotifiedForCurrentState { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggeredAt { get; set; }
    }
}
