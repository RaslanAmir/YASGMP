using System.IO;
using System.Text;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests.TestDoubles;

internal sealed class StubCodeGeneratorService : ICodeGeneratorService
{
    public string? LastName { get; private set; }

    public string? LastManufacturer { get; private set; }

    public string GenerateMachineCode(string? name, string? manufacturer)
    {
        LastName = name;
        LastManufacturer = manufacturer;
        return $"{(name ?? "GEN")}-{(manufacturer ?? "CODE")}-UNIT";
    }

    public string GenerateMachineCode()
        => "GEN-CODE-UNIT";
}

internal sealed class StubQrCodeService : IQRCodeService
{
    public string? LastPayload { get; private set; }

    public int LastPixelSize { get; private set; }

    public Stream GeneratePng(string payload, int pixelSize = 20)
    {
        LastPayload = payload;
        LastPixelSize = pixelSize;
        return new MemoryStream(Encoding.UTF8.GetBytes(payload ?? string.Empty));
    }
}

internal sealed class StubPlatformService : IPlatformService
{
    public string GetLocalIpAddress() => "127.0.0.1";

    public string GetOsVersion() => "TestOS";

    public string GetHostName() => "UnitTestHost";

    public string GetUserName() => "UnitTester";

    public string GetAppDataDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "YasGMP", "Tests");
        Directory.CreateDirectory(path);
        return path;
    }
}
