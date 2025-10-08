using System;
using System.IO;

namespace YasGMP.Wpf.Smoke.Helpers;

internal static class RepositoryPaths
{
    private static readonly Lazy<string> RootLazy = new(() =>
    {
        var current = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(current);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "yasgmp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Unable to locate repository root (yasgmp.sln).");
    });

    public static string Root => RootLazy.Value;

    public static string ResolveWpfExecutable()
    {
        var root = Root;
        var exePath = Path.Combine(root, "YasGMP.Wpf", "bin", "Debug", "net9.0-windows", "YasGMP.Wpf.exe");
        return exePath;
    }
}
