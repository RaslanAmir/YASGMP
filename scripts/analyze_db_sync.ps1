param(
  [string]$SchemaJson = "tools/schema/snapshots/schema.json",
  [string]$CodeRoot = ".",
  [string]$ReportsDir = "reports"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-Dir {
  param([string]$Path)
  if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path | Out-Null }
}

Ensure-Dir -Path $ReportsDir

# 1) Load DB schema (table -> columns)
if (-not (Test-Path $SchemaJson)) { throw "Schema JSON not found: $SchemaJson" }
$schema = Get-Content $SchemaJson -Raw | ConvertFrom-Json
$tables = @{}

# Support multiple snapshot formats:
#  - Old:   $schema.Tables = array of { Name, Columns: [{ Name, ... }] }
#  - New:   $schema.tables = object map { tableName: { columns: [{ COLUMN_NAME, ... }], ... } }

# Decide schema shape deterministically
$hasLower = ($schema.PSObject.Properties.Name -contains 'tables')
$hasUpper = ($schema.PSObject.Properties.Name -contains 'Tables')
$parsed = $false

if ($hasLower) {
  $tmap = $schema.PSObject.Properties['tables'].Value
  foreach ($prop in $tmap.PSObject.Properties) {
    $tname = $prop.Name
    $tobj = $prop.Value
    $cols = @{}
    if ($tobj -and ($tobj.PSObject.Properties.Name -contains 'columns')) {
      foreach ($c in $tobj.columns) {
        $colName = $null
        if ($c -and ($c.PSObject.Properties.Name -contains 'Name')) { $colName = $c.Name }
        elseif ($c -and ($c.PSObject.Properties.Name -contains 'COLUMN_NAME')) { $colName = $c.COLUMN_NAME }
        if ($colName) { $cols[$colName.ToLowerInvariant()] = $true }
      }
    }
    $tables[$tname.ToLowerInvariant()] = [pscustomobject]@{ Name = $tname; Columns = $cols }
  }
  $parsed = $true
}
elseif ($hasUpper) {
  # If 'Tables' is an array of typed objects
  if ($schema.Tables -is [System.Array]) {
    foreach ($t in $schema.Tables) {
      $cols = @{}
      $colArr = $null
      if ($t.PSObject.Properties.Name -contains 'Columns') { $colArr = $t.Columns } elseif ($t.PSObject.Properties.Name -contains 'columns') { $colArr = $t.columns }
      foreach ($c in $colArr) {
        $colName = $null
        if ($c.PSObject.Properties.Name -contains 'Name') { $colName = $c.Name }
        elseif ($c.PSObject.Properties.Name -contains 'COLUMN_NAME') { $colName = $c.COLUMN_NAME }
        if ($colName) { $cols[$colName.ToLowerInvariant()] = $true }
      }
      $tNameVal = if ($t.PSObject.Properties.Name -contains 'Name') { $t.Name } else { $t.Table }
      if (-not $tNameVal) { $tNameVal = $t.ToString() }
      $tables[$tNameVal.ToLowerInvariant()] = [pscustomobject]@{ Name = $tNameVal; Columns = $cols }
    }
    $parsed = $true
  } else {
    # Treat as map under 'Tables'
    $tmap = $schema.PSObject.Properties['Tables'].Value
    foreach ($prop in $tmap.PSObject.Properties) {
      $tname = $prop.Name
      $tobj = $prop.Value
      $cols = @{}
      if ($tobj -and ($tobj.PSObject.Properties.Name -contains 'columns')) {
        foreach ($c in $tobj.columns) {
          $colName = $null
          if ($c -and ($c.PSObject.Properties.Name -contains 'Name')) { $colName = $c.Name }
          elseif ($c -and ($c.PSObject.Properties.Name -contains 'COLUMN_NAME')) { $colName = $c.COLUMN_NAME }
          if ($colName) { $cols[$colName.ToLowerInvariant()] = $true }
        }
      }
      $tables[$tname.ToLowerInvariant()] = [pscustomobject]@{ Name = $tname; Columns = $cols }
    }
    $parsed = $true
  }
}
if (-not $parsed) { throw "Unrecognized schema JSON format: expected 'Tables' array or 'tables' map." }

# 1b) Include view names as schema-known (and derive columns when possible)
$viewsPath = Join-Path (Split-Path $SchemaJson) 'views.sql'
if (Test-Path $viewsPath) {
  $viewsText = Get-Content $viewsPath -Raw
  $rxViewWithSelect = [regex]'(?is)CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+[`"\[]?(\w+)[`"\]]?\s+AS\s+SELECT\s+(.+?)\s+FROM\s+'
  $rxViewName = [regex]'(?is)CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+[`"\[]?(\w+)[`"\]]?\b'
  $mAll = $rxViewWithSelect.Matches($viewsText)
  foreach ($m in $mAll) {
    $vname = $m.Groups[1].Value
    $sel = $m.Groups[2].Value
    $vcolsArr = @(Parse-ColumnsList $sel)
    $vcols = @{}
    foreach ($cc in $vcolsArr) { if (-not $vcols.ContainsKey($cc)) { $vcols[$cc] = $true } }
    $tables[$vname.ToLowerInvariant()] = [pscustomobject]@{ Name = $vname; Columns = $vcols }
  }
  # Add any remaining view names without parsed columns
  $mNames = $rxViewName.Matches($viewsText)
  foreach ($mn in $mNames) {
    $vname = $mn.Groups[1].Value
    if (-not $tables.ContainsKey($vname.ToLowerInvariant())) {
      $tables[$vname.ToLowerInvariant()] = [pscustomobject]@{ Name = $vname; Columns = @{} }
    }
  }
}

# 2) Inventory code files
$modelFiles      = Get-ChildItem -Recurse "$CodeRoot/Models" -Include *.cs -File -ErrorAction SilentlyContinue
$serviceFiles    = Get-ChildItem -Recurse "$CodeRoot/Services" -Include *.cs -File -ErrorAction SilentlyContinue
$viewModelFiles  = Get-ChildItem -Recurse "$CodeRoot/ViewModels" -Include *.cs -File -ErrorAction SilentlyContinue
$viewXamlFiles   = Get-ChildItem -Recurse "$CodeRoot/Views" -Include *.xaml -File -ErrorAction SilentlyContinue
$viewCodeFiles   = Get-ChildItem -Recurse "$CodeRoot/Views" -Include *.cs -File -ErrorAction SilentlyContinue

$invModels = $modelFiles | ForEach-Object {
  [pscustomobject]@{ Kind = 'Model'; Name = $_.BaseName; File = $_.FullName }
}
$invServices = $serviceFiles | ForEach-Object { [pscustomobject]@{ Kind = 'Service'; Name = $_.BaseName; File = $_.FullName } }
$invVMs = $viewModelFiles | ForEach-Object { [pscustomobject]@{ Kind = 'ViewModel'; Name = $_.BaseName; File = $_.FullName } }
$invViews = @()
$invViews += $viewXamlFiles  | ForEach-Object { [pscustomobject]@{ Kind = 'ViewXaml'; Name = $_.BaseName; File = $_.FullName } }
$invViews += $viewCodeFiles  | ForEach-Object { [pscustomobject]@{ Kind = 'ViewCode'; Name = $_.BaseName; File = $_.FullName } }

function Export-CsvSafe {
  param([object[]]$Rows, [string]$Path)
  if ($Rows -and $Rows.Count -gt 0) {
    $Rows | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $Path
  } else { "" | Out-File -Encoding utf8 -FilePath $Path }
}

Export-CsvSafe -Rows $invModels   -Path "$ReportsDir/inventory_models.csv"
Export-CsvSafe -Rows $invServices -Path "$ReportsDir/inventory_services.csv"
Export-CsvSafe -Rows $invVMs      -Path "$ReportsDir/inventory_viewmodels.csv"
Export-CsvSafe -Rows $invViews    -Path "$ReportsDir/inventory_views.csv"

# 3) Extract SQL statements from code (.cs across the project, excluding bin/obj)
$codeFiles = Get-ChildItem -Recurse $CodeRoot -Include *.cs -File -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch "[\\/]bin[\\/]|[\\/]obj[\\/]" }

# Also parse project SQL files for effective usage
$sqlFiles = @()
if (Test-Path "$CodeRoot/db execute") { $sqlFiles += Get-ChildItem -Recurse "$CodeRoot/db execute" -Include *.sql -File -ErrorAction SilentlyContinue }
if (Test-Path "$CodeRoot/scripts")   { $sqlFiles += Get-ChildItem -Recurse "$CodeRoot/scripts" -Include *.sql -File -ErrorAction SilentlyContinue }

# Regex helpers
$rxSqlStart = [regex]'(?is)\b(SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM|CREATE\s+VIEW|REPLACE\s+INTO)\b'
$rxInsert   = [regex]'(?is)\bINSERT\s+INTO\s+([`"\[]?\w+[`"\]]?)\s*\(([^\)]+)\)'
$rxUpdate   = [regex]'(?is)\bUPDATE\s+([`"\[]?\w+[`"\]]?)\s+SET\s+(.+?)\s+WHERE\b'
$rxDelete   = [regex]'(?is)\bDELETE\s+FROM\s+([`"\[]?\w+[`"\]]?)\b'
$rxSelect   = [regex]'(?is)\bSELECT\s+(.+?)\s+FROM\s+([`"\[]?\w+[`"\]]?)\b'
$rxJoin     = [regex]'(?is)\bJOIN\s+([`"\[]?\w+[`"\]]?)\b'

# FROM/JOIN alias mapping: captures table and optional alias
$rxFromOrJoinAlias = [regex]'(?is)\b(?:FROM|JOIN)\s+([`"\[]?\w+[`"\]]?)(?:\s+(?:AS\s+)?(\w+))?'

function Normalize-Table {
  param([string]$t)
  if (-not $t) { return $t }
  $t = $t.Trim()
  $t = $t.Trim('`','"','[',']')
  return $t
}

function Parse-ColumnsFromSet {
  param([string]$setClause)
  $cols = @()
  if (-not $setClause) { return $cols }
  # split on commas not inside parentheses (naive)
  $parts = $setClause -split ','
  foreach ($p in $parts) {
    $m = [regex]::Match($p, '(?is)\s*([`"\[]?\w+[`"\]]?)\s*=')
    if ($m.Success) { $cols += (Normalize-Table $m.Groups[1].Value).ToLowerInvariant() }
  }
  return $cols | Where-Object { $_ -ne '' } | Select-Object -Unique
}

# Parse columns preserving optional qualifier alias (e.g., t.col)
function Parse-ColumnsListWithAlias {
  param([string]$list)
  $items = @()
  if (-not $list) { return $items }
  $parts = $list -split ','
  foreach ($p in $parts) {
    $raw = $p.Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) { continue }
    $raw2 = $raw
    if ($raw2 -match '(?i)\bAS\b') { $raw2 = ($raw2 -split '(?i)\bAS\b')[0].Trim() }
    if ($raw2 -match '^[\(]') { continue }
    if ($raw2 -match '^@') { continue }
    if ($raw2.Contains("'") -or $raw2.Contains('"')) { continue }
    $alias = $null; $colName = $null
    $m = [regex]::Match($raw2, '^(?<q>\w+)\.(?<c>\*|\w+)$')
    if ($m.Success) {
      $alias = $m.Groups['q'].Value
      $colName = $m.Groups['c'].Value
    } else {
      $colName = (Normalize-Table $raw2)
      if ($colName -match '^\w+\.\w+$') { $colName = ($colName -split '\.')[1] }
    }
    if ($colName -and $colName -ne '') {
      $items += [pscustomobject]@{ Alias=$alias; Column=$colName.ToLowerInvariant() }
    }
  }
  return $items
}

function Parse-ColumnsList {
  param([string]$list)
  $cols = @()
  if (-not $list) { return $cols }
  $parts = $list -split ','
  foreach ($p in $parts) {
    $c = (Normalize-Table $p)
    # ignore functions, placeholders, params, string literals
    if ($c -match '^(SELECT|VALUES|CURRENT_\w+|\d+)$') { continue }
    if ($c -match "^@") { continue }
    if ($c -match "^['""]") { continue }
    if ($c -match "[\(\)]") { continue }
    # remove alias like 'col AS alias'
    if ($c -match '(?i)\bAS\b') { $c = ($c -split '(?i)\bAS\b')[0].Trim() }
    # remove table prefixes like t.col
    if ($c -match '^\w+\.\w+$') { $c = ($c -split '\.')[1] }
    $c = $c.Trim()
    if ($c -eq '*') { $cols += '*' } else { $cols += $c.ToLowerInvariant() }
  }
  return $cols | Where-Object { $_ -ne '' } | Select-Object -Unique
}

$statements = @()
function IsReservedToken([string]$t) { $r=@('and','or','on','as','case','when','then','else','end','values','set','into','left','right','inner','outer','where','group','order','limit','having','join','from','union','select','update','insert','delete','create','replace'); return ($r -contains $t) }
function IsValidTableName([string]$t) { return (($t -match '^[a-z][a-z0-9_]*$') -and -not (IsReservedToken $t)) }
$countCode = 0; $countSql = 0; $countReport = 0; $totalStrings=0;

function Add-StatementFromSql {
  param([string]$sql,[string]$sourceFile)
  if (-not ($sql -match $rxSqlStart)) { return }
  if ($sql -match '(?i)ANALYZER_IGNORE') { return }
  if ($sourceFile -match '(?i)YASGMP\.sql$') { return }
  if ($sourceFile -match '(?i)tools[\\/]+schema[\\/]+snapshots') { return }

  $tablesUsed = New-Object System.Collections.Generic.HashSet[string]
  $columnsUsed = New-Object System.Collections.Generic.HashSet[string]
  $op = ''
  $primaryTable = $null
  $aliasedCols = $null

  if ($sql -match $rxInsert) {
    $op = 'INSERT'
    $table = Normalize-Table $matches[1]
    $tnrm = $table.ToLowerInvariant()
    if (IsValidTableName $tnrm) { $null = $tablesUsed.Add($tnrm) }
    if (-not $primaryTable) { $primaryTable = $table.ToLowerInvariant() }
    $cols = Parse-ColumnsList $matches[2]
    foreach ($c in $cols) { $null = $columnsUsed.Add($c) }
  }
  if ($sql -match $rxUpdate) {
    $op = if ($op) { $op + '+UPDATE' } else { 'UPDATE' }
    $table = Normalize-Table $matches[1]
    $tnrm = $table.ToLowerInvariant()
    if (IsValidTableName $tnrm) { $null = $tablesUsed.Add($tnrm) }
    if (-not $primaryTable) { $primaryTable = $table.ToLowerInvariant() }
    $cols = Parse-ColumnsFromSet $matches[2]
    foreach ($c in $cols) { $null = $columnsUsed.Add($c) }
  }
  if ($sql -match $rxDelete) {
    $op = if ($op) { $op + '+DELETE' } else { 'DELETE' }
    $table = Normalize-Table $matches[1]
    $tnrm = $table.ToLowerInvariant()
    if (IsValidTableName $tnrm) { $null = $tablesUsed.Add($tnrm) }
    if (-not $primaryTable) { $primaryTable = $table.ToLowerInvariant() }
  }
  if ($sql -match $rxSelect) {
    $op = if ($op) { $op + '+SELECT' } else { 'SELECT' }
    $colsWithAlias = Parse-ColumnsListWithAlias $matches[1]
    foreach ($ci in $colsWithAlias) { $null = $columnsUsed.Add($ci.Column) }
    $table = Normalize-Table $matches[2]
    $tnrm = $table.ToLowerInvariant()
    if (IsValidTableName $tnrm) { $null = $tablesUsed.Add($tnrm) }
    if (-not $primaryTable) { $primaryTable = $table.ToLowerInvariant() }
    # Build alias map
    $aliasMap = @{}
    $fm = $rxFromOrJoinAlias.Matches($sql)
    foreach ($am in $fm) {
      $tt = (Normalize-Table $am.Groups[1].Value).ToLowerInvariant()
      $al = $am.Groups[2].Value
      if ($tt -and (IsValidTableName $tt)) { $null = $tablesUsed.Add($tt) }
      if ($al -and (IsValidTableName $tt)) { $aliasMap[$al.ToLowerInvariant()] = $tt }
    }
    # Resolve alias-qualified columns
    $aliasedCols = @{}
    foreach ($ci in $colsWithAlias) {
      if ($ci.Alias -and $aliasMap.ContainsKey($ci.Alias.ToLowerInvariant())) {
        $tn = $aliasMap[$ci.Alias.ToLowerInvariant()]
        if ($tn -and (IsValidTableName $tn)) {
          if (-not $aliasedCols.ContainsKey($tn)) { $aliasedCols[$tn] = New-Object System.Collections.Generic.HashSet[string] }
          $null = $aliasedCols[$tn].Add($ci.Column)
        }
      }
    }
    # Include JOIN tables
    $joins = $rxJoin.Matches($sql)
    foreach ($j in $joins) { $jt = (Normalize-Table $j.Groups[1].Value).ToLowerInvariant(); if (IsValidTableName $jt) { $null = $tablesUsed.Add($jt) } }
  }

  if ($tablesUsed.Count -gt 0) {
    $tJoin = ($tablesUsed | ForEach-Object { $_ } | Sort-Object -Unique) -join ';'
    $cJoin = ($columnsUsed | ForEach-Object { $_ } | Sort-Object -Unique) -join ';'
    $script:statements += [pscustomobject]@{
      File = $sourceFile
      Operation = $op
      Tables = $tJoin
      Columns = $cJoin
      PrimaryTable = $primaryTable
      AliasedColumns = $aliasedCols
      Sql = ($sql -replace "\s+", ' ').Trim()
    }
  }
}
foreach ($f in $codeFiles) {
  $text = $null
  try { $text = Get-Content $f.FullName -Raw } catch { continue }
  if ($null -eq $text) { continue }
  # Extract string literals (normal and verbatim) that likely contain SQL
  $strings = @()
  # verbatim @"..."
  $v = [regex]::Matches($text, '(?s)@"(.*?)"')
  foreach ($m in $v) { $strings += $m.Groups[1].Value }
  # normal "..." (skip XML/XAML)
  $n = [regex]::Matches($text, '"([^"\\]*(?:\\.[^"\\]*)*)"')
  foreach ($m in $n) {
    $val = $m.Groups[1].Value
    if ($val -match $rxSqlStart) { $strings += $val }
  }
  $totalStrings += ($strings | Measure-Object).Count
  foreach ($s in $strings) {
    Add-StatementFromSql -sql $s -sourceFile $f.FullName
  }
}
$countCode = ($statements | Measure-Object).Count

# Include SQL files (opt-in via env var INCLUDE_SQL_FILES=1)
$countSql = 0
if ($env:INCLUDE_SQL_FILES -eq '1') {
  foreach ($sf in $sqlFiles) {
    $text = Get-Content $sf.FullName -Raw
    # Split roughly by semicolons to catch multiple statements
    $chunks = $text -split ';'
    foreach ($chunk in $chunks) {
      $s = $chunk.Trim()
      if ([string]::IsNullOrWhiteSpace($s)) { continue }
      Add-StatementFromSql -sql $s -sourceFile $sf.FullName
    }
  }
  $countSql = (($statements | Measure-Object).Count) - $countCode
}

# Include pre-scanned report lines if explicitly enabled via env var
if ($env:INCLUDE_PRE_SCANS -eq '1') {
  $scanFiles = @(
    (Join-Path $ReportsDir 'sql_scan_from_join_into_update.txt'),
    (Join-Path $ReportsDir 'sql_scan_selectstar_insert_update_delete.txt')
  )
  foreach ($rf in $scanFiles) {
    if (-not (Test-Path $rf)) { continue }
    Get-Content $rf | ForEach-Object {
      $line = $_
      if (-not ($line -match $rxSqlStart)) { return }
      $fileTag = $rf
      $content = $line
      $m = [regex]::Match($line, '^(?<file>[^:]+):(?<line>\d+):(?<rest>.*)$')
      if ($m.Success) { $fileTag = $m.Groups['file'].Value; $content = $m.Groups['rest'].Value }
      Add-StatementFromSql -sql $content -sourceFile $fileTag
    }
  }
}
$countReport = (($statements | Measure-Object).Count) - $countCode - $countSql

function Export-CsvSafe2 { param([object[]]$Rows,[string]$Path) if($Rows){ $Rows | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $Path } else { "" | Out-File -Encoding utf8 -FilePath $Path }}
Export-CsvSafe2 -Rows $statements -Path "$ReportsDir/code_statements.csv"

# 4) Aggregate per-table usage and detect mismatches
$usage = @{}
function IsReservedColumnToken([string]$c) { $r=@('null','now','count','sum','avg','min','max','current_timestamp','current_date','current_time'); return ($r -contains $c) }
function IsValidColumnName([string]$c) { return (($c -match '^[a-z][a-z0-9_]*$') -and -not (IsReservedColumnToken $c)) }
foreach ($s in $statements) {
  $tabs = @()
  if ($s.Tables) { $tabs = $s.Tables -split ';' }
  foreach ($t in $tabs) {
    if (-not $t) { continue }
    $tn = $t.ToLowerInvariant()
    if (-not $usage.ContainsKey($tn)) {
      $usage[$tn] = [pscustomobject]@{ Table=$tn; Select=0; Insert=0; Update=0; Delete=0; Columns=@{} }
    }
    $u = $usage[$tn]
    if ($s.Operation -match 'SELECT') { $u.Select = $u.Select + 1 }
    if ($s.Operation -match 'INSERT') { $u.Insert = $u.Insert + 1 }
    if ($s.Operation -match 'UPDATE') { $u.Update = $u.Update + 1 }
    if ($s.Operation -match 'DELETE') { $u.Delete = $u.Delete + 1 }
    # Assign columns using alias resolution when available; else primary-table fallback
    $assigned = $false
    if ($s.PSObject.Properties.Match('AliasedColumns').Count -gt 0 -and $s.AliasedColumns -and $s.AliasedColumns.ContainsKey($tn)) {
      $assigned = $true
      foreach ($c in $s.AliasedColumns[$tn]) {
        if (-not (IsValidColumnName $c)) { continue }
        if (-not $u.Columns.ContainsKey($c)) { $u.Columns[$c] = 0 }
        $u.Columns[$c] = $u.Columns[$c] + 1
      }
    }
    if (-not $assigned -and $s.Columns -and $s.PrimaryTable -and ($tn -eq $s.PrimaryTable)) {
      foreach ($c in ($s.Columns -split ';')) {
        if (-not (IsValidColumnName $c)) { continue }
        if (-not $u.Columns.ContainsKey($c)) { $u.Columns[$c] = 0 }
        $u.Columns[$c] = $u.Columns[$c] + 1
      }
    }
  }
}

$usageRows = @()
foreach ($k in $usage.Keys) {
  $u = $usage[$k]
  $cols = ($u.Columns.Keys -join ';')
  $usageRows += [pscustomobject]@{ Table=$u.Table; Select=$u.Select; Insert=$u.Insert; Update=$u.Update; Delete=$u.Delete; Columns=$cols }
}
Export-CsvSafe2 -Rows $usageRows -Path "$ReportsDir/code_table_usage.csv"

# 5) Mismatch detection
$mismatches = @()
$ignoreTables = @('information_schema','mysql','performance_schema','sys')

# Allowlist support (JSON): tools/schema/allowlist.json
$allowTables = @()
$allowCols = @{}
$allowFile = Join-Path 'tools/schema' 'allowlist.json'
if (Test-Path $allowFile) {
  try {
    $allow = Get-Content $allowFile -Raw | ConvertFrom-Json
    if ($allow.IgnoreTables) { $allowTables = @($allow.IgnoreTables | ForEach-Object { $_.ToString().ToLowerInvariant() } | Select-Object -Unique) }
    if ($allow.IgnoreColumns) {
      foreach ($k in $allow.IgnoreColumns.PSObject.Properties.Name) {
        $t = $k.ToString().ToLowerInvariant()
        $vals = @($allow.IgnoreColumns.$k | ForEach-Object { $_.ToString().ToLowerInvariant() } | Select-Object -Unique)
        $allowCols[$t] = $vals
      }
    }
  } catch { }
}

foreach ($u in $usageRows) {
  $tn = $u.Table
  if ($ignoreTables -contains $tn) { continue }
  if ($allowTables -contains $tn) { continue }
  $hasTable = $tables.ContainsKey($tn)
  if (-not $hasTable) {
    $mismatches += [pscustomobject]@{
      Type = 'TABLE_NOT_IN_SCHEMA'
      Table = $tn
      Column = ''
      Evidence = "code references table '$tn'"
      Files = ($statements | Where-Object { ( $_.Tables -split ';') -contains $tn } | Select-Object -Expand File -Unique) -join ';'
    }
    continue
  }
  $schemaCols = $tables[$tn].Columns
  $colsUsed = @()
  if ($u.Columns) { $colsUsed = $u.Columns -split ';' }

  foreach ($c in $colsUsed) {
    if (-not $c -or $c -eq '*') { continue }
    # allowlist per-table columns
    if ($allowCols.ContainsKey($tn) -and ($allowCols[$tn] -contains $c)) { continue }
    if (-not $schemaCols.ContainsKey($c)) {
      $mismatches += [pscustomobject]@{
        Type = 'COLUMN_NOT_IN_SCHEMA'
        Table = $tn
        Column = $c
        Evidence = "code uses column '$c' on table '$tn'"
        Files = ($statements | Where-Object { ( $_.Tables -split ';') -contains $tn -and (($_.Columns -split ';') -contains $c) } | Select-Object -Expand File -Unique) -join ';'
      }
    }
  }

  # soft-delete expectations
  $softCols = @('is_deleted','deleted_at')
  foreach ($sc in $softCols) {
    $usesSoft = $colsUsed -contains $sc
    if ($usesSoft -and -not $schemaCols.ContainsKey($sc)) {
      $mismatches += [pscustomobject]@{
        Type = 'SOFT_DELETE_COLUMN_MISSING'
        Table = $tn
        Column = $sc
        Evidence = "code uses soft-delete column '$sc' but schema lacks it"
        Files = ($statements | Where-Object { ( $_.Tables -split ';') -contains $tn -and (($_.Columns -split ';') -contains $sc) } | Select-Object -Expand File -Unique) -join ';'
      }
    }
  }
}

Export-CsvSafe2 -Rows $mismatches -Path "$ReportsDir/mismatch-matrix.csv"

# 6) Per-table diffs and recommended SQL patches

function Ensure-ReportsSubDir([string]$name) {
  $p = Join-Path $ReportsDir $name
  if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null }
  return $p
}

$diffDir = Ensure-ReportsSubDir 'diffs'
$byTableDir = Ensure-ReportsSubDir 'diffs/by-table'

function Guess-ColumnType([string]$col) {
  $c = $col.ToLowerInvariant()
  if ($c -eq 'id' -or $c -like '*_id') { return 'INT' }
  if ($c -like '*_count' -or $c -like '*_score' -or $c -like '*_level' -or $c -like '*_ms') { return 'INT' }
  if ($c -like 'is_*' -or $c -like 'has_*' -or $c -eq 'deleted' -or $c -eq 'success' -or $c -eq 'active') { return 'TINYINT(1)' }
  if ($c -like '*_at' -or $c -like '*_time' -or $c -like '*_timestamp') { return 'DATETIME' }
  if ($c -like '*_date') { return 'DATE' }
  if ($c -like '*_price' -or $c -like '*_amount' -or $c -like '*_total') { return 'DECIMAL(18,2)' }
  if ($c -eq 'code' -or $c -eq 'name' -or $c -eq 'status' -or $c -eq 'type' -or $c -eq 'note' -or $c -like '*_by' -or $c -like '*_ip') { return 'VARCHAR(255)' }
  return 'VARCHAR(255)'
}

$patchLines = New-Object System.Collections.Generic.List[string]
$tableDiffRows = @()

foreach ($u in $usageRows) {
  $tn = $u.Table
  if ($ignoreTables -contains $tn) { continue }
  $usedCols = @()
  if ($usage.ContainsKey($tn)) { $usedCols = $usage[$tn].Columns.Keys }
  $safeUsed = $usedCols | Where-Object { IsValidColumnName $_ }

  if (-not $tables.ContainsKey($tn)) {
    # Missing table → propose CREATE TABLE with guessed columns
    $softIs = ($safeUsed -contains 'is_deleted')
    $softAt = ($safeUsed -contains 'deleted_at')
    $ddl = @()
    $ddl += ('-- Suggested CREATE TABLE for missing table `{0}`' -f $tn)
    $colLines = @()
    $colLines += '  `id` INT NOT NULL AUTO_INCREMENT'
    foreach ($c in $safeUsed) {
      if ($c -eq 'id') { continue }
      $colLines += ("  `{0}` {1} NULL" -f $c, (Guess-ColumnType $c))
    }
    if ($softIs -and -not ($safeUsed -contains 'is_deleted')) { $colLines += '  `is_deleted` TINYINT(1) DEFAULT 0' }
    if ($softAt -and -not ($safeUsed -contains 'deleted_at')) { $colLines += '  `deleted_at` DATETIME NULL' }

    $ddl += ('CREATE TABLE `{0}` (' -f $tn)
    if ($colLines.Count -gt 0) {
      $ddl += ($colLines -join ("," + [Environment]::NewLine))
      $ddl += ("," + [Environment]::NewLine + '  PRIMARY KEY (`id`)')
    } else {
      $ddl += '  PRIMARY KEY (`id`)'
    }
    $ddl += ') ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;'
    $patchLines.AddRange([string[]]$ddl)

    # Per-table diff markdown
    $md = @()
    $md += ('# Table `{0}`' -f $tn)
    $md += "Status: MISSING in schema.json"
    $md += "Used columns in code: " + (($safeUsed | Sort-Object) -join ', ')
    $md += "\nSuggested DDL:"
    $md += '```sql'
    $md += ($ddl -join [Environment]::NewLine)
    $md += '```'
    Set-Content -Path (Join-Path $byTableDir "$tn.md") -Value ($md -join [Environment]::NewLine) -Encoding UTF8

    $tableDiffRows += [pscustomobject]@{ Table=$tn; Status='MISSING_TABLE'; MissingColumns=($safeUsed -join ';'); ExtraColumns=''; UsedColumnsCount=($safeUsed | Measure-Object).Count }
    continue
  }

  # Existing table – compute column diffs and ALTER suggestions
  $schemaCols = $tables[$tn].Columns.Keys
  $missingCols = @($safeUsed | Where-Object { -not ($schemaCols -contains $_) })
  $extraCols = @($schemaCols | Where-Object { -not ($safeUsed -contains $_) })

  $alters = @()
  foreach ($mc in $missingCols) {
    $alters += ("ALTER TABLE `{0}` ADD COLUMN `{1}` {2} NULL;" -f $tn, $mc, (Guess-ColumnType $mc))
  }
  if ($alters.Count -gt 0) {
    $patchLines.Add(('-- Missing columns for `{0}`' -f $tn))
    $patchLines.AddRange([string[]]$alters)
  }

  $md = @()
  $md += ('# Table `{0}`' -f $tn)
  $md += "Schema columns: " + (($schemaCols | Sort-Object) -join ', ')
  $md += "Code-used columns: " + (($safeUsed | Sort-Object) -join ', ')
  $md += "Missing in schema: " + (($missingCols | Sort-Object) -join ', ')
  $md += "Extra in schema (unused by code): " + (($extraCols | Sort-Object) -join ', ')
  if ($alters.Count -gt 0) {
    $md += "\nSuggested ALTERs:"
    $md += '```sql'
    foreach ($a in $alters) { $md += $a }
    $md += '```'
  }
  $mdPath = (Join-Path $byTableDir "$tn.md")
  1..3 | ForEach-Object {
    try { Set-Content -Path $mdPath -Value ($md -join [Environment]::NewLine) -Encoding UTF8; throw [System.Exception]::new('ok') } catch { if ($_.Exception.Message -eq 'ok') { return } Start-Sleep -Milliseconds 100 }
  }
  $tableDiffRows += [pscustomobject]@{ Table=$tn; Status='EXISTS'; MissingColumns=($missingCols -join ';'); ExtraColumns=($extraCols -join ';'); UsedColumnsCount=($safeUsed | Measure-Object).Count }
}

Export-CsvSafe2 -Rows $tableDiffRows -Path (Join-Path $ReportsDir 'table_diffs.csv')
if ($patchLines.Count -gt 0) {
  $header = @(
    "-- Recommended SQL patches generated $(Get-Date -Format o)",
    "-- Review carefully before applying to production."
  )
  $content = @($header + '' + ($patchLines.ToArray()))
  Set-Content -Path (Join-Path $ReportsDir 'recommended_patches.sql') -Value ($content -join [Environment]::NewLine) -Encoding UTF8
}

# 7) Suggestions based on heuristics (pluralization, normalization, Levenshtein)

function Normalize-Name([string]$n) { return ($n -replace '[`"\[\]_]', '').ToLowerInvariant() }

$schemaByNorm = @{}
foreach ($k in $tables.Keys) {
  $norm = Normalize-Name $k
  if (-not $schemaByNorm.ContainsKey($norm)) { $schemaByNorm[$norm] = @() }
  $schemaByNorm[$norm] += $k
}

function Get-Variants([string]$name) {
  $v = New-Object System.Collections.Generic.HashSet[string]
  $null = $v.Add($name)
  $n = $name
  if ($n.EndsWith('ies')) { $null = $v.Add(($n.Substring(0,$n.Length-3) + 'y')) }
  if ($n.EndsWith('es'))  { $null = $v.Add($n.Substring(0,$n.Length-2)) }
  if ($n.EndsWith('s'))   { $null = $v.Add($n.Substring(0,$n.Length-1)) }
  $null = $v.Add($n + 's')
  $null = $v.Add($n + 'es')
  if ($n.EndsWith('y'))   { $null = $v.Add(($n.Substring(0,$n.Length-1) + 'ies')) }
  return $v
}

function Get-LevenshteinDistance {
  param([string]$a,[string]$b)
  if ($null -eq $a) { $a = '' }
  if ($null -eq $b) { $b = '' }
  $n = $a.Length; $m = $b.Length
  $v0 = New-Object 'int[]' ($m+1)
  $v1 = New-Object 'int[]' ($m+1)
  for ($j=0; $j -le $m; $j++) { $v0[$j] = $j }
  for ($i=0; $i -lt $n; $i++) {
    $v1[0] = $i + 1
    for ($j=0; $j -lt $m; $j++) {
      $cost = if ($a[$i] -eq $b[$j]) { 0 } else { 1 }
      $del = $v0[$j+1] + 1
      $ins = $v1[$j] + 1
      $sub = $v0[$j] + $cost
      $v1[$j+1] = [Math]::Min([Math]::Min($del,$ins),$sub)
    }
    for ($j=0; $j -le $m; $j++) { $v0[$j] = $v1[$j] }
  }
  return $v0[$m]
}

function Suggest-SimilarTable([string]$name) {
  $norm = Normalize-Name $name
  if ($schemaByNorm.ContainsKey($norm)) { return ($schemaByNorm[$norm] | Select-Object -First 1) }
  $vars = Get-Variants $name
  foreach ($v in $vars) {
    $nv = Normalize-Name $v
    if ($schemaByNorm.ContainsKey($nv)) { return ($schemaByNorm[$nv] | Select-Object -First 1) }
  }
  # substring heuristic
  foreach ($key in $schemaByNorm.Keys) {
    if ($key -like "*$norm*" -or $norm -like "*$key*") { return ($schemaByNorm[$key] | Select-Object -First 1) }
  }
  # Levenshtein over all schema tables
  $best = $null; $bestScore = [int]::MaxValue
  foreach ($key in $tables.Keys) {
    $dist = Get-LevenshteinDistance (Normalize-Name $key) $norm
    if ($dist -lt $bestScore) { $bestScore = $dist; $best = $key }
  }
  $limit = [Math]::Max(2, [int]([Math]::Ceiling($name.Length / 3.0)))
  if ($best -and $bestScore -le $limit) { return $best }
  return $null
}

$suggestions = @()
foreach ($mm in $mismatches) {
  if ($mm.Type -eq 'TABLE_NOT_IN_SCHEMA') {
    $suggest = Suggest-SimilarTable $mm.Table
    $fix = if ($suggest) { "Map to existing table '$suggest' or rename code reference to '$suggest'" } else { "Create table '$($mm.Table)' in schema or update code to use an existing table." }
    $suggestions += [pscustomobject]@{ Type=$mm.Type; Table=$mm.Table; Column=''; Suggestion=$fix; SuggestedTarget=$suggest }
  }
  elseif ($mm.Type -eq 'COLUMN_NOT_IN_SCHEMA') {
    $tableKey = $mm.Table.ToLowerInvariant()
    $cols = if ($tables.ContainsKey($tableKey)) { $tables[$tableKey].Columns.Keys } else { @() }
    # try similar column (normalize, substring, then Levenshtein)
    $colNorm = Normalize-Name $mm.Column
    $sim = $null
    foreach ($c in $cols) { if ((Normalize-Name $c) -eq $colNorm) { $sim = $c; break } }
    if (-not $sim) { foreach ($c in $cols) { if ((Normalize-Name $c) -like "*$colNorm*" -or $colNorm -like "*$(Normalize-Name $c)*") { $sim = $c; break } } }
    if (-not $sim -and $cols) {
      $best = $null; $bestScore = [int]::MaxValue
      foreach ($c in $cols) {
        $dist = Get-LevenshteinDistance (Normalize-Name $c) $colNorm
        if ($dist -lt $bestScore) { $bestScore = $dist; $best = $c }
      }
      $limit = [Math]::Max(2, [int]([Math]::Ceiling($mm.Column.Length / 3.0)))
      if ($best -and $bestScore -le $limit) { $sim = $best }
    }
    $fix = if ($sim) { "Use existing column '$sim' on '$($mm.Table)' or rename code field; if truly needed, add '$($mm.Column)' to schema." } else { "Add column '$($mm.Column)' to '$($mm.Table)' or adjust code to use existing columns." }
    $suggestions += [pscustomobject]@{ Type=$mm.Type; Table=$mm.Table; Column=$mm.Column; Suggestion=$fix; SuggestedTarget=$sim }
  }
}

Export-CsvSafe2 -Rows $suggestions -Path "$ReportsDir/mismatch_suggestions.csv"

# 8) Brief SYNC_REPORT sections (2–3)
$modelCount = ($invModels | Measure-Object).Count
$serviceCount = ($invServices | Measure-Object).Count
$vmCount = ($invVMs | Measure-Object).Count
$viewCount = ($invViews | Measure-Object).Count
$tablesCount = ($tables.Keys | Measure-Object).Count
$stmtCount = ($statements | Measure-Object).Count
$mmCount = ($mismatches | Measure-Object).Count

# Extra: Mismatch aggregation by table and top offenders
$mmByTable = @()
if ($mismatches) {
  $mmGroups = $mismatches | Group-Object Table
  foreach ($g in $mmGroups) {
    $typeCounts = ($g.Group | Group-Object Type | ForEach-Object { "{0}:{1}" -f $_.Name, $_.Count }) -join '; '
    $mmByTable += [pscustomobject]@{ Table=$g.Name; Total=$g.Count; Types=$typeCounts }
  }
}
Export-CsvSafe2 -Rows $mmByTable -Path "$ReportsDir/mismatch_by_table.csv"

$report = @()
$report += "## 2 Inventory"
$report += "- Models: $modelCount (reports/inventory_models.csv)"
$report += "- Services: $serviceCount (reports/inventory_services.csv)"
$report += "- ViewModels: $vmCount (reports/inventory_viewmodels.csv)"
$report += "- Views: $viewCount (reports/inventory_views.csv)"
$report += "- DB Tables in schema: $tablesCount"
$report += "- SQL statements detected: $stmtCount (code:$countCode, sql:$countSql, scans:$countReport)"
$report += ""
$report += "## 3 Code <-> DB Mismatch Summary"
$report += "- Mismatches: $mmCount (reports/mismatch-matrix.csv)"
$byType = $mismatches | Group-Object Type | Sort-Object Count -Descending
foreach ($g in $byType) { $report += "  - $($g.Name): $($g.Count)" }
$report += "  - Per-table summary: reports/mismatch_by_table.csv"

# Top missing tables and columns (up to 10 each)
$topMissingTables = $mismatches | Where-Object { $_.Type -eq 'TABLE_NOT_IN_SCHEMA' } |
  Group-Object Table | Sort-Object Count -Descending | Select-Object -First 10
if ($topMissingTables) {
  $report += "  - Top missing tables:" 
  foreach ($t in $topMissingTables) { $report += "    - $($t.Name) ($($t.Count))" }
}

$topMissingCols = $mismatches | Where-Object { $_.Type -eq 'COLUMN_NOT_IN_SCHEMA' } |
  ForEach-Object { "{0}.{1}" -f $_.Table, $_.Column } | Group-Object | Sort-Object Count -Descending | Select-Object -First 10
if ($topMissingCols) {
  $report += "  - Top missing columns:" 
  foreach ($c in $topMissingCols) { $report += "    - $($c.Name) ($($c.Count))" }
}
$report += ""
$report += "Artifacts:"
$report += "- reports/code_statements.csv"
$report += "- reports/code_table_usage.csv"
$report += "- reports/mismatch-matrix.csv"
$report += "- reports/inventory_*.csv"
$report += "- reports/mismatch_by_table.csv"
$report += "- reports/mismatch_suggestions.csv"
$report += "- reports/table_diffs.csv"
$report += "- reports/recommended_patches.sql"
$report += "- reports/diffs/by-table/*.md"

$reportText = ($report -join [Environment]::NewLine)
Set-Content -Path "$ReportsDir/SYNC_REPORT.md" -Value $reportText -NoNewline -Encoding UTF8

Write-Host "Analysis complete. See reports/mismatch-matrix.csv and SYNC_REPORT.md."
