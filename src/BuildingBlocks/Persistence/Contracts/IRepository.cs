using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HautSphere.BuildingBlocks.Persistence.Contracts
{
    public interface IRepository<T> where T : DomainBase
    {
        public DbSet<T> DbSet { get; }
        T Get(long id);
        Task<IEnumerable<T>> AddRangeAsyn(IList<T> entities);
        Task BulkDeleteAsync(IList<T> entities);
        Task<int> DeleteRangeAsyn(IEnumerable<T> entitys);
        Task<T> GetAsync(Expression<Func<T, bool>> expression);
        Task Save(T domain);
        Task Delete(T domain);
        Task Delete(Expression<Func<T, bool>> expression);
        Task<TResult[]> FetchAsync<TResult>(
                                        Expression<Func<T, bool>> expression,
                                        Expression<Func<T, TResult>> selector);
        Task<TResult[]> FetchAsync<TResult>(Expression<Func<T, TResult>> selector);
        IQueryable<T> Table { get; }
        IQueryable<T> TableNoTracking { get; }
        Task DeleteAll(IEnumerable<T> entities);
        Task SaveAll(IEnumerable<T> entities);
        IQueryable<T> IncludeMultiple(params Expression<Func<T, object>>[] includes);
        //for Speed Improvement of Bulk Update
        void UpdateAllNoCascade(IEnumerable<T> entities);
        void InsertAllNoCascade(IEnumerable<T> entities);
        Task<int> ExecuteStoreProcedure(string procName, params string[] parms);
        Task<int> ExecuteStoreProcedure(string procName);
        Task<IEnumerable<T>> BulkInsertOrUpdateAsync(IList<T> entities);
        Task<T> GetFristOrDefault(Expression<Func<T, bool>> expression);
        Task<TResult> GetFirstOrDefaultAsync<TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector);
        Task<bool> IsAlreadyExists(Expression<Func<T, bool>> expression);
        Task<IPagedList<TResult>> GetPagedListAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            string orderBy = null,                                                 
            int pageIndex = 1,                                      
            int pageSize = 20) where TResult : class;
        //Task AddAsync(global::Binsera.Core.Master.Domain.CountryView country);
    }
}
