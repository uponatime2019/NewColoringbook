# UIA smoke test for ZenColoringbookClone (registered layout run).
# Memory gotchas applied: Rate-This-App popup on first run eats the first click;
# name-hits may return inner TextBlock -> walk parents for ControlType.Button.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::FocusedElement
# find the app window by process
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, (Get-Process ZenColoringbookClone).Id)
$window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $window) { Write-Output "FAIL: window not found"; exit 1 }
Write-Output "WINDOW: $($window.Current.Name)"

function Find-AllByName($scope, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Invoke-Button($el) {
    $target = $el
    # walk up to a button if we hit an inner text element
    $guard = 0
    while ($target -and $guard -lt 6 -and $target.Current.ControlType.ProgrammaticName -ne "ControlType.Button") {
        $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
        $target = $walker.GetParent($target)
        $guard++
    }
    if (-not $target) { return $false }
    $inv = $null
    try { $inv = $target.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) } catch {}
    if ($inv) { $inv.Invoke(); return $true }
    try {
        $legacy = $target.GetCurrentPattern([System.Windows.Automation.AutomationElement]::LegacyIAccessiblePattern) 2>$null
    } catch {}
    return $false
}

# 1) dismiss Rate This App popup if present
Start-Sleep -Milliseconds 500
$candidates = @("Rate This App", "No, thanks", "No")
foreach ($n in @("No, thanks", "No")) {
    $els = Find-AllByName $window $n
    foreach ($el in $els) {
        Write-Output "DISMISS candidate: $n"
        Invoke-Button $el | Out-Null
        break
    }
}
Start-Sleep -Seconds 1

# 2) click the music play button
$music = Find-AllByName $window "Play or pause ambient music"
if ($music.Count -gt 0) {
    Write-Output "MUSIC BUTTON FOUND"
    Invoke-Button $music[0] | Out-Null
    Write-Output "MUSIC CLICKED"
} else {
    Write-Output "WARN: music button not found"
}

# 3) wait for synthesis then check the wav + state
Start-Sleep -Seconds 8
$pkg = Get-AppxPackage -Name "716d6200-c4a8-4885-8519-ba3117b5e03f"
$media = "$env:LOCALAPPDATA\Packages\$($pkg.PackageFamilyName)\LocalState\media\calm_forest.wav"
if (Test-Path $media) {
    $len = (Get-Item $media).Length
    Write-Output "WAV OK: $len bytes"
} else {
    Write-Output "WARN: wav not created yet"
}

# 4) open a studio card
$cards = Find-AllByName $window "Floral Owl, 0 percent complete"
if ($cards.Count -gt 0) {
    Write-Output "CARD FOUND"
    Invoke-Button $cards[0] | Out-Null
    Start-Sleep -Seconds 3
    # after navigation the studio header should be in the tree
    $titles = Find-AllByName $window "Floral Owl"
    Write-Output "STUDIO TITLE HITS: $($titles.Count)"
} else {
    Write-Output "WARN: card not found (search title text?)"
    $all = Find-AllByName $window "Floral Owl"
    Write-Output "NAME HITS Floral Owl: $($all.Count)"
}

# 5) dump interesting controls + log tail
$tools = @("Fill tool", "Brush tool", "Eraser tool", "Undo", "Redo", "Choose texture", "Back to gallery")
foreach ($t in $tools) {
    $hits = Find-AllByName $window $t
    Write-Output "CTRL '$t': $($hits.Count)"
}
Write-Output "=== log tail ==="
Get-Content "$env:LOCALAPPDATA\Packages\$($pkg.PackageFamilyName)\LocalState\logs\app.log" -Tail 15
