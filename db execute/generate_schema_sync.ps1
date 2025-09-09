Param(
  [string]$ConnectionString = "Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;Allow User Variables=true;AllowPublicKeyRetrieval=True;SslMode=None;"
)

$ErrorActionPreference = 'Stop'

# Move to repo root (parent of this script folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
Set-Location $repoRoot

Write-Host "[SchemaSync] Using repo root: $repoRoot"
Write-Host "[SchemaSync] Target DB via YASGMP_MYSQL_CS"
$env:YASGMP_MYSQL_CS = $ConnectionString

# Ensure tool builds (Release)
dotnet build .\tools\SchemaSync\SchemaSync.csproj -c Release

$outFile = Join-Path $scriptDir '03_schema_sync.sql'
Write-Host "[SchemaSync] Generating: $outFile"

# Dry-run prints all ALTER TABLE ADD COLUMN statements needed
dotnet run --project .\tools\SchemaSync\SchemaSync.csproj | Out-File -FilePath $outFile -Encoding utf8

Write-Host "[SchemaSync] Done. Review and run: `n  mysql --protocol=tcp -h localhost -u root -p*** --default-character-set=utf8mb4 -D yasgmp < `"$outFile`""
