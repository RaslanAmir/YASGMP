$ErrorActionPreference = 'Stop'

function Replace-In-CommentText {
    param([string]$text)
    $map = @{
        'Ä' = 'č'; 'ÄŤ' = 'č'; 'Ä‡' = 'ć'; 'Ä†' = 'Ć'; 'Ä‘' = 'đ'; 'Ä' = 'Đ'; 'Å¡' = 'š'; 'Å ' = 'Š'; 'Å¾' = 'ž'; 'Å½' = 'Ž';
        'UreÄ‘' = 'Uređ'; 'AĹ¾' = 'Až'; 'GreĹ' = 'Greš'; 'UÄŤ' = 'Uč'; 'PoÄ' = 'Poč'; 'ZavrĹ' = 'Zavr'; 'BiljeĹ' = 'Bilješ';
        'DobavljaÄ' = 'Dobavljač'; 'skladiĹ' = 'skladiš'; 'pridruĹ¾' = 'pridruž'; 'KritiÄ' = 'Kriti'; 'IoT ure' = 'IoT uređ'; 'ProizvoÄ‘' = 'Proizvođa';
    }
    $out = $text
    foreach ($k in $map.Keys) {
        $out = $out -replace [Regex]::Escape($k), [Regex]::Escape($map[$k]).Replace('\\','\\')
    }
    return $out
}

$files = Get-ChildItem -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
$fixed = @()
foreach ($f in $files) {
    $orig = Get-Content -Path $f.FullName -Raw
    $lines = $orig -split "`n"
    $changed = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^(\s*///)(.*)$') {
            $prefix = $Matches[1]; $body = $Matches[2]
            $newBody = Replace-In-CommentText $body
            if ($newBody -ne $body) { $lines[$i] = $prefix + $newBody; $changed = $true }
            continue
        }
        if ($line -match '^(\s*//)(.*)$') {
            $prefix = $Matches[1]; $body = $Matches[2]
            $newBody = Replace-In-CommentText $body
            if ($newBody -ne $body) { $lines[$i] = $prefix + $newBody; $changed = $true }
            continue
        }
    }
    if ($changed) {
        # Preserve CRLF and write UTF-8 without BOM
        $content = [string]::Join("`n", $lines)
        [System.IO.File]::WriteAllText($f.FullName, $content, New-Object System.Text.UTF8Encoding($false))
        $fixed += $f.FullName
    }
}
"Fixed files: $($fixed.Count)"
foreach($p in $fixed){ Write-Output $p }
