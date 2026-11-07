param([Parameter(Mandatory=$true)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$utf8 = New-Object Text.UTF8Encoding($false)
$folder = Join-Path $RunRoot 'rung0-gemma4-e4b-think-off'
$binary = Join-Path $PSScriptRoot 'ModelCheck/bin/Release/net10.0/ModelCheck.dll'
if (-not (Test-Path -LiteralPath $binary)) { throw 'Build the measurement host before timing.' }
if (-not (Test-Path -LiteralPath (Join-Path $RunRoot 'resources.jsonl'))) { throw 'Start the resource monitor first.' }
$power = Assert-AlphaPower
New-Item -ItemType Directory -Path $folder | Out-Null
$power | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'power.json') -Encoding utf8
foreach ($model in @('gemma3:4b','gemma4:e4b')) {
    $body = $utf8.GetBytes((@{model=$model;keep_alive=0} | ConvertTo-Json))
    Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body $body -MaximumRedirection 0 -TimeoutSec 60 | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder ('unload-'+$model.Replace(':','-')+'.json')) -Encoding utf8
}
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value 'rung0-gemma4-e4b-think-off' -Encoding utf8
$started = [DateTime]::UtcNow
$arguments = @($binary,'tests/fact-cases','data/law',$folder)
@{command='dotnet';arguments=$arguments;startedUtc=$started.ToString('o');plannedCases=40;readerChanged=$false;think=$false;maxTokens=300;stop='first cap or technical failure; no higher cap';binarySha256=(Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'invocation.json') -Encoding utf8
& dotnet @arguments 2>&1 | ForEach-Object {
    $line = [string]$_
    Add-Content -LiteralPath (Join-Path $folder 'stdout.txt') -Value $line -Encoding utf8
    @{utc=[DateTime]::UtcNow.ToString('o');line=$line} | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $folder 'stdout-timestamps.jsonl') -Encoding utf8
    Write-Output $line
}
$exitCode = $LASTEXITCODE
$finished = [DateTime]::UtcNow
Assert-AlphaPower | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'power-after.json') -Encoding utf8
@{exitCode=$exitCode;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o');wallSeconds=($finished-$started).TotalSeconds} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'process-completion.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value 'think-off-complete' -Encoding utf8
if ($exitCode -ne 0) { throw ('Thinking-off measurement host failed: '+$exitCode) }
