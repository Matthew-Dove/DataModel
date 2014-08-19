using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql.Attributes
{
    /// <summary>
    /// A comma separated list of names to use instead of the property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlAliasAttribute : Attribute
    {
        public string Alias { get { return _alias; } }
        private readonly string _alias = null;

        public SqlAliasAttribute(string alias)
            :base()
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentNullException("alias", "The alias cannot be null or empty");
            }
            _alias = alias;
        }
    }
}
