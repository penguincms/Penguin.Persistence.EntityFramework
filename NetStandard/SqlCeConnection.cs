using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.SqlServerCe
{
    /// <summary>
    /// A Dummy class used as a placeholder for when the framework doesn't have access to SQLCE. Dont use this
    /// </summary>
    public class SqlCeConnection : DbConnection
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public override string ConnectionString { get => Connection.ConnectionString; set => Connection.ConnectionString = value; }

        public override string Database => Connection.Database;

        public override string DataSource => Connection.DataSource;

        public override string ServerVersion => Connection.ServerVersion;

        public override ConnectionState State => Connection.State;

        public SqlCeConnection(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
        }

        public SqlCeConnection(string connectionString, SqlCredential credential)
        {
            Connection = new SqlConnection(connectionString, credential);
        }

        public override void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);

        public override void Close() => Connection.Close();

        public override void Open() => Connection.Open();

        protected SqlConnection Connection { get; set; }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => Connection.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand() => Connection.CreateCommand();

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}