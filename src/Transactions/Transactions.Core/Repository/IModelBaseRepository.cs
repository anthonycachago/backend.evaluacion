using Transactions.Core.Models;

namespace Transactions.Core.Repository;

public interface IModelBaseRepository<T> where T : ModelBase
{
    Task CreateAsync(T model);
    Task<List<T>> GetAllAsync();
    Task UpdateAsync(T model);
    Task DeleteAsync(T model);
    Task<T?> GetAsync(Guid id);
    IQueryable<T> List();
}
