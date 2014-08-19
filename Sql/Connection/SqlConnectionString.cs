using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Sql.Connection
{
    public class SqlConnectionString
    {
        public string Server { get; private set; }
        public bool IntegratedSecurity { get; private set; }
        public string InitialCatalog { get; private set; }
        public string ApplicationName { get; private set; }
        public string UserId { get; private set; }
        public string Password { get; private set; }

        public SqlConnectionString(string server, string userId, string password, 
            string initialCatalog = "Master", string applicationName = "SqlDataLayer", bool integratedSecurity = false)
        {
            Server = server;
            IntegratedSecurity = integratedSecurity;
            InitialCatalog = initialCatalog;
            ApplicationName = applicationName;
            UserId = userId;
            Password = password;
        }

        public SqlConnectionString(string server, string initialCatalog = "Master", 
            string applicationName = "SqlDataLayer", bool integratedSecurity = true)
        {
            Server = server;
            IntegratedSecurity = integratedSecurity;
            InitialCatalog = initialCatalog;
            ApplicationName = string.Empty;
            UserId = null;
            Password = null;
        }

        public string GetConnectionString()
        {
            return this.ToString();
        }
        
        public override string ToString()
        {
            if (UserId == null && Password == null)
            {
                return string.Format("Server={0};Integrated Security={1};Initial Catalog={2};Application Name={3};",
                Server, IntegratedSecurity.ToString().ToLower(), InitialCatalog, ApplicationName);
            }

            return string.Format("Server={0};Integrated Security={1};Initial Catalog={2};Application Name={3};User ID={4};Password={5};",
                Server, IntegratedSecurity.ToString().ToLower(), InitialCatalog, ApplicationName, UserId ?? string.Empty, Password ?? string.Empty);
        }
    }
}
