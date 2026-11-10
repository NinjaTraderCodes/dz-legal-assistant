# Exercises the functional monitor with a short-lived, case-free process, never a model.
$ErrorActionPreference='Stop'
$path=Join-Path $PSScriptRoot 'M5-Acceptance.ps1'
$tokens=$null; $parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw 'Acceptance tool has invalid syntax' }
foreach ($name in @('New-FunctionalRecord','Wait-FunctionalApp')) {
    $function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true)
    if ($null -eq $function) { throw 'Functional acceptance entry point is missing' }
    Invoke-Expression $function.Extent.Text
}
$record=New-FunctionalRecord -Phase ContinueOnline -PackageSha256 ('a'*64) -StandardAccount $true -FreshAppData $false -NormalOllamaAbsent $true
$expected=@('schema','phase','checkType','packageSha256','standardAccount','freshAppDataAtStart','normalOllamaAbsent','performanceMeasurementsCollected','exitCode','events')
if (@($record.Keys | Where-Object {$_ -notin $expected}).Count -gt 0 -or $record.Count -ne $expected.Count) { throw 'Functional record contains unexpected fields' }
if ($record.checkType -ne 'functional-only' -or $record.performanceMeasurementsCollected) { throw 'Functional provenance is missing' }
if ($record.freshAppDataAtStart) { throw 'Continuation was incorrectly labelled fresh' }
$saveState=@{Count=0}
$save={ $saveState.Count++; $null=$record | ConvertTo-Json -Depth 4 }
$child=Start-Process -FilePath (Join-Path $PSHOME 'powershell.exe') -ArgumentList @('-NoProfile','-NonInteractive','-Command','"Start-Sleep -Milliseconds 600; exit 0"') -WindowStyle Hidden -PassThru
try {
    Wait-FunctionalApp -App $child -Record $record -Markers @{V='model-ready'} -Save $save
    if (-not $child.HasExited -or $record.exitCode -ne 0) { throw 'Functional monitor did not record a normal exit' }
    if ($saveState.Count -lt 1) { throw 'Functional result was not saved' }
    if ($record.events.Count -ne 0) { throw 'The monitor invented an owner milestone' }
    $record.events.Add([ordered]@{event='model-ready'})
    $json=$record | ConvertTo-Json -Depth 4
    if ($json -match 'startUtc|endUtc|utc|workingSet|privateBytes|availableMemory|peak|samples|freeDisk|BytesPerSecond') { throw 'Functional result contains a timestamp or resource measurement' }
    'PASS: functional lifecycle, exit result, honest provenance and no timestamps/resource samples.'
} finally { $child.Dispose() }