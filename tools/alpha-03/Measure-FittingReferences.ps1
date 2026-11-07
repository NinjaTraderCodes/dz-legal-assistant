param([Parameter(Mandatory=$true)][string]$RunRoot)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$binary=Join-Path $PSScriptRoot 'FittingReferences/bin/Release/net10.0/FittingReferences.dll'
if(-not(Test-Path -LiteralPath $binary)){throw 'Build the reference host before timing.'}
foreach($model in @('gemma3:4b','gemma4:e4b')){
    $power=Assert-AlphaPower
    $slug=$model.Replace(':','-')
    $folder=Join-Path $RunRoot ('reference-'+$slug)
    New-Item -ItemType Directory -Path $folder | Out-Null
    $power|ConvertTo-Json|Set-Content (Join-Path $folder 'power.json') -Encoding utf8
    foreach($previous in @('gemma3:4b','gemma4:e4b')){
        Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body (@{model=$previous;keep_alive=0}|ConvertTo-Json) -TimeoutSec 60 | Out-Null
    }
    $started=[DateTime]::UtcNow
    & dotnet $binary $model tests/alpha-03-cases data/law $folder $RunRoot 2>&1 | ForEach-Object {
        $line=[string]$_
        Add-Content (Join-Path $folder 'stdout.txt') -Value $line -Encoding utf8
        Write-Output $line
    }
    $code=$LASTEXITCODE
    Assert-AlphaPower|ConvertTo-Json|Set-Content (Join-Path $folder 'power-after.json') -Encoding utf8
    @{exitCode=$code;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');processSeconds=([DateTime]::UtcNow-$started).TotalSeconds}|ConvertTo-Json|Set-Content (Join-Path $folder 'process-completion.json') -Encoding utf8
    if($code -ne 0){throw ('Reference stopped with code '+$code+'; no next model or candidate run.')}
}
Set-Content (Join-Path $RunRoot 'phase.txt') -Value 'references-complete' -Encoding utf8
