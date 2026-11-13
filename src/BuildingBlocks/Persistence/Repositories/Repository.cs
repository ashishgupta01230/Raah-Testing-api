using EFCore.BulkExtensions;
using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace HautSphere.BuildingBlocks.Persistence.Repositories
{
    public class Repository<T> : BaseRepository, IRepository<T> where T : DomainBase
    {
        public DbSet<T> DbSet { get { return DbContext.Set<T>(); } }

        public Repository(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public IQueryable<T> List => DbSet.AsQueryable();

        public IQueryable<T> Table
        {
            get
            {
                return DbSet;
            }
        }
        public IQueryable<T> TableNoTracking
        {
            get
            {
                return DbSet.AsQueryable().AsNoTracking();
            }
        }
        public async Task<IEnumerable<T>> AddRangeAsyn(IList<T> entities)
        {
            await DbContext.BulkInsertAsync(entities);
            return entities;
        }
        public async Task<IEnumerable<T>> BulkInsertOrUpdateAsync(IList<T> entities)
        {
            await DbContext.BulkInsertOrUpdateAsync<T>(entities);
            return entities;
        }

        public async Task BulkDeleteAsync(IList<T> entities)
        {
            await DbContext.BulkDeleteAsync<T>(entities);
        }

        public T Get(long id)
        {
            return DbSet.Find(id);
        }

        public Task<T> GetAsync(Expression<Func<T, bool>> expression)
        {
            return DbSet.SingleOrDefaultAsync(expression);
        }
        public Task<T> GetFristOrDefault(Expression<Func<T, bool>> expression)
        {
            return DbSet.FirstOrDefaultAsync(expression);
        }


        public async Task Save(T domain)
        {

            if (domain.Id <= 0)
            {
                await DbContext.AddAsync(domain);
                //DbContext.Entry(domain).State = EntityState.Added;
                //DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            }
            else
            {
                Update(domain);
            }

            await DbContext.SaveChangesAsync();
        }

        public async Task SaveAll(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Id <= 0)
                {
                    await AddAsync(entity);
                }
                else
                {
                    Update(entity);
                }
            }

            await DbContext.SaveChangesAsync();
        }
        public void InsertAllNoCascade(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                DbContext.Entry(entity).State = EntityState.Added;
            }
            DbContext.SaveChanges();
        }
        public void UpdateAllNoCascade(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                var original = DbSet.Find(entity.Id);
                DbContext.Entry(original).State = EntityState.Modified;
            }
            DbContext.SaveChanges();
        }
        public async Task AddAsync(T entity)
        {
            await DbContext.AddAsync(entity);
        }

        public void Update(T entity)
        {
            var original = DbSet.Find(entity.Id);

            DbContext.Entry(original).CurrentValues.SetValues(entity);
            DbContext.Entry(original).State = EntityState.Modified;
        }

        public async Task Delete(T domain)
        {
            if (DbContext.Entry(domain).State == EntityState.Detached)
            {
                DbSet.Attach(domain);
            }

            DbContext.Remove(domain);
            await DbContext.SaveChangesAsync();
        }

        public async Task Delete(Expression<Func<T, bool>> expression)
        {
            var entities = Table.Where(expression);
            await DeleteAll(entities);
        }

        public virtual async Task<int> DeleteRangeAsyn(IEnumerable<T> entitys)
        {
            DbContext.Set<T>().RemoveRange(entitys);
            return await DbContext.SaveChangesAsync();
        }
        public async Task DeleteAll(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                if (DbContext.Entry(entity).State == EntityState.Detached)
                {
                    DbSet.Attach(entity);
                }
                DbSet.Remove(entity);
            }
            await DbContext.SaveChangesAsync();
        }

        public IQueryable<T> IncludeMultiple(params Expression<Func<T, object>>[] includes)
        {
            if (includes != null)
            {
                return includes.Aggregate(Table,
                          (current, include) => current.Include(include));
            }
            return Table;
        }

        public async Task<int> ExecuteStoreProcedure(string procName, params string[] parms)
        {
            var sqlRaw = string.Format("EXEC {0} {1} ", procName, string.Join(',', parms));
            return await DbContext.Database.ExecuteSqlRawAsync(sqlRaw);
        }
        public async Task<int> ExecuteStoreProcedure(string procName)
        {
            var sqlRaw = string.Format("EXEC {0}", procName);
            return await DbContext.Database.ExecuteSqlRawAsync(sqlRaw);
        }
        public async Task<bool> IsAlreadyExists(Expression<Func<T, bool>> expression)
        {
            return await TableNoTracking.AnyAsync(expression);
        }
        public async Task<TResult[]> FetchAsync<TResult>(
                    Expression<Func<T, bool>> expression,
                    Expression<Func<T, TResult>> selector)
              => await TableNoTracking.Where(expression).Select(selector).ToArrayAsync();
        public async Task<TResult[]> FetchAsync<TResult>(Expression<Func<T, TResult>> selector)
            => await TableNoTracking.Select(selector).ToArrayAsync();

        public async Task<TResult> GetFirstOrDefaultAsync<TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector)
        {

            return await TableNoTracking.Where(expression)
                            .Select(selector).FirstOrDefaultAsync();

        }
        public async Task<IPagedList<TResult>> GetPagedListAsync<TResult>(
             Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            string orderBy = null,
            int pageIndex = 0,
            int pageSize = 20) where TResult : class
        {
            IQueryable<T> query = TableNoTracking;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
                return await query.OrderBy(orderBy)
                            .Select(selector)
                            .ToPagedListAsync(pageIndex, pageSize);

            return await query.Select(selector).ToPagedListAsync(pageIndex, pageSize);
        }

    }
}
