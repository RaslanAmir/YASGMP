using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YasGMP.Data
{
    /// <summary>
    /// Provides a design-time factory so that EF Core tools (dotnet ef) can build
    /// migrations without relying on the MAUI host bootstrapping logic.
    /// </summary>
    public sealed class YasGmpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<YasGmpDbContext>
    {
        public YasGmpDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<YasGmpDbContext>();
            var connection = Environment.GetEnvironmentVariable("YASGMP_ConnectionString")
                             ?? "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;";
            optionsBuilder.UseMySql(connection, ServerVersion.AutoDetect(connection));
            return new YasGmpDbContext(optionsBuilder.Options);
        }
    }
}

