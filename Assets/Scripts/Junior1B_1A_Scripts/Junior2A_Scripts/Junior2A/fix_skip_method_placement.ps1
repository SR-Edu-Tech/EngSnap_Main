# Fix OnSkipButtonPressed placement in all SP01 scripts
$root = "C:/Users/DASRI INISH KUMAR/OneDrive - K L University/Desktop/GitHub/EngSnap_Juniors/Assets/Scripts/Junior1A"
$files = Get-ChildItem -Path $root -Recurse -Filter "*_SP01_Junior1A.cs"
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    # Regex to capture the comment and method block (including preceding comment line)
    $pattern = '(?s)\s*//\s*Called by the Skip UI button.*?public void OnSkipButtonPressed\(\)\s*{.*?^\s*}'
    if ($content -match $pattern) {
        $methodBlock = $Matches[0]
        # Remove original occurrence
        $newContent = $content -replace [regex]::Escape($methodBlock), ""
        # Prepare the method definition (trim leading/trailing whitespace)
        $methodDef = "`r`n    // Called by the Skip UI button after a failed attempt`r`n    public void OnSkipButtonPressed()`r`n    {`r`n        if (_currentAudioIndex < _questionClips.Length - 1)`r`n        {`r`n            _currentAudioIndex++;`r`n            ShowTargetWord();`r`n        }`r`n        if (_skipButtonObj != null) _skipButtonObj.SetActive(false);`r`n    }`r`n"
        # Insert before the final closing brace of the class (assume last '}' is class end)
        $lastBraceIndex = $newContent.LastIndexOf('}')
        if ($lastBraceIndex -gt 0) {
            $newContent = $newContent.Insert($lastBraceIndex, $methodDef)
        }
        # Write back
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
        Write-Host "Fixed: $($file.FullName)"
    }
}
