# Part 7 runner (outside the repository). Same guards and argument lines as the owner's
# tools/alpha-03/Run-OwnerAlpha01.ps1 and Measure-AppBenchmarks.ps1 at commit 480d4a4; the differences are:
#   1. the models come from models.json (tag, thinking value, digest recorded after download);
#   2. the harness is the scratch build of 480d4a4 whose only change is one switch arm that lets any other
#      model run in guarded mode when its recorded digest is given (as the two phone models did).
# This script never opens, lists or copies the hidden case folder: only the harness receives its path.
param([Parameter(Mandatory=$true)][string]$RunRoot,
      [string]$Stages='facts,grounds',
      [string]$ModelsFile=(Join-Path $PSScriptRoot 'models.json'),
      [string]$Only='')
$ErrorActionPreference='Stop'
$repo='C:\Users\TheShadow\dz-arch-research\F\harness-480d4a4'
$tools=Join-Path $repo 'tools\alpha-03'
. (Join-Path $tools 'Power.ps1')
. (Join-Path $tools 'AppBenchmarkProcess.ps1')
. (Join-Path $tools 'AppBenchmarkAwake.ps1')
$privateCases='C:\dz-hidden-v2'
$root=[IO.Path]::GetFullPath($RunRoot).TrimEnd('\')
if($root.StartsWith($repo,[StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith('C:\Users\TheShadow\dz-legal-assistant',[StringComparison]::OrdinalIgnoreCase) -or
   $root.StartsWith('C:\Users\TheShadow\dz-fable-arch',[StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith($privateCases,[StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith('\\')){
    throw 'GUARD output must be a local folder outside every repository and the case folder.'
}
if(Test-Path -LiteralPath $root){throw 'GUARD use a fresh output folder; no overwrite or automatic retry.'}
$binary=Join-Path $repo 'src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll'
if(-not(Test-Path -LiteralPath $binary)){throw 'GUARD the scratch harness is not built.'}
$stageList=@($Stages.Split(',')|ForEach-Object{$_.Trim()}|Where-Object{$_})
foreach($s in $stageList){if($s -notin @('facts','grounds','draft')){throw ('Unknown stage '+$s)}}
if($stageList -contains 'draft'){
    # Same check as Measure-AppBenchmarks.ps1 -IncludeMoreDrafts: the locked 12 files of stage 2, unchanged.
    $draftManifest=Get-Content -LiteralPath (Join-Path $tools 'DRAFT-STAGE-2.json') -Raw|ConvertFrom-Json
    if(@($draftManifest.cases).Count -ne 12){throw 'Expected the locked 12-case manifest.'}
    foreach($entry in $draftManifest.cases){if((Get-FileHash -LiteralPath (Join-Path $repo $entry.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256){throw 'An original drafting case differs from the locked manifest.'}}
    $actualDraftFiles=@(Get-ChildItem -LiteralPath (Join-Path $repo 'tests\drafting-cases'),(Join-Path $repo 'tests\drafting-cases-more') -Filter '*.json' -File)
    if($actualDraftFiles.Count -ne 12){throw 'The original drafting folders no longer contain exactly 12 files.'}
}
$models=@(Get-Content -LiteralPath $ModelsFile -Raw|ConvertFrom-Json)
if($Only){$wanted=@($Only.Split(',')|ForEach-Object{$_.Trim()});$models=@($models|Where-Object{$_.model -in $wanted})}
if(-not $models.Count){throw 'No model to run.'}
foreach($m in $models){if($m.digest -notmatch '^[a-f0-9]{64}$'){throw ('Missing recorded digest for '+$m.model)}}
Assert-AlphaPower|Out-Null
if(@(Get-Process -Name brave,chrome,msedge,firefox,Code,dotnet,testhost,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue).Count){throw 'GUARD close browsers and VS Code, and finish builds and tests.'}
if(-not @(Get-Process -Name DzLegalAssistant -ErrorAction SilentlyContinue).Count){throw 'GUARD open the desktop app and leave it idle (template mode).'}
New-Item -ItemType Directory -Path $root|Out-Null
$index=@()
$awakePrevious=Enter-AppBenchmarkAwake
$monitorScript=Join-Path $tools 'Measure-Resources.ps1'
function Stop-Resident {
    $loaded=Invoke-RestMethod -Uri http://127.0.0.1:11434/api/ps -TimeoutSec 10
    foreach($resident in @($loaded.models)){
        Invoke-RestMethod -Uri http://127.0.0.1:11434/api/generate -Method Post -ContentType application/json -Body (@{model=$resident.name;keep_alive=0}|ConvertTo-Json) -TimeoutSec 120|Out-Null
    }
    for($i=0;$i -lt 30;$i++){
        if(-not @((Invoke-RestMethod -Uri http://127.0.0.1:11434/api/ps -TimeoutSec 10).models).Count){return}
        Start-Sleep -Seconds 2
    }
    throw 'GUARD a model stayed resident.'
}
try{
  foreach($m in $models){
    $model=[string]$m.model
    $tags=Invoke-RestMethod -Uri http://127.0.0.1:11434/api/tags -TimeoutSec 10
    if(@($tags.models|Where-Object {$_.name -eq $model -and $_.digest -eq $m.digest}).Count -ne 1){
        Write-Output ('SKIP '+$model+': installed digest differs from the recorded one, or the model is absent.')
        $index+=[pscustomobject]@{model=$model;stage='-';exitCode=-1;note='digest mismatch or absent'}
        continue
    }
    foreach($stage in $stageList){
        Stop-Resident
        $folder=Join-Path $root ($stage+'-'+($model -replace '[:/\\]','-'))
        New-Item -ItemType Directory -Path $folder|Out-Null
        Assert-AlphaPower|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'power-before.json') -Encoding utf8
        $monitor=Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','RemoteSigned','-File',('"'+$monitorScript+'"'),'-RunRoot',('"'+$folder+'"'),'-SharedAppend') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $folder 'monitor-stdout.txt') -RedirectStandardError (Join-Path $folder 'monitor-stderr.txt')
        $code=3
        $started=[DateTimeOffset]::UtcNow
        try{
            $ready=$false
            for($attempt=0;$attempt -lt 60;$attempt++){
                Start-Sleep -Seconds 2
                Assert-AlphaPower|Out-Null
                $resource=Join-Path $folder 'resources.jsonl'
                if(-not(Test-Path -LiteralPath $resource)){continue}
                $samples=@(Get-Content -LiteralPath $resource -Tail 6|ForEach-Object{try{$_|ConvertFrom-Json}catch{}}|Where-Object intervalSeconds -gt 1|Select-Object -Last 5)
                if($samples.Count -eq 5 -and ($samples|Measure-Object otherCores -Average).Average -le .5){$ready=$true;break}
                if($attempt % 10 -eq 0){Write-Output 'GUARD waiting for quiet background CPU.'}
            }
            if(-not $ready){throw 'GUARD quiet preflight not met; harness not started.'}
            if(@(Get-Process -Name brave,chrome,msedge,firefox,Code -ErrorAction SilentlyContinue).Count){throw 'GUARD browser or editor reopened; harness not started.'}
            if($stage -eq 'facts'){
                $arguments=@($binary,'facts','--alpha','--claims','given','--models',$model,'--cases',$privateCases,
                    '--law-root',(Join-Path $repo 'data\law'),'--out',$folder,'--measurement-root',$folder,'--endpoint','http://127.0.0.1:11434','--expected-digest',$m.digest)
            } else {
                $arguments=@($binary,$stage,'--models',$model,'--out',$folder,'--measurement-root',$folder,'--endpoint','http://127.0.0.1:11434',
                    '--law-root',(Join-Path $repo 'data\law'),'--data',(Join-Path $repo 'data\document-types'))
                if($stage -eq 'grounds'){$arguments+=@('--cases',(Join-Path $repo 'tests\grounds-cases'),'--partition','development,robustness')}
                else{$arguments+=@('--cases',(Join-Path $repo 'tests\drafting-cases'),'--extra-cases',(Join-Path $repo 'tests\drafting-cases-more'))}  # stage 2's 12 files: 01-05 and 06-12
            }
            if($m.think -eq 'false'){$arguments+=@('--think','false')}
            $argumentLine=($arguments|ForEach-Object{'"'+$_+'"'})-join ' '
            if($stage -ne 'facts'){$argumentLine|Set-Content -LiteralPath (Join-Path $folder 'command.txt') -Encoding utf8}
            Write-Output ($stage+' '+$model+' started '+$started.ToString('o'))
            $stdout=Join-Path $folder 'stdout.txt'
            $process=Start-AppBenchmarkProcess -FilePath dotnet.exe -Arguments $argumentLine -WorkingDirectory $repo -Stdout $stdout -Stderr (Join-Path $folder 'harness-stderr.txt')
            $lastProgress=''
            while(-not $process.HasExited){
                Start-Sleep -Seconds 5
                if(Test-Path -LiteralPath $stdout){
                    $progress=(@(Get-Content -LiteralPath $stdout -Tail 3|Where-Object {$_ -match '^PROGRESS |^GUARD '}|Select-Object -Last 1) -join ' ')
                    if($progress -and $progress -ne $lastProgress){Write-Output ($stage+' '+$model+' '+$progress);$lastProgress=$progress}
                }
            }
            $code=Get-AppBenchmarkExitCode -Process $process
        } catch {
            Write-Output ('STOPPED '+$stage+' '+$model+': '+$_.Exception.Message)
        } finally {
            Set-Content -LiteralPath (Join-Path $folder 'monitor.stop') -Value 'Run ended' -Encoding utf8
            if($monitor -and -not $monitor.HasExited){$monitor.WaitForExit(10000)|Out-Null}
        }
        $finished=[DateTimeOffset]::UtcNow
        @{exitCode=$code;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'process-completion.json') -Encoding utf8
        Write-Output ($stage+' '+$model+' ended with code '+$code+' after '+[int]($finished-$started).TotalSeconds+' s')
        $index+=[pscustomobject]@{model=$model;stage=$stage;exitCode=$code;folder=$folder;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o')}
        $index|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'RUN-INDEX.json') -Encoding utf8
        if($code -ne 0){Write-Output ('Stage '+$stage+' did not complete for '+$model+'; no retry. The other stages still run.')}
    }
    try{Stop-Resident}catch{Write-Output $_.Exception.Message}
  }
} finally {
    Exit-AppBenchmarkAwake -Previous $awakePrevious
}
Write-Output ('DONE. Index: '+(Join-Path $root 'RUN-INDEX.json'))
