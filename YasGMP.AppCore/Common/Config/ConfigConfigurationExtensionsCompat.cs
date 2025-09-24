// ==============================================================================
// Purpose : Provides a lightweight GetConnectionString extension for IConfiguration
//           so calls like config.GetConnectionString("MySqlDb") compile without
//           additional NuGet packages. Reads "ConnectionStrings:Name" from config.
// ==============================================================================
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Compatibility extensions for <see cref="IConfiguration"/> used by YasGMP.
    /// </summary>
    public static class ConfigurationExtensionsCompat
    {
        /// <summary>
        /// Returns the connection string with the given <paramref name="name"/> from
        /// the configuration path <c>ConnectionStrings:{name}</c>. Returns <c>null</c>
        /// if the value is missing or empty.
        /// </summary>
        public static string? GetConnectionString(this IConfiguration configuration, string name)
            => configuration[$"ConnectionStrings:{name}"];
    }
}
