using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Core.Interfaces;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Infrastructure.Repositories;

public class EfRepository<T>(PharmacyDbContext db) : IRepository<T> where T : class
{
    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Set<T>().FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => await db.Set<T>().AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await db.Set<T>().Where(predicate).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await db.Set<T>().AddAsync(entity, ct);

    public void Update(T entity) => db.Set<T>().Update(entity);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
}

public class UnitOfWork(PharmacyDbContext db) : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class => new EfRepository<T>(db);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public void Dispose() => db.Dispose();
}
