using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Sql.Attributes;
using DataLayer.Sql.Connection;
using FastMember;

namespace DataLayer.Sql
{
    public class SqlLayer
    {
        /// <summary>
        /// Holds a range of connection strings that can be used
        /// </summary>
        public static ConnectionStrings ConnectionString { get; set; }

        /// <summary>
        /// Only call this constructor if the connection string has already been set once before
        /// </summary>
        static SqlLayer()
        {
            ConnectionString = new ConnectionStrings();
        }

        public static void SaveModel<T>(string uspName, T model) where T : class
        {
            SaveModel<T>(ConnectionString.Current, uspName, model);
        }

        public static void SaveModel<T>(string connectionString, string uspName, T model) where T : class
        {
            if (model == null)
            {
                throw new ArgumentNullException("model", "model cannot be null.");
            }

            var sqlParameters = model.ToSqlParameters();
            Save(connectionString, uspName, sqlParameters);

            // Check for output and return sql parameters to set on the model
            bool isReturnSet = false;
            TypeAccessor accessor = TypeAccessor.Create(typeof(T));
            MemberSet members = accessor.GetMembers();

            foreach (var member in members)
            {
                if (member.IsDefined(typeof(SqlOutputAttribute)))
                {
                    string parameterName = "@" + member.Name;
                    var sqlOutput = SqlExtensions.GetAttribute<T, SqlOutputAttribute>(member.Name);
                    if (!string.IsNullOrWhiteSpace(sqlOutput.Alias))
                    {
                        parameterName = "@" + sqlOutput.Alias;
                    }

                    var sqlParameter = sqlParameters.First(x => x.ParameterName.Equals(parameterName));

                    if (!sqlParameter.Value.GetType().Equals(typeof(DBNull)))
                    {
                        accessor[model, member.Name] = sqlParameter.Value;
                    }
                }
                else if (!isReturnSet && member.IsDefined(typeof(SqlReturnAttribute)))
                {
                    var sqlParameter = sqlParameters.First(x => x.ParameterName.Equals("@" + member.Name));
                    accessor[model, member.Name] = Convert.ToInt32(sqlParameter.Value);
                    isReturnSet = true;
                }
            }
        }

        public static void Save(string uspName, params SqlParameter[] parameters)
        {
            Save(ConnectionString.Current, uspName, parameters);
        }

        public static void Save(string connectionString, string uspName, params SqlParameter[] parameters)
        {
            parameters = (parameters ?? new SqlParameter[] { }).Select(x =>
            {
                if (x.Value == null)
                    x.SqlValue = DBNull.Value;
                return x;
            }).ToArray();

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = uspName;
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }

        public static T LoadModel<T>(string uspName, params SqlParameter[] parameters) where T : new()
        {
            return LoadModel<T>(ConnectionString.Current, uspName, parameters);
        }

        public static T LoadModel<T>(string connectionString, string uspName, params SqlParameter[] parameters) where T : new()
        {
            var models = LoadModels<T>(connectionString, uspName, parameters);

            if (models != null && models.Count() > 0)
            {
                return models.ElementAt(0);
            }

            return new T();
        }

        public static IEnumerable<T> LoadModels<T>(string uspName, params SqlParameter[] parameters) where T : new()
        {
            return LoadModels<T>(ConnectionString.Current, uspName, parameters);
        }

        public static IEnumerable<T> LoadModels<T>(string connectionString, string uspName, params SqlParameter[] parameters) where T : new()
        {
            connectionString.CheckArgument();
            uspName.CheckArgument();

            List<T> models = new List<T>();

            TypeAccessor accessor = TypeAccessor.Create(typeof(T), false);
            MemberSet members = accessor.GetMembers();

            var outputAliasNames = new List<MemberAlias>();
            var aliasNames = new List<MemberAlias>();
            var ignorgeProperties = new List<string>();
            var outputParameters = new List<SqlParameter>();
            string returnMember = null;

            // Look for return and output attributes
            foreach (var member in members)
            {
                if (member.IsDefined(typeof(SqlIgnorgeAttribute)))
                {
                    ignorgeProperties.Add(member.Name);
                }
                else
                {
                    if (member.IsDefined(typeof(SqlOutputAttribute)))
                    {
                        SqlOutputAttribute outputAttribute = SqlExtensions.GetAttribute<T, SqlOutputAttribute>(member.Name);
                        SqlDbType sqlDbType = outputAttribute.DatabaseType ?? member.Type.ToSqlType();

                        // If type is string or byte, we must know the size before hand
                        if (outputAttribute.Size < 1 && member.Type.Equals(typeof(string)))
                        {
                            throw new ArgumentException(string.Format("You must provide the number of expected characters for the string variable {0}, on the type {1}. Refer to the SqlOutputAttribute.",
                                member.Name, typeof(T).Name));
                        }
                        else if (outputAttribute.Size < 1 && member.Type.Equals(typeof(byte[])))
                        {
                            throw new ArgumentException(string.Format("You must provide the number of expected bytes for the byte array {0}, on the type {1}. Refer to the SqlOutputAttribute.",
                                member.Name, typeof(T).Name));
                        }

                        // If we don't know what the size is, then we need to set it.
                        int size = outputAttribute.Size;
                        if (size < 1)
                        {
                            size = sqlDbType.ToClrType().GetSize();
                        }

                        var parameter = new SqlParameter("@" + member.Name, sqlDbType, size);
                        parameter.Scale = outputAttribute.Scale;
                        parameter.Precision = outputAttribute.Precision;
                        parameter.Direction = ParameterDirection.Output;

                        if (!string.IsNullOrWhiteSpace(outputAttribute.Alias))
                        {
                            outputAliasNames.Add(new MemberAlias(member.Name, outputAttribute.Alias));
                            parameter.ParameterName = "@" + outputAttribute.Alias;
                        }

                        outputParameters.Add(parameter);
                    }
                    else
                    {
                        if (returnMember == null && member.IsDefined(typeof(SqlReturnAttribute)))
                        {
                            returnMember = member.Name; // Max one return attribute per model
                        }
                        else if (member.IsDefined(typeof(SqlAliasAttribute)))
                        {
                            var aliasAttribute = SqlExtensions.GetAttribute<T, SqlAliasAttribute>(member.Name);
                            IEnumerable<string> aliases = aliasAttribute.Alias.Split(',');

                            foreach (var alias in aliases)
                            {
                                aliasNames.Add(new MemberAlias(member.Name, alias));
                            }
                        }
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = uspName;
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddRange(parameters);

                // Add the return value parameter
                var returnValue = new SqlParameter(null, SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;
                command.Parameters.Add(returnValue);

                // Add the output parameters
                command.Parameters.AddRange(outputParameters.ToArray());

                using (var dr = command.ExecuteReader())
                {
                    if (dr.HasRows)
                    {
                        int totalColumns = dr.FieldCount;
                        string[] columnNames = new string[totalColumns];
                        for (int i = 0; i < totalColumns; i++)
                            columnNames[i] = dr.GetName(i);

                        // Go through each row
                        while (dr.Read())
                        {
                            T model = new T();

                            // Go through each coloumn in this row
                            foreach (string columnName in columnNames)
                            {
                                var member = members.FirstOrDefault(x => x.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase) && // Match table column names with model member names
                                    ignorgeProperties.Count(y => y.Equals(x.Name)) == 0); // Don't match them if the member should be ignorged

                                // If no match was found, but there are aliases, check if an alias matches the column name
                                if (member == null && aliasNames.Count > 0)
                                {
                                    var match = aliasNames.FirstOrDefault(x => x.Alias.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                                    if (match != null)
                                    {
                                        member = members.FirstOrDefault(x => x.Name.Equals(match.Name));
                                    }
                                }

                                if (member != null)
                                {
                                    accessor[model, member.Name] = dr.IsDBNull(dr.GetOrdinal(columnName)) ? GetDefaultValue(member.Type) : dr[columnName];
                                }
                            }

                            models.Add(model);
                        }

                        dr.Close();

                        // Get return and output values

                        if (returnMember != null)
                        {
                            foreach (var model in models)
                            {
                                accessor[model, returnMember] = Convert.ToInt32(returnValue.Value);
                            }
                        }
                        
                        foreach (var parameter in outputParameters)
                        {   // Remove the prefixed '@'
                            string memberName = parameter.ParameterName.Substring(1, parameter.ParameterName.Length - 1);
                            foreach (var model in models)
                            {
                                if (!parameter.Value.GetType().Equals(typeof(DBNull)))
                                {
                                    // Check if this output parameter had an alias
                                    var match = outputAliasNames.FirstOrDefault(x => x.Alias.Equals(memberName, StringComparison.OrdinalIgnoreCase));
                                    if (match != null)
                                    {
                                        memberName = match.Name;
                                    }

                                    accessor[model, memberName] = parameter.Value;
                                }
                            }
                        }
                    }
                }
            }

            return models;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

    }
}
