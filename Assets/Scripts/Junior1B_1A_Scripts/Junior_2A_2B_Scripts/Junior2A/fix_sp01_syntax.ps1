# PowerShell script to fix malformed if statements and escaped tokens in all *_SP01_Junior1A.cs files
# Usage: Run this script from the Unity project root.
$projectRoot = (Resolve-Path "C:/Users/DASRI INISH KUMAR/OneDrive - K L University/Desktop/GitHub/EngSnap_Juniors").Path
$searchPattern = "*_SP01_Junior1A.cs"
$files = Get-ChildItem -Path "$projectRoot/Assets/Scripts/Junior1A" -Recurse -Filter $searchPattern -File
$logPath = "$projectRoot/fix_sp01_syntax_log.txt"
"--- Fix SP01 Syntax Script Log ---" | Out-File -FilePath $logPath -Encoding utf8
foreach ($file in $files) {
    $original = Get-Content -Path $file.FullName -Raw
    $modified = $original
    # 1. Fix escaped logical operators
    $modified = $modified -replace "\\u0026\\u0026", "&&"
    $modified = $modified -replace "\\u003c", "<"
    $modified = $modified -replace "\\u003e", ">"
    # 2. Ensure if statements have closing parenthesis and opening brace
    $modified = $modified -replace "(?m)^(\s*if\s*\([^\)]+)$", "$1) {"
    # 3. Remove duplicate consecutive skip button activation lines
    $modified = $modified -replace "(?m)(^\s*if \(_skipButtonObj != null\) \{ _skipButtonObj\.SetActive\(true\); \}\s*\r?\n){2,}", "$1"
    # 4. Balance braces – simple heuristic: if count of '{' > count of '}' append missing closing braces
    $openCount = ($modified -split "{").Length - 1
    $closeCount = ($modified -split "}").Length - 1
    if ($openCount -gt $closeCount) {
        $missing = $openCount - $closeCount
        $modified += "`n" + ("}" * $missing)
    }
    if ($modified -ne $original) {
        Set-Content -Path $file.FullName -Value $modified -Encoding utf8
        "Modified: $($file.FullName)" | Out-File -FilePath $logPath -Append -Encoding utf8
    } else {
        "Unchanged: $($file.FullName)" | Out-File -FilePath $logPath -Append -Encoding utf8
    }
}
"--- End of Log ---" | Out-File -FilePath $logPath -Append -Encoding utf8
