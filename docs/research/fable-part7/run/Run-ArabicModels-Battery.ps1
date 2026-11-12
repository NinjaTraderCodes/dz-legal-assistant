# Part 7 runner, BATTERY variant (outside the repository). A copy of Run-ArabicModels.ps1 as reviewed in 1e8b3a1 / 167d753.
# Every hidden-set protection, guard, argument line and output rule is the same. The differences are:
#   1. power: the laptop must be ON BATTERY for the whole run (acLineStatus 0); Best performance is not required and the
#      Windows power mode is left as found and recorded. The scratch harness is told so through DZLEGAL_PART7_POWER=battery
#      (see harness-480d4a4.diff: with it the harness refuses mains instead of battery; every other condition is unchanged);
#   2. battery limits: 25 % or more before each stage, and a stage is stopped (never counted) if the battery falls below 15 %;
#      the run then stops cleanly and prints what remains and the exact command to resume in a new run folder;
#   3. order by stage: facts for every model, then grounds, then draft, then the reference gemma4:e4b on grounds and draft
#      only (never facts: its hidden v2 result exists);
#   4. power-before.json and power-after.json for every stage; -StartAt and -PlanOnly.
# This script never opens, lists or copies the hidden case folder: only the harness receives its path.
param([Parameter(Mandatory=$true)][string]$RunRoot,
      [string]$Stages='facts,grounds,draft',
      [string]$ModelsFile='',
      [string]$Only='',
      [int]$StartAt=1,
      [switch]$PlanOnly)
$ErrorActionPreference='Stop'
# Windows PowerShell 5.1 leaves $PSScriptRoot empty inside param() defaults when started with -File, so the default is set here.
if(-not $ModelsFile){$ModelsFile=Join-Path $PSScriptRoot 'models.json'}
$startMinimum=25   # battery percent required before each stage
$stopBelow=15      # a running stage is stopped below this
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
# Windows PowerShell 5.1 passes a JSON array down the pipeline as ONE object, so it is assigned first and wrapped after.
$parsedModels=Get-Content -LiteralPath $ModelsFile -Raw|ConvertFrom-Json
$models=@($parsedModels)
# The battery speed reference: the app's model, with its committed digest and thinking off, on grounds and draft only.
$reference=[pscustomobject]@{model='gemma4:e4b';think='false';digest='dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c'}
$wanted=@()
if($Only){$wanted=@($Only.Split(',')|ForEach-Object{$_.Trim()});$models=@($models|Where-Object{$_.model -in $wanted})}
foreach($m in $models){if($m.digest -notmatch '^[a-f0-9]{64}$'){throw ('Missing recorded digest for '+$m.model)}}
# The plan, by stage: every listed model on facts, then on grounds, then on draft; then the reference on grounds and draft.
$plan=@()
foreach($stage in @('facts','grounds','draft')){
    if($stage -in $stageList){foreach($m in $models){$plan+=[pscustomobject]@{n=0;stage=$stage;m=$m}}}
}
if(-not $Only -or $reference.model -in $wanted){
    foreach($stage in @('grounds','draft')){if($stage -in $stageList){$plan+=[pscustomobject]@{n=0;stage=$stage;m=$reference}}}
}
for($i=0;$i -lt $plan.Count;$i++){$plan[$i].n=$i+1}
if(-not $plan.Count){throw 'Nothing to run.'}
if($StartAt -lt 1 -or $StartAt -gt $plan.Count){throw ('-StartAt must be between 1 and '+$plan.Count)}
foreach($step in $plan){if($step.stage -eq 'facts' -and $step.m.model -like 'gemma*'){throw 'GUARD the reference model never runs the hidden set.'}}
function Format-Step($step){('{0,2}. {1,-7} {2}' -f $step.n,$step.stage,$step.m.model)}
if($PlanOnly){
    Write-Output ('PLAN ('+$plan.Count+' stages; starting at '+$StartAt+'):')
    foreach($step in $plan){Write-Output ((Format-Step $step)+$(if($step.n -lt $StartAt){'   (skipped by -StartAt)'}else{''}))}
    return
}
$modeNames=@{'00000000-0000-0000-0000-000000000000'='Balanced';'961cc777-2547-4f9d-8174-7d86181b8a7a'='Best power efficiency';'ded574b5-45a0-4f42-8737-46345c09c238'='Best performance'}
function Get-BatteryPower {
    # Replaces Assert-AlphaPower: on battery is required; the power mode is recorded, not required.
    $power=Get-AlphaPower
    if($power.acLineStatus -ne 0){throw ('Battery run: the laptop must be on battery (unplug the charger): '+($power|ConvertTo-Json -Compress))}
    $mode=$modeNames[[string]$power.effectiveOverlayGuid]
    [pscustomobject]@{utc=[DateTimeOffset]::UtcNow.ToString('o');acLineStatus=$power.acLineStatus;batteryPercent=$power.batteryPercent;energySaver=$power.energySaver;
        effectiveOverlayGuid=$power.effectiveOverlayGuid;actualOverlayGuid=$power.actualOverlayGuid;powerMode=$(if($mode){$mode}else{'unknown'});requiredMode='none (battery run, mode as found)'}
}
Get-BatteryPower|Out-Null
if(@(Get-Process -Name brave,chrome,msedge,firefox,Code,dotnet,testhost,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue).Count){throw 'GUARD close browsers and VS Code, and finish builds and tests.'}
if(-not @(Get-Process -Name DzLegalAssistant -ErrorAction SilentlyContinue).Count){throw 'GUARD open the desktop app and leave it idle (template mode).'}
New-Item -ItemType Directory -Path $root|Out-Null
$env:DZLEGAL_PART7_POWER='battery'   # read by the scratch harness; inherited by the processes this script starts
$index=@()
$stopReason=$null
$nextStep=$null
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
  foreach($step in $plan){
    if($step.n -lt $StartAt){continue}
    $m=$step.m
    $stage=$step.stage
    $model=[string]$m.model
    # Battery limit before the stage: stop the whole run cleanly, nothing started.
    try{$before=Get-BatteryPower}catch{$stopReason='the laptop is on mains';$nextStep=$step;break}
    if($before.batteryPercent -lt $startMinimum){$stopReason=('battery at '+$before.batteryPercent+' %, below '+$startMinimum+' % before a stage');$nextStep=$step;break}
    $tags=Invoke-RestMethod -Uri http://127.0.0.1:11434/api/tags -TimeoutSec 10
    if(@($tags.models|Where-Object {$_.name -eq $model -and $_.digest -eq $m.digest}).Count -ne 1){
        Write-Output ('SKIP '+$stage+' '+$model+': installed digest differs from the recorded one, or the model is absent.')
        $index+=[pscustomobject]@{n=$step.n;model=$model;stage=$stage;exitCode=-1;complete=$false;note='digest mismatch or absent'}
        $index|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'RUN-INDEX.json') -Encoding utf8
        continue
    }
    Stop-Resident
    $folder=Join-Path $root ($stage+'-'+($model -replace '[:/\\]','-'))
    New-Item -ItemType Directory -Path $folder|Out-Null
    $before|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'power-before.json') -Encoding utf8
    $monitor=Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','RemoteSigned','-File',('"'+$monitorScript+'"'),'-RunRoot',('"'+$folder+'"'),'-SharedAppend') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $folder 'monitor-stdout.txt') -RedirectStandardError (Join-Path $folder 'monitor-stderr.txt')
    $code=3
    $lowBattery=$false
    $started=[DateTimeOffset]::UtcNow
    try{
        $ready=$false
        for($attempt=0;$attempt -lt 60;$attempt++){
            Start-Sleep -Seconds 2
            Get-BatteryPower|Out-Null
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
        Write-Output ((Format-Step $step)+' started '+$started.ToString('o')+' at '+$before.batteryPercent+' % battery, mode '+$before.powerMode)
        $stdout=Join-Path $folder 'stdout.txt'
        $process=Start-AppBenchmarkProcess -FilePath dotnet.exe -Arguments $argumentLine -WorkingDirectory $repo -Stdout $stdout -Stderr (Join-Path $folder 'harness-stderr.txt')
        $lastProgress=''
        $stopAsked=$null
        while(-not $process.HasExited){
            Start-Sleep -Seconds 5
            if(Test-Path -LiteralPath $stdout){
                $progress=(@(Get-Content -LiteralPath $stdout -Tail 3|Where-Object {$_ -match '^PROGRESS |^GUARD '}|Select-Object -Last 1) -join ' ')
                if($progress -and $progress -ne $lastProgress){Write-Output ($stage+' '+$model+' '+$progress);$lastProgress=$progress}
            }
            # Battery limit during the stage: ask the harness to stop through its own operator-stop file; the stage is not counted.
            $now=Get-AlphaPower
            if(-not $stopAsked -and $now.acLineStatus -eq 0 -and $now.batteryPercent -lt $stopBelow){
                $lowBattery=$true
                $stopAsked=[DateTimeOffset]::UtcNow
                Set-Content -LiteralPath (Join-Path $folder 'measurement.stop') -Value ('Battery below '+$stopBelow+' %') -Encoding utf8
                Write-Output ('BATTERY '+$now.batteryPercent+' %: stopping '+$stage+' '+$model+'; this stage will not be counted.')
            }
            if($stopAsked -and ([DateTimeOffset]::UtcNow-$stopAsked).TotalSeconds -gt 300 -and -not $process.HasExited){
                Write-Output 'BATTERY the harness did not stop within 5 minutes; ending its process.'
                $process.Kill()
                $process.WaitForExit(15000)|Out-Null
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
    $afterRaw=Get-AlphaPower
    $afterMode=$modeNames[[string]$afterRaw.effectiveOverlayGuid]
    $after=[pscustomobject]@{utc=$finished.ToString('o');acLineStatus=$afterRaw.acLineStatus;batteryPercent=$afterRaw.batteryPercent;energySaver=$afterRaw.energySaver;
        effectiveOverlayGuid=$afterRaw.effectiveOverlayGuid;actualOverlayGuid=$afterRaw.actualOverlayGuid;powerMode=$(if($afterMode){$afterMode}else{'unknown'})}
    $after|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'power-after.json') -Encoding utf8
    # How many resource samples of this stage had Windows' energy saver on (it changes speed); counted from the monitor's own file.
    $saverSamples=0
    $resourceFile=Join-Path $folder 'resources.jsonl'
    if(Test-Path -LiteralPath $resourceFile){$saverSamples=@(Select-String -LiteralPath $resourceFile -Pattern '"energySaver":\s*[1-9]').Count}
    $complete=($code -eq 0 -and -not $lowBattery)
    @{exitCode=$code;complete=$complete;lowBatteryStop=$lowBattery;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $folder 'process-completion.json') -Encoding utf8
    Write-Output ((Format-Step $step)+' ended with code '+$code+' after '+[int]($finished-$started).TotalSeconds+' s; battery '+$before.batteryPercent+' % -> '+$after.batteryPercent+' %'+$(if($complete){''}else{'; NOT COMPLETE, not counted'}))
    $index+=[pscustomobject]@{n=$step.n;model=$model;stage=$stage;exitCode=$code;complete=$complete;lowBatteryStop=$lowBattery;folder=$folder;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o');
        batteryBefore=$before.batteryPercent;batteryAfter=$after.batteryPercent;powerModeBefore=$before.powerMode;powerModeAfter=$after.powerMode;energySaverSamples=$saverSamples}
    $index|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'RUN-INDEX.json') -Encoding utf8
    try{Stop-Resident}catch{Write-Output $_.Exception.Message}
    if($lowBattery){$stopReason=('battery fell below '+$stopBelow+' % during '+$stage+' '+$model);$nextStep=$step;break}
    # A stage cut short because the charger was plugged in is resumed too (12 November: Falcon was left out of the resume).
    if(-not $complete -and $afterRaw.acLineStatus -ne 0){$stopReason=('the charger was plugged in during '+$stage+' '+$model);$nextStep=$step;break}
    if($code -ne 0){Write-Output ('Stage '+$stage+' did not complete for '+$model+'; no retry. The other stages still run.')}
  }
} finally {
    Exit-AppBenchmarkAwake -Previous $awakePrevious
    Remove-Item Env:\DZLEGAL_PART7_POWER -ErrorAction SilentlyContinue
}
$lines=@()
$failed=@($index|Where-Object{-not $_.complete})
if($nextStep){
    # A new folder name for the resume: <name>-2, <name>-3 ...
    $next=if($root -match '^(.*)-(\d+)$'){$Matches[1]+'-'+([int]$Matches[2]+1)}else{$root+'-2'}
    $lines+=('RUN STOPPED: '+$stopReason+'.')
    $lines+='Remaining stages, in order:'
    foreach($step in $plan){if($step.n -ge $nextStep.n){$lines+=('  '+(Format-Step $step))}}
    $lines+='To resume: charge the laptop, unplug it, keep the app open and idle, close browsers and VS Code, then run:'
    $resume='powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "'+$PSCommandPath+'" -RunRoot "'+$next+'" -Stages "'+($stageList -join ',')+'"'
    if($Only){$resume+=' -Only "'+$Only+'"'}
    $resume+=' -StartAt '+$nextStep.n
    $lines+=('  '+$resume)
} else {
    $lines+='ALL PLANNED STAGES WERE ATTEMPTED.'
}
if($failed.Count){
    $lines+='Stages that did not complete in this run (not counted):'
    foreach($f in $failed){$lines+=('  '+$f.n+'. '+$f.stage+' '+$f.model+' (exit '+$f.exitCode+$(if($f.note){'; '+$f.note}else{''})+')')}
}
if(-not $index.Count){'[]'|Set-Content -LiteralPath (Join-Path $root 'RUN-INDEX.json') -Encoding utf8}
$lines|Set-Content -LiteralPath (Join-Path $root 'REMAINING.txt') -Encoding utf8
$lines|ForEach-Object{Write-Output $_}
Write-Output ('DONE. Index: '+(Join-Path $root 'RUN-INDEX.json'))
