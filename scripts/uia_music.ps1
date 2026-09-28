# Focused UIA test: dismiss rate popup, click music play, verify UI stays responsive.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process ZenColoringbookClone
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $window) { Write-Output "FAIL: window not found"; exit 1 }
Write-Output "WINDOW: $($window.Current.Name)"

function Find-ByName($scope, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Invoke-El($el) {
    $target = $el
    $guard = 0
    while ($target -and $guard -lt 6 -and $target.Current.ControlType.ProgrammaticName -ne "ControlType.Button") {
        $target = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($target)
        $guard++
    }
    if (-not $target) { return $false }
    try {
        $inv = $target.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
        return $true
    } catch { return $false }
}

# dismiss first-run rate popup (memory: eats the first click)
Start-Sleep -Seconds 1
foreach ($n in @("No, thanks", "No", "Later")) {
    $el = Find-ByName $window $n
    if ($el) { Write-Output "DISMISS: $n"; Invoke-El $el | Out-Null; break }
}

# click music play
$music = Find-ByName $window "Play or pause ambient music"
if ($music) {
    Write-Output "MUSIC: invoking"
    Invoke-El $music | Out-Null
} else {
    Write-Output "WARN music button not found"
}

# poll responsiveness for 30s — the previous build hung here
$hung = $false
for ($i = 0; $i -lt 10; $i++) {
    Start-Sleep -Seconds 3
    $p = Get-Process ZenColoringbookClone -ErrorAction SilentlyContinue
    if (-not $p) { Write-Output "FAIL: process died"; exit 1 }
    if (-not $p.Responding) { $hung = $true }
    Write-Output ("t+{0,2}s responding={1}" -f (($i + 1) * 3), $p.Responding)
}
Write-Output $(if ($hung) { "RESULT: HUNG (some polls false)" } else { "RESULT: RESPONSIVE throughout" })

$pkg = Get-AppxPackage -Name "716d6200-c4a8-4885-8519-ba3117b5e03f"
"=== log ==="
Get-Content "$env:LOCALAPPDATA\Packages\$($pkg.PackageFamilyName)\LocalState\logs\app.log" -Tail 8
