$ErrorActionPreference = 'Stop'

function Invoke-Dotnet {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )

    $dotnetPath = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if ($Args.Length -gt 0 -and $Args[0] -eq '--info') {
        return (& $dotnetPath @Args | Out-String)
    }

    & $dotnetPath @Args
}

Set-Alias -Name dotnet -Value Invoke-Dotnet
$env:PATH = "${PWD}\tools;$env:PATH"

Start-Transcript -Path 'logs\bootstrap-dotnet9-transcript.txt' -Force
try {
    .\scripts\bootstrap-dotnet9.ps1
}
finally {
    Stop-Transcript | Out-Null
}
