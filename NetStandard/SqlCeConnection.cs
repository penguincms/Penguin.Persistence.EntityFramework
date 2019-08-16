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
        #region Properties
        public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

        public override string Database => _connection.Database;

        public override string DataSource => _connection.DataSource;

        public override string ServerVersion => _connection.ServerVersion;

        public override ConnectionState State => _connection.State;

        #endregion Properties

        #region Constructors

        public SqlCeConnection(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public SqlCeConnection(string connectionString, SqlCredential credential)
        {
            _connection = new SqlConnection(connectionString, credential);
        }

        #endregion Constructors

        #region Methods

        public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        public override void Close() => _connection.Close();

        public override void Open() => _connection.Open();

        #endregion Methods

        protected SqlConnection _connection { get; set; }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand() => _connection.CreateCommand();

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}