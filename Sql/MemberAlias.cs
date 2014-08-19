using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql
{
    /// <summary>
    /// Has the name of a member, as well as its alias
    /// </summary>
    internal class MemberAlias
    {
        public string Name { get { return _name; } }
        public string Alias { get { return _alias; } }

        private readonly string _name = null;
        private readonly string _alias = null;

        public MemberAlias(string name, string alias)
        {
            _name = name;
            _alias = alias;
        }
    }
}
