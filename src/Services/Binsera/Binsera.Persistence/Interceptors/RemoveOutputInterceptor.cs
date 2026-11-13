using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Binsera.Persistence.Interceptors
{
    public class RemoveOutputInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            RemoveOutputIfExists(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            RemoveOutputIfExists(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            RemoveOutputIfExists(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        private void RemoveOutputIfExists(DbCommand command)
        {
            if (command.CommandText.Contains("OUTPUT INSERTED", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText = RemoveOutputClause(command.CommandText);
            }
        }

        private string RemoveOutputClause(string sql)
        {
            int start = sql.IndexOf("OUTPUT", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return sql;

            int end = sql.IndexOf("VALUES", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) return sql;

            return sql.Remove(start, end - start);
        }
    }


}
