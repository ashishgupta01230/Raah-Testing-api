using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.QueryBuilder.Models;
using System.Data;

namespace HautSphere.BuildingBlocks.QueryBuilder.Query
{
    public interface IDapperQuery
    {
        IDbConnection OpenConnection();
        IDbConnection Connection { get; }
        Task<TReturn> QueryFirstOrDefaultAsync<TReturn>(string sql, object param = null, CommandType? commandType = null);
        List<TReturn> Query<TReturn>(string sql, object? param = null, CommandType? commandType = null);
        Task<IEnumerable<TReturn>> QueryAsync<TReturn>(string sql, object param = null, CommandType? commandType = null);
        Task<PagedList<TReturn>> QueryPageAsync<TReturn>(string countSql, string dataSql, int pageindex, int pageSize, object param = null);
        Task<PagedList<TReturn>> FunctionQueryPageAsync<Table, TReturn>(int pageindex, int pageSize, string queryBuilder, object filterParameters, params OrderByInfo[] orderByInfo);
        Task<PagedList<TReturn>> QueryNoCountAsync<TReturn>(string dataSql, int pageindex, int pageSize, object param = null);
        void DisposeConnection();
    }
}
