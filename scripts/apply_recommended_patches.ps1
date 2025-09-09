param(
  [string]$SqlFile = "reports/recommended_patches.sql",
  [string]$Project = "tools/DbPatchApplier/DbPatchApplier.csproj",
  [string]$Config = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SqlFile)) { throw "SQL file not found: $SqlFile" }
if (-not (Test-Path $Project)) { throw "Project not found: $Project" }

Write-Host "Building DbPatchApplier..."
dotnet build $Project -c $Config | Out-Null

Write-Host "Applying patches from $SqlFile ..."
dotnet run --project $Project -c $Config -- $SqlFile

