using HautSphere.BuildingBlocks.QueryBuilder.Query;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace HautSphere.BuildingBlocks.QueryBuilder.Providers
{

    public class PostgressDapper : DapperQuery<NpgsqlConnection>
    {
        public PostgressDapper(string connectionString) : base(connectionString)
        {
        }
    }

    public class MsSqlDapper : DapperQuery<SqlConnection>
    {
        public MsSqlDapper(string connectionString) : base(connectionString)
        {
        }
    }
}
