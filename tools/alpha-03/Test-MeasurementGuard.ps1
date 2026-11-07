param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop'
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if($OutputRoot -eq $workspace -or $OutputRoot.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Guard fixture outputs must remain outside the repository.'}
$hostBinary=Join-Path $PSScriptRoot 'FittingReferences/bin/Release/net10.0/FittingReferences.dll'
$fixtureBinary=Join-Path $PSScriptRoot 'GuardCheck/bin/Release/net10.0/brave.exe'
if(@(Get-Process -Name brave,chrome,msedge,firefox,Code -ErrorAction SilentlyContinue).Count){throw 'Close actual browsers before this no-model guard test.'}
New-Item -ItemType Directory -Path $OutputRoot | Out-Null
$best='ded574b5-45a0-4f42-8737-46345c09c238'
function Write-Sample([long]$Available,[int]$Ac=1){
    @{utc=[DateTime]::UtcNow.ToString('o');power=@{acLineStatus=$Ac;actualOverlayGuid=$best;effectiveOverlayGuid=$best;configuredAcGuid=$best};appWorkingSetBytes=200MB;availablePhysicalBytes=$Available;modelPrivateBytes=4GB;totalPhysicalBytes=16GB} | ConvertTo-Json -Depth 4 -Compress | Set-Content (Join-Path $OutputRoot 'resources.jsonl') -Encoding utf8
}
function Check-Exit([int]$Expected){
    & dotnet $hostBinary --check-guard $OutputRoot
    if($LASTEXITCODE -ne $Expected){throw ('Unexpected guard exit '+$LASTEXITCODE+' expected '+$Expected)}
}
Write-Sample 1GB
Check-Exit 0
Write-Sample (511MB)
Check-Exit 3
Copy-Item -LiteralPath (Join-Path $OutputRoot 'precondition-failure.json') -Destination (Join-Path $OutputRoot 'headroom-failure.json')
Write-Sample 1GB 0
Check-Exit 3
Copy-Item -LiteralPath (Join-Path $OutputRoot 'precondition-failure.json') -Destination (Join-Path $OutputRoot 'mains-failure.json')
$stopFile=Join-Path $OutputRoot 'helper.stop'
$fixture=Start-Process -FilePath $fixtureBinary -ArgumentList $stopFile -WindowStyle Hidden -PassThru
try{
    Write-Sample 1GB
    Check-Exit 3
    $failure=Get-Content (Join-Path $OutputRoot 'precondition-failure.json') -Raw -Encoding utf8|ConvertFrom-Json
    if($failure.Reason -ne 'browser or VS Code reopened during timed run' -or @($failure.BrowserProcesses|Where-Object Id -eq $fixture.Id).Count -ne 1){throw 'Browser fixture was not the recorded stop reason'}
    Copy-Item -LiteralPath (Join-Path $OutputRoot 'precondition-failure.json') -Destination (Join-Path $OutputRoot 'browser-failure.json')
} finally{
    Set-Content -LiteralPath $stopFile -Value 'finish benign guard fixture' -Encoding utf8
    if(-not $fixture.WaitForExit(5000)){throw 'Owned guard fixture did not exit normally'}
}
Write-Sample 1GB
Check-Exit 0
'PASS: clean start; 511 MiB blocked; battery blocked; reopening detected by actual process ID; clean recovery. No model calls.'
