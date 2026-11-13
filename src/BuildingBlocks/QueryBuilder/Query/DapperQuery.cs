using Dapper;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.QueryBuilder.Extension;
using HautSphere.BuildingBlocks.QueryBuilder.Models;
using System.Data;
using System.Data.Common;

#nullable disable
namespace HautSphere.BuildingBlocks.QueryBuilder.Query
{
    public abstract class DapperQuery<TDbConnection> : IDapperQuery where TDbConnection : DbConnection, new()
    {
        IDbConnection _connection;
        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = OpenConnection();
                }
                return _connection;
            }
        }

        private readonly string pagingQuery = "limit @Take OFFSET @Skip;";
        private string ConnectionString { get; set; }
        protected DapperQuery(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public IDbConnection OpenConnection()
        {
            var conn = new TDbConnection();
            conn.ConnectionString = ConnectionString;
            //if (conn != null && conn.State != ConnectionState.Open)
            //{
            //    conn.Open();
            //}
            return conn;
        }
        public List<TReturn> Query<TReturn>(string sql, object param = null, CommandType? commandType = null)
        {
            var list = Connection.Query<TReturn>(sql, param, commandType: commandType);
            return list.ToList();
        }

        public async Task<IEnumerable<TReturn>> QueryAsync<TReturn>(string sql, object param = null, CommandType? commandType = null)
        {
            var list = await Connection.QueryAsync<TReturn>(sql, param, commandType: commandType);
            return list.ToList();
        }

        public async Task<TReturn> QueryFirstOrDefaultAsync<TReturn>(string sql, object param = null, CommandType? commandType = null)
        {
            return await Connection.QueryFirstOrDefaultAsync<TReturn>(sql, param, commandType: commandType);
        }

        public async Task<PagedList<TReturn>> QueryPageAsync<TReturn>(string countSql, string dataSql,
            int pageindex, int pageSize, object param = null)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();

            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });

            var sql = $"{countSql}{(countSql.EndsWith(";") ? "" : ";")}{dataSql.Replace(";", " ")} {pagingQuery}";
            using var multi = await Connection.QueryMultipleAsync(sql, pars);
            var count = (await multi.ReadAsync<int>()).FirstOrDefault();
            var data = (await multi.ReadAsync<TReturn>()).ToList();
            DisposeConnection();
            var result = new PagedList<TReturn>
            {
                TotalCount = count,
                PageIndex = pageindex,
                PageSize = pageSize,
                Items = data
            };
            result.TotalPages = result.TotalCount % pageSize == 0
                  ? result.TotalCount / pageSize
                  : result.TotalCount / pageSize + 1;
            if (result.PageSize > result.TotalPages)
                result.PageSize = pageSize;
            return result;
        }

        public virtual void DisposeConnection()
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        public async Task<PagedList<TReturn>> FunctionQueryPageAsync<Table, TReturn>(int pageindex, int pageSize,
            string queryBuilder, object filterParameters, params OrderByInfo[] orderByInfo)
        {
            var countQuery = $"{QueryExtension.CountQuery<Table>()}({queryBuilder})";
            var selectQuery = $"{QueryExtension.SelectQuery<Table, TReturn>()}({queryBuilder})";
            if (orderByInfo != null && orderByInfo.Length > 0)
            {
                var orderByColumn = QueryExtension.OrderByQuery<Table>(orderByInfo);
                selectQuery += $" {orderByColumn}";
            }
            return await QueryPageAsync<TReturn>(countQuery, selectQuery, pageindex, pageSize, filterParameters);
        }

        public async Task<PagedList<TReturn>> QueryNoCountAsync<TReturn>(string dataSql, int pageindex, int pageSize, object param = null)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();

            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });

            var sql = $"{dataSql.Replace(";", " ")} {pagingQuery}";
            var data = await Connection.QueryAsync<TReturn>(sql, pars);

            var result = new PagedList<TReturn>
            {
                PageIndex = pageindex,
                PageSize = pageSize,
                Items = data.ToList()
            };
            result.TotalPages = result.TotalCount % pageSize == 0
                  ? result.TotalCount / pageSize
                  : result.TotalCount / pageSize + 1;
            if (result.PageSize > result.TotalPages)
                result.PageSize = result.TotalPages;
            return result;
        }
    }
}
