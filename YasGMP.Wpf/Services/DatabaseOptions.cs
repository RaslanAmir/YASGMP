using System;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Provides the configured database connection string for services that
    /// need to talk to the legacy MySQL instance from the WPF shell.
    /// </summary>
    public sealed class DatabaseOptions
    {
        public DatabaseOptions(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            }

            ConnectionString = connectionString;
        }

        /// <summary>Gets the resolved MySQL connection string.</summary>
        public string ConnectionString { get; }
    }
}

