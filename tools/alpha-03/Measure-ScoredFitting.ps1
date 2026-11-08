param([Parameter(Mandatory=$true)][string]$RunRoot,
      [ValidateSet('capability','fitting')][string]$Mode='fitting',
      [ValidateSet('gemma3:4b','gemma4:e4b')][string[]]$ModelNames=@('gemma3:4b','gemma4:e4b'))
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$binary=Join-Path $PSScriptRoot 'ScoredFitting/bin/Release/net10.0/ScoredFitting.dll'
if(-not(Test-Path -LiteralPath $binary)){throw 'Build the scored collector before timing.'}
if(Test-Path -LiteralPath (Join-Path $RunRoot 'measurement.stop')){throw 'Stop marker present; no resume.'}
if(@(Get-Process -Name brave,chrome,msedge,firefox,Code -ErrorAction SilentlyContinue).Count){throw 'Owner must close browser/editor before timing.'}
if(-not $ModelNames.Count -or @($ModelNames|Sort-Object -Unique).Count -ne $ModelNames.Count){throw 'Select distinct declared models.'}
foreach($model in $ModelNames){
    $power=Assert-AlphaPower
    $folder=Join-Path $RunRoot ($Mode+'-'+$model.Replace(':','-'))
    if(Test-Path -LiteralPath $folder){throw 'Fresh output required; no restart within a prior collection.'}
    $started=[DateTime]::UtcNow
    $code=1
    $nativeError=$null
    try {
        & dotnet $binary $Mode $model tests/alpha-03-cases data/law tools/alpha-03/scored-status.ar.txt $folder $RunRoot 2>&1 | ForEach-Object {
            $line=[string]$_
            Add-Content -LiteralPath (Join-Path $RunRoot ($model.Replace(':','-')+'-stdout.txt')) -Value $line -Encoding utf8
            Write-Output $line
        }
        $code=$LASTEXITCODE
    } catch {
        $nativeError=$_.Exception.Message
        Add-Content -LiteralPath (Join-Path $RunRoot ($model.Replace(':','-')+'-native-error.txt')) -Value $nativeError -Encoding utf8
    } finally {
        @{exitCode=$code;nativeError=$nativeError;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');power=(Get-AlphaPower)}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $RunRoot ($model.Replace(':','-')+'-process-completion.json')) -Encoding utf8
        Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body (@{model=$model;keep_alive=0}|ConvertTo-Json) -TimeoutSec 60|ConvertTo-Json|Set-Content (Join-Path $RunRoot ($model.Replace(':','-')+'-final-unload.json')) -Encoding utf8
    }
    if($nativeError -or $code -ne 0){throw ('Scored collection stopped with code '+$code+'; no next model or fitting phase.')}
    Assert-AlphaPower|Out-Null
}
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value ($Mode+'-complete') -Encoding utf8
