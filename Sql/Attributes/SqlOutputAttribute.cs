using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlOutputAttribute : Attribute
    {
        public SqlDbType? DatabaseType { get { return _databaseType; } }
        public int Size { get { return _size; } }
        public byte Scale { get { return _scale; } }
        public byte Precision { get { return _precision; } }
        public string Alias { get { return _alias; } }

        private readonly SqlDbType? _databaseType;
        private readonly int _size;
        private readonly byte _scale;
        private readonly byte _precision;
        private readonly string _alias;

        public SqlOutputAttribute(SqlDbType dbType, int size, byte scale = 0, byte precision = 0, string alias = null)
            :base()
        {
            if (!Enum.IsDefined(typeof(SqlDbType), dbType))
            {
                throw new ArgumentOutOfRangeException("dbType", string.Format("The underlying value {0} for the enum SqlDbType is not defined.", Convert.ToInt64(dbType)));
            }

            _databaseType = dbType;
            _size = size;
            _scale = scale;
            _precision = precision;
            _alias = alias;
        }

        public SqlOutputAttribute(SqlDbType dbType, byte scale = 0, byte precision = 0, string alias = null)
            : this(dbType, 0, scale, precision, alias)
        {
            
        }

        public SqlOutputAttribute(int size, byte scale = 0, byte precision = 0, string alias = null)
            :base()
        {
            _databaseType = null;
            _size = size;
            _scale = scale;
            _precision = precision;
            _alias = alias;
        }

        public SqlOutputAttribute(byte scale = 0, byte precision = 0, string alias = null)
            : this(0, scale, precision, alias)
        {

        }

    }
}
