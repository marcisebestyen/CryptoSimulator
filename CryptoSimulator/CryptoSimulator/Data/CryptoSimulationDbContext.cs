using CryptoSimulator.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Data
{
    public class CryptoSimulationDbContext : DbContext
    {
        public CryptoSimulationDbContext(DbContextOptions<CryptoSimulationDbContext> options) : base(options) { }

        public DbSet<Crypto> Cryptos { get; set; }
        public DbSet<CryptoLog> CryptoLogs { get; set; }
        public DbSet<MyCryptos> MyCryptos { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<PriceAlert> PriceAlerts { get; set; }
        public DbSet<SavedCard> SavedCards { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<OfferFill> OfferFills { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Crypto>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<CryptoLog>()
                .HasKey(cl => new { cl.CryptoId, cl.From });

            modelBuilder.Entity<MyCryptos>()
                .HasKey(mc => new { mc.WalletId, mc.CryptoId });

            modelBuilder.Entity<Transactions>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<Wallet>()
                .HasKey(w => w.Id);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId);

            modelBuilder.Entity<Transactions>()
                .HasOne(t => t.Wallet)
                .WithMany()
                .HasForeignKey(t => t.WalletId);

            modelBuilder.Entity<Transactions>()
                .HasOne(t => t.Crypto)
                .WithMany()
                .HasForeignKey(t => t.CryptoId);

            modelBuilder.Entity<MyCryptos>()
                .HasOne(mc => mc.Wallet)
                .WithMany()
                .HasForeignKey(mc => mc.WalletId);

            modelBuilder.Entity<MyCryptos>()
                .HasOne(mc => mc.Crypto)
                .WithMany()
                .HasForeignKey(mc => mc.CryptoId);

            modelBuilder.Entity<CryptoLog>()
                .HasOne(cl => cl.Crypto)
                .WithMany()
                .HasForeignKey(cl => cl.CryptoId);

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.RecoveryCodeHash)
                .IsUnique();

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CryptoLog>()
                .Property(cl => cl.CurrentValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MyCryptos>()
                .Property(mc => mc.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Transactions>()
                .Property(t => t.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Transactions>()
                .Property(t => t.ExchangeRate)
                .HasPrecision(18, 8);

            modelBuilder.Entity<PriceAlert>()
                .HasKey(pa => pa.Id);

            modelBuilder.Entity<PriceAlert>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PriceAlert>()
                .HasOne(pa => pa.Crypto)
                .WithMany()
                .HasForeignKey(pa => pa.CryptoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedCard>()
                .HasKey(sc => sc.Id);

            modelBuilder.Entity<SavedCard>()
                .Property(sc => sc.HolderName)
                .HasMaxLength(100);

            modelBuilder.Entity<SavedCard>()
                .Property(sc => sc.Last4)
                .HasMaxLength(4)
                .IsFixedLength();

            modelBuilder.Entity<SavedCard>()
                .Property(sc => sc.Network)
                .HasConversion<string>();

            modelBuilder.Entity<SavedCard>()
                .HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Deposit>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Deposit>()
                .Property(d => d.Last4)
                .HasMaxLength(4)
                .IsFixedLength();

            modelBuilder.Entity<Deposit>()
                .Property(d => d.Network)
                .HasConversion<string>();

            modelBuilder.Entity<Deposit>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Deposit>()
                .HasOne(d => d.Wallet)
                .WithMany()
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Deposit>()
                .HasOne(d => d.SavedCard)
                .WithMany()
                .HasForeignKey(d => d.SavedCardId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Offer>()
                .HasKey(o => o.Id);

            modelBuilder.Entity<Offer>()
                .Property(o => o.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Offer>()
                .Property(o => o.RemainingAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Offer>()
                .Property(o => o.AskingPrice)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Offer>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Offer>()
                .HasIndex(o => new { o.Status, o.CryptoId });

            modelBuilder.Entity<Offer>()
                .HasOne(o => o.SellerWallet)
                .WithMany()
                .HasForeignKey(o => o.SellerWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Offer>()
                .HasOne(o => o.Crypto)
                .WithMany()
                .HasForeignKey(o => o.CryptoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferFill>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<OfferFill>()
                .Property(f => f.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OfferFill>()
                .Property(f => f.Price)
                .HasPrecision(18, 8);

            modelBuilder.Entity<OfferFill>()
                .HasOne(f => f.Offer)
                .WithMany()
                .HasForeignKey(f => f.OfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferFill>()
                .HasOne(f => f.BuyerWallet)
                .WithMany()
                .HasForeignKey(f => f.BuyerWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
