param([Parameter(Mandatory=$true)][string]$RunRoot)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
. (Join-Path $PSScriptRoot 'AppBenchmarkProcess.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\')
$root=[IO.Path]::GetFullPath($RunRoot).TrimEnd('\')
if($root -eq $repo -or $root.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith('\\')){throw 'Use a fresh local directory outside the repository.'}
if(Test-Path -LiteralPath $root){throw 'Run directory already exists; no overwrite or automatic retry.'}
$binary=Join-Path $repo 'src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll'
if(-not(Test-Path -LiteralPath $binary)){throw 'Build the Release harness before timing.'}
$digests=@{'gemma3:4b'='c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291';'gemma4:e4b'='dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c'}
New-Item -ItemType Directory -Path $root|Out-Null
foreach($command in @('grounds','draft')){
 foreach($model in @('gemma3:4b','gemma4:e4b')){
    Assert-AlphaPower|Out-Null
    if(@(Get-Process -Name brave,chrome,msedge,firefox,Code,dotnet,testhost,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue).Count){throw 'Close browsers/VS Code and finish all builds/tests before timing.'}
    if(-not @(Get-Process -Name DzLegalAssistant -ErrorAction SilentlyContinue).Count){throw 'Leave the desktop app open and idle in template mode.'}
    $tags=Invoke-RestMethod -Uri http://127.0.0.1:11434/api/tags -TimeoutSec 10
    if(@($tags.models|Where-Object {$_.name -eq $model -and $_.digest -eq $digests[$model]}).Count -ne 1){throw 'Installed model digest differs from the recorded model.'}
    $loaded=Invoke-RestMethod -Uri http://127.0.0.1:11434/api/ps -TimeoutSec 10
    foreach($resident in $loaded.models){
        if(-not $digests.ContainsKey($resident.name)){throw 'Another model is resident; stop before timing.'}
        Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body (@{model=$resident.name;keep_alive=0}|ConvertTo-Json) -TimeoutSec 60|Out-Null
    }
    $folder=Join-Path $root ($command+'-'+$model.Replace(':','-'))
    New-Item -ItemType Directory -Path $folder|Out-Null
    Assert-AlphaPower|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'power-before.json') -Encoding utf8
    @{command=$command;model=$model;digest=$digests[$model];think=$(if($model -eq 'gemma4:e4b'){'false'}else{'omitted'});cases='default development and robustness only';reloadEvery=10;minimumAvailableMiB=512;sharedResourceAppend=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'measurement-plan.json') -Encoding utf8
    $monitorScript=Join-Path $PSScriptRoot 'Measure-Resources.ps1'
    $monitor=Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','RemoteSigned','-File',('"'+$monitorScript+'"'),'-RunRoot',('"'+$folder+'"'),'-SharedAppend') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $folder 'monitor-stdout.txt') -RedirectStandardError (Join-Path $folder 'monitor-stderr.txt')
    try{
        $ready=$false
        for($attempt=0;$attempt -lt 60;$attempt++){
            Start-Sleep -Seconds 2
            Assert-AlphaPower|Out-Null
            $resource=Join-Path $folder 'resources.jsonl'
            if(-not(Test-Path -LiteralPath $resource)){continue}
            $samples=@(Get-Content -LiteralPath $resource -Tail 6|ForEach-Object{try{$_|ConvertFrom-Json}catch{}}|Where-Object intervalSeconds -gt 1|Select-Object -Last 5)
            if($samples.Count -eq 5 -and ($samples|Measure-Object otherCores -Average).Average -le .5){$ready=$true;break}
        }
        if(-not $ready){throw 'Quiet preflight not met; no benchmark started.'}
        $arguments=@($binary,$command,'--models',$model,'--out',$folder,'--measurement-root',$folder,'--endpoint','http://127.0.0.1:11434','--law-root',(Join-Path $repo 'data/law'),'--data',(Join-Path $repo 'data/document-types'))
        if($command -eq 'grounds'){$arguments+=@('--cases',(Join-Path $repo 'tests/grounds-cases'),'--partition','development,robustness')}
        else{$arguments+=@('--cases',(Join-Path $repo 'tests/drafting-cases'))}
        if($model -eq 'gemma4:e4b'){$arguments+=@('--think','false')}
        $argumentLine=($arguments|ForEach-Object{'"'+$_+'"'})-join ' '
        $argumentLine|Set-Content -LiteralPath (Join-Path $folder 'command.txt') -Encoding utf8
        $started=[DateTimeOffset]::UtcNow
        Write-Output ($command+' '+$model+' started '+$started.ToString('o'))
        $stdout=Join-Path $folder 'stdout.txt'
        $process=Start-AppBenchmarkProcess -FilePath dotnet.exe -Arguments $argumentLine -WorkingDirectory $repo -Stdout $stdout -Stderr (Join-Path $folder 'stderr.txt')
        $lastProgress=''
        while(-not $process.HasExited){
            Start-Sleep -Seconds 2
            if(Test-Path -LiteralPath $stdout){
                $progress=(@(Get-Content -LiteralPath $stdout -Tail 5|Where-Object {$_ -match '^PROGRESS |^GUARD '}) -join ' ')
                if($progress -and $progress -ne $lastProgress){Write-Output ($command+' '+$model+' '+$progress);$lastProgress=$progress}
            }
        }
        $code=Get-AppBenchmarkExitCode -Process $process
        @{exitCode=$code;startedUtc=$started.ToString('o');finishedUtc=[DateTimeOffset]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'process-completion.json') -Encoding utf8
        Assert-AlphaPower|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'power-after.json') -Encoding utf8
        Write-Output ($command+' '+$model+' ended with code '+$code)
        if($code -ne 0){throw 'Guarded benchmark stopped; preserve partial record, no next run or retry.'}
        $completion=Get-Content -LiteralPath (Join-Path $folder 'measurement-completion.json') -Raw|ConvertFrom-Json
        if(-not $completion.Complete){throw 'Measurement incomplete; no next run.'}
    } finally{
        Set-Content -LiteralPath (Join-Path $folder 'monitor.stop') -Value 'Run ended' -Encoding utf8
        if($monitor -and -not $monitor.HasExited){$monitor.WaitForExit(10000)|Out-Null}
    }
 }
}
