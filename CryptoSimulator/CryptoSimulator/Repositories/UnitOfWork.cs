using CryptoSimulator.Data;
using CryptoSimulator.Entities;

namespace CryptoSimulator.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Crypto> CryptoRepository { get; }
        IRepository<CryptoLog> CryptoLogRepository { get; }
        IRepository<MyCryptos> MyCryptosRepository { get; }
        IRepository<Transactions> TransactionRepository { get; }
        IRepository<User> UserRepository { get; }
        IRepository<Wallet> WalletRepository { get; }

        void Save();
        Task SaveAsync();
    }

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
        }

        public IRepository<Crypto> CryptoRepository { get; set; }
        public IRepository<CryptoLog> CryptoLogRepository { get; set; }
        public IRepository<MyCryptos> MyCryptosRepository { get; set; }
        public IRepository<Transactions> TransactionRepository { get; set; }
        public IRepository<User> UserRepository { get; set; }
        public IRepository<Wallet> WalletRepository { get; set; }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
