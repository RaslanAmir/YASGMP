using System.Reflection;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

[assembly: AssemblyCompany("YasGMP.AppCore")]
[assembly: AssemblyProduct("YasGMP.AppCore")]
[assembly: AssemblyTitle("YasGMP.AppCore")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("YasGMP")]
[assembly: InternalsVisibleTo("YasGMP.Wpf")]
[assembly: InternalsVisibleTo("YasGMP.Tests")]
#if NET9_0
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName=".NET 9.0")]
#elif NET8_0
[assembly: TargetFramework(".NETCoreApp,Version=v8.0", FrameworkDisplayName=".NET 8.0")]
#endif
