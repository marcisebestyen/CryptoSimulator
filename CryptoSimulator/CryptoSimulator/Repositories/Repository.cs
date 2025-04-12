using CryptoSimulator.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CryptoSimulator.Repositories
{
    public interface IRepository<T> where T : class
    {
        void Insert(T entity);
        void Delete(params object[] keyValues);
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate, string[]? includeProperties = null);
        T? GetById(object[] keyValues, string[]? includeProperties = null, string[]? includeCollections = null);
        void Update(T entity);
        Task InsertAsync(T entity);
        Task DeleteAsync(params object[] keyValues);
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, string[]? includeProperties = null);
        Task<T?> GetByIdAsync(object[] keyValues, string[]? includeProperties = null, string[]? includeCollections = null);
        Task<IEnumerable<T>> GetAllAsync(string[]? includeProperties = null);
        Task UpdateAsync(T entity);
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly CryptoSimulationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(CryptoSimulationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        public void Insert(T entity)
        {
            _dbSet.Add(entity);
        }

        public async Task InsertAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Delete(params object[] keyValues)
        {
            T? entity = _dbSet.Find(keyValues);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
            else
            {
                throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with the provided key values was not found.");
            }
        }

        public async Task DeleteAsync(params object[] keyValues)
        {
            T? entity = await _dbSet.FindAsync(keyValues);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
            else
            {
                throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with the provided key values was not found.");
            }
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate, string[]? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            query = query.Where(predicate);
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }
            return query.ToList();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, string[]? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            query = query.Where(predicate);
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }
            return await query.ToListAsync();
        }

        public T? GetById(object[] keyValues, string[]? includeReferences = null, string[]? includeCollections = null)
        {
            T? entity = _dbSet.Find(keyValues);
            if (entity == null)
            {
                return null;
            }

            if (includeReferences != null)
            {
                foreach (var reference in includeReferences)
                {
                    _context
                        .Entry(entity)
                        .Reference(reference)
                        .Load();
                }
            }

            if (includeCollections != null)
            {
                foreach (var collection in includeCollections)
                {
                    _context
                        .Entry(entity)
                        .Collection(collection)
                        .Load();
                }
            }

            return entity;
        }

        public async Task<T?> GetByIdAsync(object[] keyValues, string[]? includeReferences = null, string[]? includeCollections = null)
        {
            T? entity = await _dbSet.FindAsync(keyValues);
            if (entity == null)
            {
                return null;
            }

            List<Task> tasks = new List<Task>();

            if (includeReferences != null)
            {
                foreach (var reference in includeReferences)
                {
                    tasks.Add(
                        _context
                            .Entry(entity)
                            .Reference(reference)
                            .LoadAsync()
                    );

                }
            }

            if (includeCollections != null)
            {
                foreach (var collection in includeCollections)
                {
                    tasks.Add(
                        _context
                            .Entry(entity)
                            .Collection(collection)
                            .LoadAsync()
                    );
                }
            }

            await Task.WhenAll(tasks);

            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync(string[]? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }
            return await query.ToListAsync();
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Run(() => _dbSet.Update(entity));
            await Task.CompletedTask;
        }
    }
}
