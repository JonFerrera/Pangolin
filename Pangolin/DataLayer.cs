using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pangolin
{
    public static class DataLayer
    {
        #region GUID
        public static string GetGUID()
        {
            return new Guid().ToString();
        }

        public static byte[] GetGuidBytes()
        {
            return new Guid().ToByteArray();
        }
        #endregion

        #region Parse Connection Strings
        private static async Task<bool> IsOdbcConnectionStringAsync(string connectionString)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }
            else if (string.Equals(connectionString, string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be an empty string.",nameof(connectionString)); }
            else if (string.Equals(connectionString.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be a white-space string.", nameof(connectionString)); }

            bool isOdbcConnectionString = false;

            try
            {
                new OdbcConnectionStringBuilder(connectionString);
                isOdbcConnectionString = true;
            }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

            return isOdbcConnectionString;
        }

        private static async Task<bool> IsOleDbConnectionStringAsync(string connectionString)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }
            else if (string.Equals(connectionString, string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be an empty string.", nameof(connectionString)); }
            else if (string.Equals(connectionString.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be a white-space string.", nameof(connectionString)); }

            bool isOleDbConnectionString = false;

            try
            {
                new OleDbConnectionStringBuilder(connectionString);
                isOleDbConnectionString = true;
            }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

            return isOleDbConnectionString;
        }

        private static async Task<bool> IsSqlConnectionStringAsync(string connectionString)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }
            else if (string.Equals(connectionString, string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be an empty string.", nameof(connectionString)); }
            else if (string.Equals(connectionString.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(connectionString)} cannot be a white-space string.", nameof(connectionString)); }

            bool isSqlConnectionString = false;

            try
            {
                new SqlConnectionStringBuilder(connectionString);
                isSqlConnectionString = true;
            }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            catch (FormatException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            catch (KeyNotFoundException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

            return isSqlConnectionString;
        }
        #endregion

        #region Parse Query
        private static async Task<CommandType> DetermineCommandTypeAsync(string commandText)
        {
            if (commandText == null) { throw new ArgumentNullException(nameof(commandText)); }

            CommandType commandType = CommandType.StoredProcedure;

            try
            {
                string[] commandTextParts = commandText.Split(ConfigurationLayer.WhitespaceSplit, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    if (commandTextParts?.Length > 0)
                    {
                        commandType = commandTextParts.Length == 1 ? CommandType.StoredProcedure : CommandType.Text;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(commandText)} cannot be parsed.");
                    }
                }
                catch (OverflowException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

            return commandType;
        }
        #endregion

        #region Database Access
        #region Odbc - Read
        public static async Task<DataRow> OdbcReadDataRowAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await OdbcReadDataTableAsync(connectionString, query, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }

        public static async Task<DataRow> OdbcReadDataRowAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await OdbcReadDataTableAsync(connectionString, query, odbcParameters, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }

        public static async Task<DataTable> OdbcReadDataTableAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            using (DataTable dataTable = new DataTable())
            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            using (DbDataReader dbDataReader = await odbcCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(dbDataReader);
                            }
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return dataTable;
            }
        }

        public static async Task<DataTable> OdbcReadDataTableAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            using (DataTable dataTable = new DataTable())
            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        odbcCommand.Parameters.AddRange(odbcParameters);

                        try
                        {
                            using (DbDataReader dbDataReader = await odbcCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(dbDataReader);
                            }
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, odbcParameters); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                return dataTable;
            }
        }

        public static async Task<DataSet> OdbcReadDataSetAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (DataSet dataSet = new DataSet())
            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    using (OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand))
                    {
                        try
                        {
                            odbcDataAdapter.Fill(dataSet);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }

        public static async Task<DataSet> OdbcReadDataSetAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (DataSet dataSet = new DataSet())
            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        odbcCommand.Parameters.AddRange(odbcParameters);

                        using (OdbcDataAdapter odbcDataAdapter = new OdbcDataAdapter(odbcCommand))
                        {
                            try
                            {
                                odbcDataAdapter.Fill(dataSet);
                            }
                            catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, odbcParameters); throw; }
                            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }

        public static async Task<object> OdbcReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConn = new OdbcConnection(connectionString))
            {
                await odbcConn.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTrans = odbcConn.BeginTransaction())
                    using (OdbcCommand odbcCmd = new OdbcCommand(query, odbcConn, odbcTrans) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await odbcCmd.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            odbcTrans.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }

        public static async Task<object> OdbcReadDatumAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConn = new OdbcConnection(connectionString))
            {
                await odbcConn.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTrans = odbcConn.BeginTransaction())
                    using (OdbcCommand odbcCmd = new OdbcCommand(query, odbcConn, odbcTrans) { CommandType = commandType })
                    {
                        odbcCmd.Parameters.AddRange(odbcParameters);

                        try
                        {
                            datum = await odbcCmd.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, odbcParameters); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            odbcTrans.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }
        #endregion

        #region Odbc - Write
        public static async Task<int> OdbcWriteDataAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            rowsAffected += await odbcCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> OdbcWriteDataAsync(string connectionString, string[] queries, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (queries == null) { throw new ArgumentNullException(nameof(queries)); }
            else if (queries.Length < 1) { throw new ArgumentException($"{nameof(queries)} contains no queries.", nameof(queries)); }

            int rowsAffected = 0;

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    {
                        foreach (string query in queries)
                        {
                            CommandType commandType = await DetermineCommandTypeAsync(query);

                            using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                            {
                                try
                                {
                                    rowsAffected += await odbcCommand.ExecuteNonQueryAsync(cancellationToken);
                                }
                                catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                                catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                            }
                        }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> OdbcWriteDataAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        odbcCommand.Parameters.AddRange(odbcParameters);

                        try
                        {
                            rowsAffected += await odbcCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> OdbcWriteDataAsync(string connectionString, string query, OdbcParameter[][] odbcParametersCollection, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParametersCollection == null) { throw new ArgumentNullException(nameof(odbcParametersCollection)); }
            else if (odbcParametersCollection.Length < 1) { throw new ArgumentException($"{nameof(odbcParametersCollection)} contains no parameter collections.", nameof(odbcParametersCollection)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    {
                        foreach (OdbcParameter[] odbcParameters in odbcParametersCollection)
                        {
                            if (odbcParameters?.Length > 0)
                            {
                                using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                                {
                                    odbcCommand.Parameters.AddRange(odbcParameters);

                                    try
                                    {
                                        rowsAffected += await odbcCommand.ExecuteNonQueryAsync(cancellationToken);
                                    }
                                    catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, odbcParameters); throw; }
                                }
                            }
                        }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<object> OdbcWriteDataReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await odbcCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }

        public static async Task<object> OdbcWriteDataReadDatumAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        odbcCommand.Parameters.AddRange(odbcParameters);

                        try
                        {
                            datum = await odbcCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }

        public static async Task<OdbcParameter[]> OdbcWriteDataReadDataAsync(string connectionString, string query, OdbcParameter[] odbcParameters, CancellationToken cancellationToken)
        {
            if (!await IsOdbcConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid Odbc connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (odbcParameters == null) { throw new ArgumentNullException(nameof(odbcParameters)); }
            else if (odbcParameters.Length < 1) { throw new ArgumentException($"{nameof(odbcParameters)} contains no parameters.", nameof(odbcParameters)); }

            OdbcParameter[] data = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                await odbcConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OdbcTransaction odbcTransaction = odbcConnection.BeginTransaction())
                    using (OdbcCommand odbcCommand = new OdbcCommand(query, odbcConnection, odbcTransaction) { CommandType = commandType })
                    {
                        odbcCommand.Parameters.AddRange(odbcParameters);

                        try
                        {
                            await odbcCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, odbcParameters); throw; }

                        try
                        {
                            odbcTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        data = odbcParameters.Where(x => x.Direction == ParameterDirection.ReturnValue || x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput).ToArray();
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return data;
        }
        #endregion

        #region OleDb - Read
        public static async Task<DataRow> OleDbReadDataRowAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await OleDbReadDataTableAsync(connectionString, query, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }

        public static async Task<DataRow> OleDbReadDataRowAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await OleDbReadDataTableAsync(connectionString, query, oleDbParameters, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }

        public static async Task<DataTable> OleDbReadDataTableAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            
            using (DataTable dataTable = new DataTable())
            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            using (DbDataReader dbDataReader = await oleDbCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(dbDataReader);
                            }
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return dataTable;
            }
        }

        public static async Task<DataTable> OleDbReadDataTableAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }
            
            using (DataTable dataTable = new DataTable())
            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            using (DbDataReader dbDataReader = await oleDbCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(dbDataReader);
                            }
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return dataTable;
            }
        }

        public static async Task<DataSet> OleDbReadDataSetAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (DataSet dataSet = new DataSet())
            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    using (OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter(oleDbCommand))
                    {
                        try
                        {
                            oleDbDataAdapter.Fill(dataSet);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }

        public static async Task<DataSet> OleDbReadDataSetAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (DataSet dataSet = new DataSet())
            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            using (OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter(oleDbCommand))
                            {
                                oleDbDataAdapter.Fill(dataSet);
                            }
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }

        public static async Task<object> OleDbReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConn = new OleDbConnection(connectionString))
            {
                await oleDbConn.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConn.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConn, oleDbTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await oleDbCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return datum;
            }
        }

        public static async Task<object> OleDbReadDatumAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConn = new OleDbConnection(connectionString))
            {
                await oleDbConn.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConn.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConn, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            datum = await oleDbCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return datum;
            }
        }
        #endregion

        #region OleDb - Write
        public static async Task<int> OleDbWriteDataAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConn = new OleDbConnection(connectionString))
            {
                await oleDbConn.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConn.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConn, oleDbTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            rowsAffected += await oleDbCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return rowsAffected;
            }
        }

        public static async Task<int> OleDbWriteDataAsync(string connectionString, string[] queries, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (queries == null) { throw new ArgumentNullException(nameof(queries)); }
            else if (queries.Length < 1) { throw new ArgumentException($"{nameof(queries)} contains no queries.", nameof(queries)); }

            int rowsAffected = 0;

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    {
                        foreach (string query in queries)
                        {
                            CommandType commandType = await DetermineCommandTypeAsync(query);

                            using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                            {
                                try
                                {
                                    rowsAffected += await oleDbCommand.ExecuteNonQueryAsync(cancellationToken);
                                }
                                catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                                catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                            }
                        }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> OleDbWriteDataAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            rowsAffected += await oleDbCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return rowsAffected;
            }
        }

        public static async Task<int> OleDbWriteDataAsync(string connectionString, string query, OleDbParameter[][] oleDbParametersCollection, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParametersCollection == null) { throw new ArgumentNullException(nameof(oleDbParametersCollection)); }
            else if (oleDbParametersCollection.Length < 1) { throw new ArgumentException($"{nameof(oleDbParametersCollection)} contains no parameter collections.", nameof(oleDbParametersCollection)); }

            int rowsAffected = 0;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    {
                        foreach (OleDbParameter[] oleDbParameters in oleDbParametersCollection)
                        {
                            if (oleDbParameters?.Length > 0)
                            {
                                using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                                {
                                    oleDbCommand.Parameters.AddRange(oleDbParameters);

                                    try
                                    {
                                        rowsAffected += await oleDbCommand.ExecuteNonQueryAsync(cancellationToken);
                                    }
                                    catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }
                                }
                            }
                        }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<object> OleDbWriteDataReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await oleDbCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return datum;
            }
        }

        public static async Task<object> OleDbWriteDataReadDatumAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }

            object datum = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            datum = await oleDbCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, oleDbParameters); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return datum;
            }
        }

        public static async Task<OleDbParameter[]> OleDbWriteDataReadDataAsync(string connectionString, string query, OleDbParameter[] oleDbParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid OleDb connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (oleDbParameters == null) { throw new ArgumentNullException(nameof(oleDbParameters)); }
            else if (oleDbParameters.Length < 1) { throw new ArgumentException($"{nameof(oleDbParameters)} contains no parameters.", nameof(oleDbParameters)); }

            OleDbParameter[] data = null;

            CommandType commandType = await DetermineCommandTypeAsync(query);

            using (OleDbConnection oleDbConnection = new OleDbConnection(connectionString))
            {
                await oleDbConnection.OpenAsync(cancellationToken);

                try
                {
                    using (OleDbTransaction oleDbTransaction = oleDbConnection.BeginTransaction())
                    using (OleDbCommand oleDbCommand = new OleDbCommand(query, oleDbConnection, oleDbTransaction) { CommandType = commandType })
                    {
                        oleDbCommand.Parameters.AddRange(oleDbParameters);

                        try
                        {
                            await oleDbCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (DbException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            oleDbTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        data = oleDbParameters.Where(x => x.Direction == ParameterDirection.ReturnValue || x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput).ToArray();
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return data;
        }
        #endregion

        #region SQL - Read
        public static async Task<DataRow> SQLReadDataRowAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await SQLReadDataTableAsync(connectionString, query, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }

        public static async Task<DataRow> SQLReadDataRowAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            using (DataTable dataTable = await SQLReadDataTableAsync(connectionString, query, sqlParameters, cancellationToken))
            {
                return dataTable?.Rows.Count >= 1 ? dataTable.Rows[0] : dataTable.NewRow();
            }
        }
        
        public static async Task<DataTable> SQLReadDataTableAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            using (DataTable dataTable = new DataTable())
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(sqlDataReader);
                            }
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return dataTable;
            }
        }
        
        public static async Task<DataTable> SQLReadDataTableAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            using (DataTable dataTable = new DataTable())
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                dataTable.TableName = commandType == CommandType.StoredProcedure ? query : string.Empty;

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken))
                            {
                                dataTable.Load(sqlDataReader);
                            }
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                return dataTable;
            }
        }

        public static async Task<DataSet> SQLReadDataSetAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            using (DataSet dataSet = new DataSet())
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                    {
                        try
                        {
                            sqlDataAdapter.Fill(dataSet);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }
        
        public static async Task<DataSet> SQLReadDataSetAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            using (DataSet dataSet = new DataSet())
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                            {
                                sqlDataAdapter.Fill(dataSet);
                            }
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                if (commandType == CommandType.StoredProcedure)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        dataSet.Tables[i].TableName = query;
                    }
                }

                return dataSet;
            }
        }
        
        public static async Task<object> SQLReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await sqlCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }
        
        public static async Task<object> SQLReadDatumAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            object datum = null;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            datum = await sqlCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }
        #endregion

        #region SQL - Write
        public static async Task<int> SQLWriteDataAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            int rowsAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                try
                {
                    await sqlConnection.OpenAsync(cancellationToken);

                    CommandType commandType = await DetermineCommandTypeAsync(query);

                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            rowsAffected += await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> SQLWriteDataAsync(string connectionString, string[] queries, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (queries == null) { throw new ArgumentNullException(nameof(queries)); }
            else if (queries.Length < 1) { throw new ArgumentException($"{nameof(queries)} contains no queries.", nameof(queries)); }

            int rowsAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    {
                        foreach (string query in queries)
                        {
                            CommandType commandType = await DetermineCommandTypeAsync(query);

                            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                            {
                                try
                                {
                                    rowsAffected += await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                                }
                                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                                catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                            }
                        }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<int> SQLWriteDataAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            int rowsAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                try
                {
                    await sqlConnection.OpenAsync(cancellationToken);

                    CommandType commandType = await DetermineCommandTypeAsync(query);

                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            rowsAffected += await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }
        
        public static async Task<int> SQLWriteDataAsync(string connectionString, string query, SqlParameter[][] sqlParametersCollection, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParametersCollection == null) { throw new ArgumentNullException(nameof(sqlParametersCollection)); }
            else if (sqlParametersCollection.Length < 1) { throw new ArgumentException($"{nameof(sqlParametersCollection)} contains no parameters.", nameof(sqlParametersCollection)); }

            int rowsAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                try
                {
                    await sqlConnection.OpenAsync(cancellationToken);

                    CommandType commandType = await DetermineCommandTypeAsync(query);

                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    {
                        foreach (SqlParameter[] sqlParameters in sqlParametersCollection)
                        {
                            if (sqlParameters?.Length > 0)
                            {
                                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                                {
                                    sqlCommand.Parameters.AddRange(sqlParameters);

                                    try
                                    {
                                        rowsAffected += await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                                    }
                                    catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }
                                    catch (FormatException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                    catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                    catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                    catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                                }
                            }
                        }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return rowsAffected;
        }

        public static async Task<object> SQLWriteDataReadDatumAsync(string connectionString, string query, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }

            object datum = null;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        try
                        {
                            datum = await sqlCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, null); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }

        public static async Task<object> SQLWriteDataReadDatumAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            object datum = null;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            datum = await sqlCommand.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return datum;
        }

        public static async Task<SqlParameter[]> SQLWriteDataReadDataAsync(string connectionString, string query, SqlParameter[] sqlParameters, CancellationToken cancellationToken)
        {
            if (!await IsOleDbConnectionStringAsync(connectionString)) { throw new ArgumentException($"{nameof(connectionString)} is not a valid SQL connection string.", nameof(connectionString)); }
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            if (sqlParameters == null) { throw new ArgumentNullException(nameof(sqlParameters)); }
            else if (sqlParameters.Length < 1) { throw new ArgumentException($"{nameof(sqlParameters)} contains no parameters.", nameof(sqlParameters)); }

            SqlParameter[] data = null;

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(cancellationToken);

                CommandType commandType = await DetermineCommandTypeAsync(query);

                try
                {
                    using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
                    using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction) { CommandType = commandType })
                    {
                        sqlCommand.Parameters.AddRange(sqlParameters);

                        try
                        {
                            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (SqlException exc) { await ExceptionLayer.HandleAsync(exc, query, sqlParameters); throw; }
                        catch (InvalidCastException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); throw; }

                        try
                        {
                            sqlTransaction.Commit();
                        }
                        catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
                        catch (Exception exc) { await ExceptionLayer.HandleAsync(exc); throw; }


                        data = sqlParameters.Where(x => x.Direction == ParameterDirection.ReturnValue || x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput).ToArray();
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); throw; }
            }

            return data;
        }
        #endregion
        #endregion

        #region Database Scripting
        public static async Task<DataTable> GetDatabaseTablesAsync(string connectionString, string tableCatalog, string tableSchema, CancellationToken cancellationToken)
        {
            string query = "select distinct isc.TABLE_NAME from information_schema.columns isc where isc.TABLE_CATALOG = @TableCatalog and isc.TABLE_SCHEMA = @TableSchema order by isc.TABLE_NAME";

            SqlParameter[] sqlParameters = new SqlParameter[]
            {
                new SqlParameter("@TableCatalog", tableCatalog) { SqlDbType = SqlDbType.NVarChar },
                new SqlParameter("@TableSchema", tableSchema) { SqlDbType = SqlDbType.NVarChar }
            };

            return await SQLReadDataTableAsync(connectionString, query, sqlParameters, cancellationToken);
        }

        public static async Task<DataTable> GetDatabaseColumnsAsync(string connectionString, string tableCatalog, string tableSchema, string tableName, CancellationToken cancellationToken)
        {
            string query = "select isc.COLUMN_NAME, isc.DATA_TYPE from information_schema.columns isc where isc.TABLE_CATALOG = @TableCatalog and isc.TABLE_SCHEMA = @TableSchema and isc.TABLE_NAME = @TableName order by isc.ORDINAL_POSITION";

            SqlParameter[] sqlParameters = new SqlParameter[]
            {
                new SqlParameter("@TableCatalog", tableCatalog) { SqlDbType = SqlDbType.NVarChar },
                new SqlParameter("@TableSchema", tableSchema) { SqlDbType = SqlDbType.NVarChar },
                new SqlParameter("@TableName", tableName) { SqlDbType = SqlDbType.NVarChar }
            };

            return await SQLReadDataTableAsync(connectionString, query, sqlParameters, cancellationToken);
        }
        #endregion

        #region Data Manipulation
        public static string[] GetColumnNames(DataTable dataTable)
        {
            if (dataTable == null) { throw new ArgumentNullException(nameof(dataTable)); }
            else if (dataTable.Columns.Count < 1) { throw new ArgumentException($"{nameof(dataTable)} has no columns.", nameof(dataTable)); }

            return dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
        }

        public static DataSet CreateDataSetFromRows(DataRow[] dataRows)
        {
            if (dataRows == null) { throw new ArgumentNullException(nameof(dataRows)); }
            else if (dataRows.Length < 1) { throw new ArgumentException($"{nameof(dataRows)} contains no rows.", nameof(dataRows)); }

            using (DataSet dataSet = new DataSet())
            {
                dataSet.Merge(dataRows);

                return dataSet;
            }
        }

        public static DataTable CreateDataTableFromRows(DataRow[] dataRows)
        {
            if (dataRows == null) { throw new ArgumentNullException(nameof(dataRows)); }
            else if (dataRows.Length < 1) { throw new ArgumentException($"{nameof(dataRows)} contains no rows.", nameof(dataRows)); }

            using (DataSet dataSet = new DataSet())
            {
                dataSet.Merge(dataRows);

                return dataSet.Tables[0];
            }
        }
        #endregion

        #region Data Functions
        public static object Coalesce(params object[] parameters)
        {
            if (parameters == null) { throw new ArgumentNullException(nameof(parameters)); }
            else if (parameters.Length < 1) { throw new ArgumentException($"{nameof(parameters)} contains no objects.", nameof(parameters)); }

            object value = DBNull.Value;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != null && parameters[i] != DBNull.Value)
                {
                    value = parameters[i];
                    break;
                }
            }

            return value;
        }

        public static object IfNull(object firstObject, object secondObject)
        {
            return IsNull(firstObject, secondObject);
        }

        public static bool IsNull(object value)
        {
            return value == null || value == DBNull.Value;
        }

        public static object IsNull(object firstObject, object secondObject)
        {
            return firstObject == null || firstObject == DBNull.Value ? secondObject : firstObject;
        }

        public static object NullIf(object firstObject, object secondObject)
        {
            return firstObject != null && firstObject != DBNull.Value && firstObject.Equals(secondObject) ? DBNull.Value : firstObject;
        }

        public static string QuoteName(string text, char quoteChar)
        {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            else if (string.Equals(text, string.Empty)) { throw new ArgumentException($"{nameof(text)} cannot be an empty string.", nameof(text)); }

            switch (quoteChar)
            {
                case '\'':
                    text = $"'{text}'";
                    break;
                case '"':
                    text = $"\"{text}\"";
                    break;
                case '`':
                    text = $"`{text}`";
                    break;
                case '(':
                case ')':
                    text = $"({text})";
                    break;
                case '{':
                case '}':
                    text = "{" + text + "}";
                    break;
                case '<':
                case '>':
                    text = $"<{text}>";
                    break;
                case '[':
                case ']':
                default:
                    text = $"[{text}]";
                    break;
            }

            return text;
        }
        #endregion
    }
}