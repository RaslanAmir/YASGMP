param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Command = @("dotnet", "run", "--project", "YasGMP.Wpf"),

    [string]$FixturePath,

    [string]$FixtureJson,

    [switch]$Verbose
)

if ($FixturePath -and $FixtureJson) {
    throw "Specify either -FixturePath or -FixtureJson, not both."
}

$resolvedCommand = if ($Command.Length -gt 0) {
    $Command
} else {
    @("dotnet", "run", "--project", "YasGMP.Wpf")
}

$envSnapshot = @{
    "YASGMP_WPF_TESTHOOKS"   = $env:YASGMP_WPF_TESTHOOKS
    "YASGMP_WPF_FIXTURE_PATH" = $env:YASGMP_WPF_FIXTURE_PATH
    "YASGMP_WPF_FIXTURE_JSON" = $env:YASGMP_WPF_FIXTURE_JSON
}

$env:YASGMP_WPF_TESTHOOKS = "1"

if ($FixturePath) {
    $resolvedPath = Resolve-Path -Path $FixturePath -ErrorAction Stop
    $env:YASGMP_WPF_FIXTURE_PATH = $resolvedPath.ProviderPath
    if ($Verbose) {
        Write-Host "[wpf-testhooks] Using fixture path: $($env:YASGMP_WPF_FIXTURE_PATH)"
    }
} elseif ($FixtureJson) {
    $env:YASGMP_WPF_FIXTURE_JSON = $FixtureJson
    if ($Verbose) {
        Write-Host "[wpf-testhooks] Using inline fixture JSON (${($FixtureJson.Length)} chars)"
    }
} else {
    Remove-Item Env:YASGMP_WPF_FIXTURE_PATH -ErrorAction SilentlyContinue | Out-Null
    Remove-Item Env:YASGMP_WPF_FIXTURE_JSON -ErrorAction SilentlyContinue | Out-Null
    if ($Verbose) {
        Write-Host "[wpf-testhooks] No fixture supplied; runtime will fall back to built-in defaults."
    }
}

try {
    if ($Verbose) {
        Write-Host "[wpf-testhooks] Launching: $($resolvedCommand -join ' ')"
    }

    & $resolvedCommand[0] @($resolvedCommand[1..($resolvedCommand.Length - 1)])
    $exitCode = if ($LASTEXITCODE -ne $null) { $LASTEXITCODE } else { 0 }
} finally {
    foreach ($key in $envSnapshot.Keys) {
        if ($envSnapshot[$key] -ne $null) {
            [Environment]::SetEnvironmentVariable($key, $envSnapshot[$key])
        } else {
            [Environment]::SetEnvironmentVariable($key, $null)
        }
    }
}

exit $exitCode
