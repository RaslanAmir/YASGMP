param(
  [switch]$Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Info($m){ if(-not $Quiet){ Write-Host $m } }

Info "Configuring Git hooks path to .githooks..."
git config core.hooksPath .githooks | Out-Null

if (Get-Command chmod -ErrorAction SilentlyContinue) {
  Info "Ensuring pre-commit hook is executable..."
  chmod +x .githooks/pre-commit | Out-Null
}

Info "Done. Pre-commit will run the DB analyzer and gate commits on mismatches."

