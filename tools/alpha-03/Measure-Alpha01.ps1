param([Parameter(Mandatory=$true)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$utf8 = New-Object Text.UTF8Encoding($false)
$models = @('gemma3:4b','gemma4:e4b')
foreach ($model in $models) {
  $modelPower = Assert-AlphaPower
  $slug = $model.Replace(':','-')
  $phase = 'rung0-' + $slug
  Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value $phase -Encoding utf8
  foreach ($previousModel in $models) {
    $body = @{model=$previousModel;keep_alive=0} | ConvertTo-Json
    $response = Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body ($utf8.GetBytes($body)) -MaximumRedirection 0 -TimeoutSec 60
    $response | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $RunRoot ($phase+'-unload-'+$previousModel.Replace(':','-')+'.json')) -Encoding utf8
  }
  $folder = Join-Path $RunRoot $phase
  New-Item -ItemType Directory -Path $folder | Out-Null
  $modelPower | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'power.json') -Encoding utf8
  $started = [DateTime]::UtcNow
  $command = @('src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll','facts','--alpha','--claims','given','--models',$model,'--partition','development','--cases','tests/fact-cases','--law-root','data/law','--out',$folder)
  @{command='dotnet';arguments=$command;startedUtc=$started.ToString('o');expectedInferenceCases=40;excludedDevelopmentCases=4;noThinkOverride=$true;readerChanged=$false} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder 'invocation.json') -Encoding utf8
  & dotnet @command 2>&1 | ForEach-Object {
    $line = [string]$_
    Add-Content -LiteralPath (Join-Path $folder 'stdout.txt') -Value $line -Encoding utf8
    @{utc=[DateTime]::UtcNow.ToString('o');line=$line} | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $folder 'stdout-timestamps.jsonl') -Encoding utf8
    Write-Output $line
  }
  $code = $LASTEXITCODE
  Assert-AlphaPower | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'power-after.json') -Encoding utf8
  @{exitCode=$code;finishedUtc=[DateTime]::UtcNow.ToString('o');wallSeconds=([DateTime]::UtcNow-$started).TotalSeconds} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'completion.json') -Encoding utf8
  if ($code -ne 0) { throw "Unchanged Alpha-01 harness failed for $model with exit $code" }
  Add-Content -LiteralPath (Join-Path $folder 'summary.md') -Value ("`nMeasurement conditions: mains power; Best performance. Actual/effective/configured AC mode GUID: {0}. Companion power records and continuous CPU/memory/paging samples are retained outside the repository.`n" -f $modelPower.effectiveOverlayGuid) -Encoding utf8
}
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value 'rung0-complete' -Encoding utf8
