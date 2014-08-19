using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Sql.Connection;

namespace DataLayer.Sql
{
    public class SqlProvider : IDisposable
    {
        public ConnectionStrings ConnectionStrings { get { return _connectionStrings; } }

        private readonly ConnectionStrings _connectionStrings = null;
        private readonly string _connectionString = null;
        private SqlConnection _sqlConnection = null;
        private SqlDataReader _sqlDataReader = null;

        public SqlProvider(ConnectionStrings connectionStrings)
        {
            _connectionStrings = connectionStrings;
        }

        public SqlProvider(string connectionString)
        {            
            _connectionString = connectionString;
        }

        public SqlProvider(SqlConnectionString connectionString)
            : this(connectionString.GetConnectionString())
        {

        }

        private SqlConnection GetSqlConnection()
        {
            if (_sqlConnection == null)
            {
                if (_connectionString == null)
                {
                    if (_connectionStrings != null && _connectionStrings.Current != null)
                    {
                        _sqlConnection = new SqlConnection(_connectionStrings.Current);
                    }
                }
                else
                {
                    _sqlConnection = new SqlConnection(_connectionString);
                }
            }

            if (_sqlConnection == null)
            {
                throw new Exception("Cannot create a sqlConnection because a connection string was not supplied.");
            }

            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                if (_sqlConnection.State != System.Data.ConnectionState.Closed)
                {
                    _sqlConnection.Close();
                }

                _sqlConnection.Open();
            }

            return _sqlConnection; 
        }

        private SqlCommand GetSqlCommand(string uspName, params SqlParameter[] parameters)
        {
            SqlConnection connection = GetSqlConnection();

            SqlCommand sqlCommand = connection.CreateCommand();
            sqlCommand.CommandText = uspName;
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCommand.Parameters.AddRange(parameters);

            return sqlCommand;
        }

        public SqlDataReader ExecuteReader(string uspName, params SqlParameter[] parameters)
        {
            using (var sqlCommand = GetSqlCommand(uspName, parameters))
            {
                if (_sqlDataReader != null)
                {
                    _sqlDataReader.Dispose();
                }

                _sqlDataReader = sqlCommand.ExecuteReader();
                return _sqlDataReader;
            }
        }

        /// <summary>
        /// Returns the number of rows affected
        /// </summary>
        public int ExecuteNonQuery(string uspName, params SqlParameter[] parameters)
        {
            using (var sqlCommand = GetSqlCommand(uspName, parameters))
            {
                return sqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns the first coloumn of the first row
        /// </summary>
        public T ExecuteScalar<T>(string uspName, T @default = default(T), params SqlParameter[] parameters)
        {
            T value = @default;
            object rawValue = null;

            using (var sqlCommand = GetSqlCommand(uspName, parameters))
            {
                rawValue = sqlCommand.ExecuteScalar();
            }

            if (rawValue != null)
            {
                Type type = typeof(T);

                // If the type is int? then we want to convert it to int
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition())
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                // If the type is an enum we need a different conversion
                if (type.IsEnum)
                {
                    if (Enum.IsDefined(type, rawValue))
                    {
                        value = (T)Enum.ToObject(type, rawValue);
                    }
                }
                else
                {
                    value = (T)Convert.ChangeType(rawValue, type);
                }
            }

            return value;
        }

        public void Dispose()
        {
            if (_sqlDataReader != null)
            {
                _sqlDataReader.Dispose();
            }
            if (_sqlConnection != null)
            {
                _sqlConnection.Dispose();
            }
        }

    }
}
