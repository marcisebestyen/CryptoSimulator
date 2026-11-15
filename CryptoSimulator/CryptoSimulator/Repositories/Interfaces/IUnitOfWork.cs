using CryptoSimulator.Entities;

namespace CryptoSimulator.Repositories
{
    public interface IUnitOfWork
    {
        IRepository<Crypto> CryptoRepository { get; }
        IRepository<CryptoLog> CryptoLogRepository { get; }
        IRepository<MyCryptos> MyCryptosRepository { get; }
        IRepository<Transactions> TransactionRepository { get; }
        IRepository<User> UserRepository { get; }
        IRepository<Wallet> WalletRepository { get; }
        IRepository<PriceAlert> PriceAlertRepository { get; }
        IRepository<SavedCard> SavedCardRepository { get; }
        IRepository<Deposit> DepositRepository { get; }

        IRepository<Offer> OfferRepository { get; }
        IRepository<OfferFill> OfferFillRepository { get; }

        Task SaveAsync();

        Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Takes row locks (FOR UPDATE) on the offer, then on both wallets in ascending id order, so
        /// concurrent buys queue up instead of deadlocking. Must run inside BeginTransactionAsync.
        /// Tracked Offer, Wallet and MyCryptos of the locked wallets are detached;
        /// load them again after this call.
        /// Unknown ids lock nothing and do not throw.
        /// </summary>
        Task LockOfferAndWalletsAsync(int offerId, int walletIdA, int walletIdB, CancellationToken ct = default);
    }
}
