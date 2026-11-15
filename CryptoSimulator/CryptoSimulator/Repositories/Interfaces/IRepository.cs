using System.Linq.Expressions;

namespace CryptoSimulator.Repositories
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate, string[]? includeProperties = null);
        void Update(T entity);
        Task InsertAsync(T entity);
        Task DeleteAsync(params object[] keyValues);
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, string[]? includeProperties = null);
        Task<T?> GetByIdAsync(object[] keyValues, string[]? includeProperties = null, string[]? includeCollections = null);
        Task<IEnumerable<T>> GetAllAsync(string[]? includeProperties = null);
        Task UpdateAsync(T entity);
    }
}
