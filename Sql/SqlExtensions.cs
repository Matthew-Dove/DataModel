using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Sql.Attributes;
using FastMember;

namespace DataLayer.Sql
{
    public static class SqlExtensions
    {
        [DebuggerStepThrough]
        public static T Get<T>(this IDataRecord data, string columnName, T @default = default(T))
        {
            int ordinal = -1;

            if (!string.IsNullOrWhiteSpace(columnName))
            {
                ordinal = data.GetOrdinal(columnName);
            }

            return data.Get<T>(ordinal, @default);
        }
        
        [DebuggerStepThrough]
        public static T Get<T>(this IDataRecord data, int ordinal, T @default = default(T))
        {
            T value = @default;

            if (ordinal > -1 && !data.IsDBNull(ordinal))
            {
                object rawValue = data.GetValue(ordinal);
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
            }

            return value;
        }

        [DebuggerStepThrough]
        public static T Get<T>(this IDataRecord data, Func<IDataRecord, T> function)
        {
            return function(data);
        }

        /// <summary>
        /// Turns a model in SqlParameters.
        /// Applies [SqlIgnorge], [SqlOutput], [SqlReturn], [SqlAlias] attributes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SqlParameter[] ToSqlParameters<T>(this T model) where T : class
        {
            List<SqlParameter> parameters = new List<SqlParameter>();

            TypeAccessor accessor = TypeAccessor.Create(typeof(T));
            MemberSet members = accessor.GetMembers();

            bool isReturnMemberDefined = false;

            /* *
             * If member is ignorged, continue.
             * If member is output set it, then continue.
             * If member is return and we haven't set a return member yet, set it then continue.
             * If the member has no matching attribues, set it as a vanilla parameter and continue.
             */

            foreach (var member in members)
            {
                if (!member.IsDefined(typeof(SqlIgnorgeAttribute)))
                {
                    var sqlParameter = new SqlParameter("@" + member.Name, accessor[model, member.Name] ?? DBNull.Value);

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
                            size = GetSize(sqlDbType.ToClrType());
                        }

                        sqlParameter.Scale = outputAttribute.Scale;
                        sqlParameter.Precision = outputAttribute.Precision;
                        sqlParameter.Direction = ParameterDirection.Output;

                        if (!string.IsNullOrWhiteSpace(outputAttribute.Alias))
                        {
                            sqlParameter.ParameterName = "@" + outputAttribute.Alias;
                        }
                    }
                    else
                    {
                        if (member.IsDefined(typeof(SqlReturnAttribute)))
                        {
                            if (isReturnMemberDefined == true)
                            {
                                continue;
                            }

                            isReturnMemberDefined = true;
                            sqlParameter.Direction = ParameterDirection.ReturnValue;
                        }
                        else if (member.IsDefined(typeof(SqlAliasAttribute)))
                        {
                            var aliasAttribute = SqlExtensions.GetAttribute<T, SqlAliasAttribute>(member.Name);
                            sqlParameter.ParameterName = "@" + aliasAttribute.Alias.Split(',')[0];
                        }
                    }

                    parameters.Add(sqlParameter);
                }
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Gets the first attribute from a member that matches the type
        /// </summary>
        /// <typeparam name="M">The model type</typeparam>
        /// <typeparam name="A">The attribute type</typeparam>
        /// <param name="name">The name of the property or field</param>
        /// <returns>The attribute for the member</returns>
        [DebuggerStepThrough]
        public static A GetAttribute<M, A>(string name)
        {
            A attribute = default(A);

            var propertyInfo = typeof(M).GetProperty(name);

            if (propertyInfo == null)
            {
                var memberInfo = typeof(M).GetMember(name)[0];
                attribute = (A)memberInfo.GetCustomAttributes(typeof(A), true)[0];
            }
            else
            {
                attribute = (A)propertyInfo.GetCustomAttributes(typeof(A), true)[0];
            }

            return attribute;
        }

        /// <summary>
        /// Gets the size in bytes of a value type
        /// </summary>
        [DebuggerStepThrough]
        public static int GetSize(this Type type)
        {
            int size = 0;

            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type.IsValueType)
            {
                size = System.Runtime.InteropServices.Marshal.SizeOf(type);
            }
            else
            {
                throw new ArgumentException(string.Format("The type ({0}) must be a value type, that is an enum or a struct.", type.Name));
            }

            return size;
        }

        [DebuggerStepThrough]
        public static Type ToClrType(this SqlDbType sqlType)
        {
            switch (sqlType)
            {
                case SqlDbType.BigInt:
                    return typeof(long);

                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                case SqlDbType.VarBinary:
                    return typeof(byte[]);

                case SqlDbType.Bit:
                    return typeof(bool);

                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                    return typeof(string);

                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Date:
                case SqlDbType.Time:
                case SqlDbType.DateTime2:
                    return typeof(DateTime);

                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    return typeof(decimal);

                case SqlDbType.Float:
                    return typeof(double);

                case SqlDbType.Int:
                    return typeof(int);

                case SqlDbType.Real:
                    return typeof(float);

                case SqlDbType.UniqueIdentifier:
                    return typeof(Guid);

                case SqlDbType.SmallInt:
                    return typeof(short);

                case SqlDbType.TinyInt:
                    return typeof(byte);

                case SqlDbType.Variant:
                case SqlDbType.Udt:
                    return typeof(object);

                case SqlDbType.Structured:
                    return typeof(DataTable);

                case SqlDbType.DateTimeOffset:
                    return typeof(DateTimeOffset);

                default:
                    throw new ArgumentOutOfRangeException("sqlType", Enum.GetName(typeof(SqlDbType), sqlType));
            }
        }

        [DebuggerStepThrough]
        public static SqlDbType ToSqlType(this Type type)
        {
            if (type.Equals(typeof(int)))
            {
                return SqlDbType.Int;
            }
            else if (type.Equals(typeof(string)))
            {
                return SqlDbType.NVarChar;
            }
            else if (type.Equals(typeof(DateTime)))
            {
                return SqlDbType.DateTime;
            }
            else if (type.Equals(typeof(bool)))
            {
                return SqlDbType.Bit;
            }
            else if (type.Equals(typeof(byte)))
            {
                return SqlDbType.TinyInt;
            }
            else if (type.Equals(typeof(long)))
            {
                return SqlDbType.BigInt;
            }
            else if (type.Equals(typeof(decimal)))
            {
                return SqlDbType.Decimal;
            }
            else if (type.Equals(typeof(short)))
            {
                return SqlDbType.SmallInt;
            }
            else if (type.Equals(typeof(float)))
            {
                return SqlDbType.Real;
            }
            else if (type.Equals(typeof(byte[])))
            {
                return SqlDbType.VarBinary;
            }
            else if (type.Equals(typeof(Guid)))
            {
                return SqlDbType.UniqueIdentifier;
            }
            else if (type.Equals(typeof(object)))
            {
                return SqlDbType.Udt;
            }
            else if (type.Equals(typeof(DateTimeOffset)))
            {
                return SqlDbType.DateTimeOffset;
            }

            throw new ArgumentOutOfRangeException("type", type.Name);
        }

    }
}
