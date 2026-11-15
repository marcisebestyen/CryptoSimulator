using CryptoSimulator.Data;
using CryptoSimulator.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CryptoSimulationDbContext _context;

        public UnitOfWork(CryptoSimulationDbContext context)
        {
            _context = context;
            CryptoRepository = new Repository<Crypto>(_context);
            CryptoLogRepository = new Repository<CryptoLog>(_context);
            MyCryptosRepository = new Repository<MyCryptos>(_context);
            TransactionRepository = new Repository<Transactions>(_context);
            UserRepository = new Repository<User>(_context);
            WalletRepository = new Repository<Wallet>(_context);
            PriceAlertRepository = new Repository<PriceAlert>(_context);
            SavedCardRepository = new Repository<SavedCard>(_context);
            DepositRepository = new Repository<Deposit>(_context);
            OfferRepository = new Repository<Offer>(_context);
            OfferFillRepository = new Repository<OfferFill>(_context);
        }

        public IRepository<Crypto> CryptoRepository { get; set; }
        public IRepository<CryptoLog> CryptoLogRepository { get; set; }
        public IRepository<MyCryptos> MyCryptosRepository { get; set; }
        public IRepository<Transactions> TransactionRepository { get; set; }
        public IRepository<User> UserRepository { get; set; }
        public IRepository<Wallet> WalletRepository { get; set; }
        public IRepository<PriceAlert> PriceAlertRepository { get; set; }
        public IRepository<SavedCard> SavedCardRepository { get; set; }
        public IRepository<Deposit> DepositRepository { get; set; }
        public IRepository<Offer> OfferRepository { get; set; }
        public IRepository<OfferFill> OfferFillRepository { get; set; }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default)
        {
            return await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            var transaction = _context.Database.CurrentTransaction
                ?? throw new InvalidOperationException("No transaction is active.");
            await transaction.CommitAsync(ct);
        }

        public async Task LockOfferAndWalletsAsync(int offerId, int walletIdA, int walletIdB, CancellationToken ct = default)
        {
            if (_context.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException("Row locks need an active transaction.");
            }

            await _context.Database
                .SqlQuery<int>($"SELECT \"Id\" AS \"Value\" FROM \"Offers\" WHERE \"Id\" = {offerId} FOR UPDATE")
                .ToListAsync(ct);

            foreach (var walletId in new[] { walletIdA, walletIdB }.Distinct().OrderBy(id => id))
            {
                await _context.Database
                    .SqlQuery<int>($"SELECT \"Id\" AS \"Value\" FROM \"Wallets\" WHERE \"Id\" = {walletId} FOR UPDATE")
                    .ToListAsync(ct);
            }

            DetachStale(offerId, walletIdA, walletIdB);
        }

        // Rows read before the lock may be outdated, so drop them from the tracker to force a fresh read.
        private void DetachStale(int offerId, int walletIdA, int walletIdB)
        {
            var stale = _context.ChangeTracker.Entries()
                .Where(e => e.Entity switch
                {
                    Offer offer => offer.Id == offerId,
                    Wallet wallet => wallet.Id == walletIdA || wallet.Id == walletIdB,
                    MyCryptos holding => holding.WalletId == walletIdA || holding.WalletId == walletIdB,
                    _ => false
                })
                .ToList();

            foreach (var entry in stale)
            {
                if (entry.State != EntityState.Unchanged)
                {
                    throw new InvalidOperationException("Offer, wallet or holding has pending changes; lock before modifying them.");
                }

                entry.State = EntityState.Detached;
            }
        }
    }
}
