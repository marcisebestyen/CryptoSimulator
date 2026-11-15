namespace CryptoSimulator.Repositories
{
    public static class UnitOfWorkExtensions
    {
        private const int NoOfferId = 0;

        // Locks one wallet row (offer 0 does not exist). Run inside a transaction, then reload the wallet and holdings.
        public static Task LockWalletAsync(this IUnitOfWork unitOfWork, int walletId, CancellationToken ct = default)
        {
            return unitOfWork.LockOfferAndWalletsAsync(NoOfferId, walletId, walletId, ct);
        }
    }
}
