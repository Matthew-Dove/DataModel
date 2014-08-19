using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql.Attributes
{
    /// <summary>
    /// Expects a 32-bit int for the return type
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlReturnAttribute : Attribute
    {
        public SqlReturnAttribute()
            :base()
        {

        }
    }
}
