using Microsoft.EntityFrameworkCore;
using Transactions.Core.Models;
using Transactions.Core.Repository;
using Transactions.Infrastructure.DataBase;

namespace Transactions.Infrastructure.Repository;

public class BaseRepository<T> : IModelBaseRepository<T> where T : ModelBase
{
    protected readonly TransactionsContext _db;

    public BaseRepository(TransactionsContext db)
    {
        _db = db;
    }

    public Task<List<T>> GetAllAsync()
    {
        return _db.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task CreateAsync(T entity)
    {
        await _db.Set<T>().AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _db.Set<T>().Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _db.Set<T>().Remove(entity);
        await _db.SaveChangesAsync();
    }

    public Task<T?> GetAsync(Guid id)
    {
        return _db.Set<T>().FirstOrDefaultAsync(x => x.Id == id);
    }

    public IQueryable<T> List()
    {
        return _db.Set<T>().AsNoTracking();
    }
}
