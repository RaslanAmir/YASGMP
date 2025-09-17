using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using YasGMP.Data;

namespace YasGMP.EfMigrations;

public class DesignTimeYasGmpDbContextFactory : IDesignTimeDbContextFactory<YasGmpDbContext>
{
    public YasGmpDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(basePath, "..", "appsettings.json"), optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var connectionString = configuration.GetConnectionString("MySqlDb")
            ?? configuration["ConnectionStrings:MySqlDb"]
            ?? "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        var optionsBuilder = new DbContextOptionsBuilder<YasGmpDbContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion, options =>
        {
            options.SchemaBehavior(MySqlSchemaBehavior.Ignore);
        });

        return new YasGmpDbContext(optionsBuilder.Options);
    }
}
