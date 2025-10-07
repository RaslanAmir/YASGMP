using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using YasGMP.Data;

namespace YasGMP.AppCore.DependencyInjection
{
    /// <summary>
    /// Builder used to configure YasGMP core service registrations for platform hosts.
    /// </summary>
    public sealed class YasGmpCoreBuilder
    {
        private Func<IServiceProvider, string>? _connectionResolver;
        private Func<IServiceProvider, ServerVersion>? _serverVersionResolver;
        private Action<IServiceProvider, DbContextOptionsBuilder, string>? _dbContextPostConfigure;
        private bool _registerDbContextFactory = true;
        private Type? _databaseServiceType;
        private Func<IServiceProvider, string, object>? _databaseFactory;
        private Action<IServiceProvider, object, string>? _databasePostConfigure;

        internal YasGmpCoreBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>Gets the underlying service collection for additional registrations.</summary>
        public IServiceCollection Services { get; }

        /// <summary>Uses a constant connection string.</summary>
        public YasGmpCoreBuilder UseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            }

            return UseConnectionString(_ => connectionString);
        }

        /// <summary>Uses the provided resolver to obtain the MySQL connection string.</summary>
        public YasGmpCoreBuilder UseConnectionString(Func<IServiceProvider, string> resolver)
        {
            _connectionResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            return this;
        }

        /// <summary>Registers a pre-computed server version.</summary>
        public YasGmpCoreBuilder UseServerVersion(ServerVersion version)
        {
            if (version is null) throw new ArgumentNullException(nameof(version));
            return UseServerVersion(_ => version);
        }

        /// <summary>Registers a resolver for the database server version.</summary>
        public YasGmpCoreBuilder UseServerVersion(Func<IServiceProvider, ServerVersion> resolver)
        {
            _serverVersionResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            return this;
        }

        /// <summary>Allows customization of DbContext options after the provider has been configured.</summary>
        public YasGmpCoreBuilder ConfigureDbContext(Action<IServiceProvider, DbContextOptionsBuilder, string> configure)
        {
            _dbContextPostConfigure = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <summary>Enables or disables registration of <see cref="IDbContextFactory{TContext}"/>.</summary>
        public YasGmpCoreBuilder EnableDbContextFactory(bool enable = true)
        {
            _registerDbContextFactory = enable;
            return this;
        }

        /// <summary>Registers the DatabaseService factory used by both mobile and desktop hosts.</summary>
        public YasGmpCoreBuilder UseDatabaseService<TService>(
            Func<IServiceProvider, string, TService> factory,
            Action<IServiceProvider, TService, string>? configure = null) where TService : class
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            _databaseServiceType = typeof(TService);
            _databaseFactory = (sp, conn) => factory(sp, conn)!;

            if (configure is not null)
            {
                _databasePostConfigure = (sp, instance, conn) => configure(sp, (TService)instance, conn);
            }
            else
            {
                _databasePostConfigure = null;
            }

            return this;
        }

        internal YasGmpCoreState Build()
        {
            if (_connectionResolver is null)
            {
                throw new InvalidOperationException("UseConnectionString must be configured before building the YasGMP core services.");
            }

            var connectionResolver = _connectionResolver;
            var serverVersionResolver = _serverVersionResolver;
            var dbContextPostConfigure = _dbContextPostConfigure;
            var registerFactory = _registerDbContextFactory;
            var databaseServiceType = _databaseServiceType;
            var databaseFactory = _databaseFactory;
            var databasePostConfigure = _databasePostConfigure;

            var versionLock = new object();
            ServerVersion? cachedVersion = null;
            string? cachedConnection = null;

            ServerVersion ResolveServerVersion(IServiceProvider sp, string connection)
            {
                if (serverVersionResolver is not null)
                {
                    var version = serverVersionResolver(sp);
                    return version ?? throw new InvalidOperationException("Server version resolver returned null.");
                }

                lock (versionLock)
                {
                    if (cachedVersion is null || !string.Equals(cachedConnection, connection, StringComparison.Ordinal))
                    {
                        cachedVersion = ServerVersion.AutoDetect(connection);
                        cachedConnection = connection;
                    }

                    return cachedVersion;
                }
            }

            return new YasGmpCoreState(
                connectionResolver,
                ResolveServerVersion,
                dbContextPostConfigure,
                registerFactory,
                databaseServiceType,
                databaseFactory,
                databasePostConfigure);
        }
    }

    internal sealed class YasGmpCoreState
    {
        /// <summary>
        /// Initializes a new instance of the YasGmpCoreState class.
        /// </summary>
        public YasGmpCoreState(
            Func<IServiceProvider, string> connectionResolver,
            Func<IServiceProvider, string, ServerVersion> serverVersionResolver,
            Action<IServiceProvider, DbContextOptionsBuilder, string>? dbContextPostConfigure,
            bool registerDbContextFactory,
            Type? databaseServiceType,
            Func<IServiceProvider, string, object>? databaseFactory,
            Action<IServiceProvider, object, string>? databasePostConfigure)
        {
            ConnectionResolver = connectionResolver;
            ServerVersionResolver = serverVersionResolver;
            DbContextPostConfigure = dbContextPostConfigure;
            RegisterDbContextFactory = registerDbContextFactory;
            DatabaseServiceType = databaseServiceType;
            DatabaseFactory = databaseFactory;
            DatabasePostConfigure = databasePostConfigure;
        }
        /// <summary>
        /// Gets or sets the string.
        /// </summary>

        public Func<IServiceProvider, string> ConnectionResolver { get; }
        /// <summary>
        /// Gets or sets the string.
        /// </summary>

        public Func<IServiceProvider, string, ServerVersion> ServerVersionResolver { get; }
        /// <summary>
        /// Gets or sets the db context options builder.
        /// </summary>

        public Action<IServiceProvider, DbContextOptionsBuilder, string>? DbContextPostConfigure { get; }
        /// <summary>
        /// Gets or sets the register db context factory.
        /// </summary>

        public bool RegisterDbContextFactory { get; }
        /// <summary>
        /// Gets or sets the database service type.
        /// </summary>

        public Type? DatabaseServiceType { get; }
        /// <summary>
        /// Gets or sets the string.
        /// </summary>

        public Func<IServiceProvider, string, object>? DatabaseFactory { get; }
        /// <summary>
        /// Gets or sets the object.
        /// </summary>

        public Action<IServiceProvider, object, string>? DatabasePostConfigure { get; }
    }

    /// <summary>
    /// Extension methods that register the YasGMP shared core services with a platform specific host.
    /// </summary>
    public static class YasGmpCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Executes the add yas gmp core services operation.
        /// </summary>
        public static IServiceCollection AddYasGmpCoreServices(
            this IServiceCollection services,
            Action<YasGmpCoreBuilder> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var builder = new YasGmpCoreBuilder(services);
            configure(builder);
            var state = builder.Build();

            void ConfigureDbContext(IServiceProvider sp, DbContextOptionsBuilder options)
            {
                var connection = state.ConnectionResolver(sp);
                var serverVersion = state.ServerVersionResolver(sp, connection);
                options.UseMySql(connection, serverVersion);
                state.DbContextPostConfigure?.Invoke(sp, options, connection);
            }

            services.AddDbContext<YasGmpDbContext>(ConfigureDbContext);

            if (state.RegisterDbContextFactory)
            {
                services.AddDbContextFactory<YasGmpDbContext>(ConfigureDbContext);
            }

            if (state.DatabaseServiceType is not null && state.DatabaseFactory is not null)
            {
                services.AddSingleton(state.DatabaseServiceType, sp =>
                {
                    var connection = state.ConnectionResolver(sp);
                    var instance = state.DatabaseFactory(sp, connection)
                                   ?? throw new InvalidOperationException("Database service factory returned null.");

                    if (!state.DatabaseServiceType.IsInstanceOfType(instance))
                    {
                        throw new InvalidOperationException($"Factory produced instance of type {instance.GetType()} which is not assignable to {state.DatabaseServiceType}.");
                    }

                    state.DatabasePostConfigure?.Invoke(sp, instance, connection);
                    return instance;
                });
            }

            return services;
        }
    }

    /// <summary>
    /// Guard helpers that ensure shared YasGMP services are registered with the expected lifetimes.
    /// </summary>
    public static class YasGmpCoreServiceGuards
    {
        /// <summary>
        /// Removes any pre-existing registrations for <see cref="AuditService"/> and re-adds it as a singleton.
        /// </summary>
        /// <remarks>
        /// Some hosts accidentally registered <see cref="AuditService"/> multiple times (e.g. transient + singleton).
        /// This guard normalises the registration so dependents always resolve the singleton instance.
        /// </remarks>
        /// <param name="services">Service collection to normalise.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static void EnsureAuditServiceSingleton(IServiceCollection services)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            services.RemoveAll<AuditService>();
            services.AddSingleton<AuditService>();
        }
    }
}
