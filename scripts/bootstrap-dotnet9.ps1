$ErrorActionPreference = 'Stop'

Write-Host "=== Checking dotnet SDK (expect 9.x) ==="
$info = (& dotnet --info) 2>$null
if (-not $info -or ($info -notmatch 'Version:\s+9\.\d+\.\d+')) {
  Write-Error "dotnet SDK 9.x not detected. Install via 'winget install Microsoft.DotNet.SDK.9' and re-run."
}

Write-Host "=== Verifying Windows SDK 10.0.19041+ present (Visual Studio Installer recommended) ==="
Write-Host "If missing, open VS Installer -> add .NET Desktop Development + Windows 10 SDK 10.0.19041+."

Write-Host "=== Pinning SDK to 9.0 via global.json ==="
@'
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
'@ | Set-Content -Encoding UTF8 "$PSScriptRoot\..\global.json"

Write-Host "=== Ensuring nuget.org feed is enabled ==="
nuget sources Add -Name nuget.org -Source https://api.nuget.org/v3/index.json -NonInteractive 2>$null | Out-Null
nuget sources Enable -Name nuget.org -NonInteractive 2>$null | Out-Null

Write-Host "=== Restore & build solution (MAUI + WPF) ==="
Set-Location "$PSScriptRoot\.."
dotnet restore
dotnet build -c Debug

Write-Host "=== Seeding test fixture ==="
New-Item -ItemType Directory -Force -Path ".\tests\fixtures" | Out-Null
"hello" | Set-Content -Encoding UTF8 ".\tests\fixtures\hello.txt"

Write-Host "=== Done. You can now tell Codex to start Batch 0. ==="
