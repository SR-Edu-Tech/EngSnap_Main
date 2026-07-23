# PowerShell script to reorder the skip method and the score handling block
$root = "C:/Users/DASRI INISH KUMAR/OneDrive - K L University/Desktop/GitHub/EngSnap_Juniors/Assets/Scripts/Junior1A"
$files = Get-ChildItem -Path $root -Recurse -Filter "*_SP01_Junior1A.cs"
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    # Regex to capture the Skip method block and the following score-if block
    $pattern = '(?s)(?<skip>\s*//\s*Called by the Skip UI button[\s\S]*?public\s+void\s+OnSkipButtonPressed\s*\(\)\s*{[\s\S]*?}\s*)(?<ifblock>\s*if\s*\(score\s*>=\s*passThreshold\)[\s\S]*?}\s*else\s+if\s*\(final\)[\s\S]*?})'
    if ($content -match $pattern) {
        $skipBlock = $Matches['skip']
        $ifBlock = $Matches['ifblock']
        # Remove original occurrence
        $newContent = $content -replace [regex]::Escape($skipBlock), ""
        $newContent = $newContent -replace [regex]::Escape($ifBlock), ""
        # Insert the if block before the skip block (maintaining original order of other code)
        # Find the position to insert: after the previous line where the skip originally was (approx) – insert at the first occurrence of a closing brace of EvaluateSpeech before skip
        # Simpler: insert at the location of the first occurrence of the skip method (which we removed), i.e., replace a marker.
        # We'll just append the reordered blocks where the skip method used to be.
        $reordered = "`r`n$ifBlock`r`n$skipBlock"
        # Insert reordered blocks where the first occurrence of the original skip method was (we can use the position of the removed skip)
        # Since we removed both blocks, just place them at the end of class before the last brace.
        $lastBraceIdx = $newContent.LastIndexOf('}')
        if ($lastBraceIdx -gt 0) {
            $newContent = $newContent.Insert($lastBraceIdx, $reordered)
        } else {
            $newContent = $newContent + $reordered
        }
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
        Write-Host "Reordered skip method in $($file.FullName)"
    }
}
