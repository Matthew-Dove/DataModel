using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql.Attributes
{
    /// <summary>
    /// Ignorge this property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlIgnorgeAttribute : Attribute
    {
        public SqlIgnorgeAttribute()
            :base()
        {

        }
    }
}
