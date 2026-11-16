using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MiApi.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private  readonly AppDbContext _context;

        private readonly DbSet<T> _dbSet;
        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            var query = _dbSet.AsQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }
        public async Task<T?> GetByIdAsync(int id) =>
            await _dbSet.FindAsync(id);

        public async Task AddAsync(T entity) =>
            await _dbSet.AddAsync(entity);

        public void Update(T entity) =>
            _dbSet.Update(entity);

        public void  Delete(T entity) =>
            _dbSet.Remove(entity);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> filter) => 
                await _dbSet.FirstOrDefaultAsync(filter);
    }
}