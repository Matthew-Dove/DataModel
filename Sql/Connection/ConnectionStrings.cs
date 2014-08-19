using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DataLayer.Sql.Connection
{
    /// <summary>
    /// Holds connection strings to databases, this class is thread safe.
    /// </summary>
    public class ConnectionStrings
    {
        /// <summary>
        /// Returns the connection for the currently selected key, null if no keys have been added yet.
        /// </summary>
        public string Current { get {
            return Select(_current);
            } 
        }

        private volatile string _current;
        private readonly object _padLock = null;  // Internal locking when writing to _current, let volatile handle the reads.

        private readonly ConcurrentDictionary<string, string> _connections = null;

        public ConnectionStrings()
        {
            _current = null;
            _connections = new ConcurrentDictionary<string, string>();
            _padLock = new object();
        }

        public ConnectionStrings(string key, string connection)
            :this()
        {
            Upsert(key, connection);
        }

        public ConnectionStrings(string key, SqlConnectionString connection)
            : this()
        {
            Upsert(key, connection);
        }

        public void SetCurrent(string key)
        {
            key.CheckArgument("key");
            if (_connections.Keys.Count(x => (x ?? string.Empty).Equals(key)) == 1)
            {
                lock (_padLock)
                    _current = key;
            }
        }

        /// <summary>
        /// Updates a key if it exists otherwise inserts the key with the connection.
        /// </summary>
        public void Upsert(string key, string connection)
        {
            key.CheckArgument("key");
            connection.CheckArgument("connection");

            _connections.AddOrUpdate(key, connection, (x, y) => connection);

            if (_connections.Count == 1) // If one was just added
            {
                lock (_padLock)
                    _current = key;
            }
        }        

        /// <summary>
        /// Updates a key if it exists otherwise inserts the key with the connection.
        /// </summary>
        public void Upsert(string key, SqlConnectionString connection)
        {
            Upsert(key, connection.GetConnectionString());
        }

        /// <summary>
        /// Removes a key from the connection strings,
        /// if the removed key is the current connection,
        /// the current connection is changed to the last entry,
        /// or null if there isn't another entry.
        /// </summary>
        public void Delete(string key)
        {
            key.CheckArgument("key");

            string delete = null;
            _connections.TryRemove(key, out delete);

            if (_current == key) // Need to change the current key
            {
                lock (_padLock)
                {
                    _current = null;
                    if (_connections.Count > 0)
                    {
                        _current = _connections.Last().Key;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the connection for a key.
        /// If the key does not exist null is returned.
        /// </summary>
        public string Select(string key)
        {
            key.CheckArgument("key");
            string connection = null;

            _connections.TryGetValue(key, out connection);

            return connection;
        }
    }
}
