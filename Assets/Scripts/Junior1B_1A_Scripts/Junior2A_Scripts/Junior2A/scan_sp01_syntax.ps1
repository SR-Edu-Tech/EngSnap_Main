# Scan for remaining syntax issues in Junior1A SP01 scripts
$root = "C:/Users/DASRI INISH KUMAR/OneDrive - K L University/Desktop/GitHub/EngSnap_Juniors/Assets/Scripts/Junior1A"
$log = "$root/scan_sp01_syntax_log.txt"
"--- Scan Start $(Get-Date) ---" | Out-File -FilePath $log -Encoding utf8
# Incomplete if statements
"\nIncomplete IF statements:" | Out-File -FilePath $log -Append -Encoding utf8
Get-ChildItem -Path $root -Recurse -Filter "*_SP01_Junior1A.cs" | ForEach-Object {
    $matches = Select-String -Path $_.FullName -Pattern "if\s*\([^)]*$" -AllMatches
    foreach ($m in $matches) {
        "{0}: line {1}" -f $_.FullName, $m.LineNumber | Out-File -FilePath $log -Append -Encoding utf8
    }
}
# Escaped tokens still present
"\nEscaped token occurrences:" | Out-File -FilePath $log -Append -Encoding utf8
Get-ChildItem -Path $root -Recurse -Filter "*_SP01_Junior1A.cs" | ForEach-Object {
    $matches = Select-String -Path $_.FullName -Pattern "\\u0026\\u0026|\\u003c|\\u003e" -AllMatches
    foreach ($m in $matches) {
        "{0}: line {1}" -f $_.FullName, $m.LineNumber | Out-File -FilePath $log -Append -Encoding utf8
    }
}
"--- Scan End $(Get-Date) ---" | Out-File -FilePath $log -Append -Encoding utf8
