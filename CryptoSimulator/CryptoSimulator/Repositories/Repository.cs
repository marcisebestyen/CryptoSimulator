﻿using CryptoSimulator.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Repositories
{
    public interface IRepository<T> where T : class
    {
        void Insert(T entity);
        Task InsertAsync(T entity);
        void Delete(int id);
        Task DeleteAsync(int id);
        IEnumerable<T> Get(string[]? includeProperties = null);
        Task<IEnumerable<T>> GetAsync(string[]? includeProperties = null);
        T? GetById(int id, string[]? includeReferences = null, string[]? includeCollections = null);
        Task<T?> GetByIdAsync(int id, string[]? includeReferences = null, string[]? includeCollections = null);
        void Update(T entity);
        Task UpdateAsync(T entity);
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly CryptoSimulationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(CryptoSimulationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public void Insert(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public async Task InsertAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public void Delete(int id)
        {
            T? entity = _dbSet.Find(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
            else
            {
                throw new ArgumentNullException(nameof(entity), "Entity not found");
            }
            _context.SaveChanges();
        }

        public async Task DeleteAsync(int id)
        {
            T? entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
            else
            {
                throw new ArgumentNullException(nameof(entity), "Entity not found");
            }
            await _context.SaveChangesAsync();
        }

        public IEnumerable<T> Get(string[]? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }
            return query.ToList();
        }

        public async Task<IEnumerable<T>> GetAsync(string[]? includeProperties = null)
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

        public T? GetById(int id, string[]? includeReferences = null, string[]? includeCollections = null)
        {
            T? entity = _dbSet.Find(id);
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

        public async Task<T?> GetByIdAsync(int id, string[]? includeReferences = null, string[]? includeCollections = null)
        {
            T? entity = await _dbSet.FindAsync(id);
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

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Run(() => _dbSet.Update(entity));
            await _context.SaveChangesAsync();
        }
    }
}
