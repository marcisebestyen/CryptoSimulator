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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Crypto>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<CryptoLog>()
                .HasKey(cl => cl.CryptoId);

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

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CryptoLog>()
                .Property(cl => cl.CurrentValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MyCryptos>()
                .Property(mc => mc.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transactions>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transactions>()
                .Property(t => t.ExchangeRate)
                .HasPrecision(18, 8); 

            base.OnModelCreating(modelBuilder);
        }
    }
}
