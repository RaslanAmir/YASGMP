using MySqlConnector;

namespace YasGMP.Wpf.Configuration;

/// <summary>
/// Lightweight options record exposing the database catalog and server extracted from the configured connection string.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Gets the database catalog name resolved from the connection string.</summary>
    public string Database { get; init; } = string.Empty;

    /// <summary>Gets the server/host name resolved from the connection string.</summary>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Creates a <see cref="DatabaseOptions"/> instance from the supplied connection string.
    /// </summary>
    /// <param name="connectionString">The configured database connection string.</param>
    public static DatabaseOptions FromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DatabaseOptions();
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            return new DatabaseOptions
            {
                Database = builder.Database ?? string.Empty,
                Server = builder.Server ?? string.Empty
            };
        }
        catch
        {
            return new DatabaseOptions();
        }
    }
}
