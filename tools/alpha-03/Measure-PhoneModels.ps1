param([Parameter(Mandatory=$true)][string]$MetadataRoot)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
. (Join-Path $PSScriptRoot 'AppBenchmarkProcess.ps1')
. (Join-Path $PSScriptRoot 'AppBenchmarkAwake.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\')
$metadata=[IO.Path]::GetFullPath($MetadataRoot).TrimEnd('\')
$parent=Join-Path $env:LOCALAPPDATA 'DzLegalAssistant\alpha-03-owner-runs'
if($metadata -eq $repo -or $metadata.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase) -or $metadata.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase) -or $metadata.StartsWith('\\') -or -not(Test-Path -LiteralPath $metadata)){throw 'Use the existing separate local download-metadata folder.'}
$pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'PHONE-MODELS.json') -Raw|ConvertFrom-Json
$jobs=@(
    @{model='gemma4:e2b';think='false';folder='hidden-v2-alpha01-gemma4-e2b-think-off-2026-11-09';console='gemma4-e2b-console-2026-11-09.txt'},
    @{model='gemma3n:e2b';think=$null;folder='hidden-v2-alpha01-gemma3n-e2b-2026-11-09';console='gemma3n-e2b-console-2026-11-09.txt'}
)
foreach($job in $jobs){
    $pin=@($pins.models|Where-Object model -eq $job.model)
    if($pin.Count -ne 1 -or $pin[0].digest -notmatch '^[a-f0-9]{64}$'){throw 'Missing recorded model digest.'}
    $job.digest=$pin[0].digest
    $job.output=Join-Path $parent $job.folder
    $job.consolePath=Join-Path $parent $job.console
    if((Test-Path -LiteralPath $job.output) -or (Test-Path -LiteralPath $job.consolePath)){throw 'Fresh output and console paths required; never retry an existing run.'}
}
New-Item -ItemType Directory -Path $parent -Force|Out-Null
$searchOriginal=Get-CimInstance Win32_Service -Filter "Name='WSearch'"
$paused=$false
$pauseAttempted=$false
$awakePrevious=Enter-AppBenchmarkAwake
$started=[DateTimeOffset]::UtcNow.ToString('o')
$results=@()
$batchCode=0
try{
    foreach($job in $jobs){
        Assert-AlphaPower|Out-Null
        if(@(Get-Process -Name brave,chrome,msedge,firefox,Code,dotnet,testhost,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue).Count){throw 'Close browsers/editors and finish build/test work before timing.'}
        if(-not @(Get-Process -Name DzLegalAssistant -ErrorAction SilentlyContinue).Count){throw 'Idle template-mode app must remain open.'}
        $searchCores=Get-AppSearchActivity
        if($searchCores -gt .5 -and (Get-Service WSearch).Status -eq 'Running'){
            $pauseAttempted=$true
            Stop-Service WSearch -ErrorAction Stop
            (Get-Service WSearch).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(20))
            $paused=$true
        }
        $arguments=@('-NoProfile','-ExecutionPolicy','RemoteSigned','-File',(Join-Path $PSScriptRoot 'Run-OwnerAlpha01.ps1'),'-Model',$job.model,'-ExpectedDigest',$job.digest,'-OutputRoot',$job.output)
        if($job.think){$arguments+=@('-Think',$job.think)}
        $runStarted=[DateTimeOffset]::UtcNow.ToString('o')
        Write-Output ($job.model+' started '+$runStarted)
        # The entire child console is private. Never read it or any case output here.
        & powershell.exe @arguments *> $job.consolePath
        $runCode=$LASTEXITCODE
        if($null -eq $runCode){throw 'Wrapper exit code unavailable; do not continue.'}
        $runEnded=[DateTimeOffset]::UtcNow.ToString('o')
        $results += [pscustomobject]@{model=$job.model;digest=$job.digest;think=$(if($job.think){'false'}else{'omitted'});startedUtc=$runStarted;endedUtc=$runEnded;exitCode=$runCode;searchCoresBefore=$searchCores;searchPaused=$paused}
        ConvertTo-Json -InputObject @($results) -Depth 4|Set-Content -LiteralPath (Join-Path $metadata 'phone-launch-completions.json') -Encoding utf8
        Write-Output ($job.model+' ended code '+$runCode+' '+$runEnded)
        if($runCode -ne 0){$batchCode=$runCode;break}
    }
}finally{
    try{
        Exit-AppBenchmarkAwake -Previous $awakePrevious
        @{startedUtc=$started;restoredUtc=[DateTimeOffset]::UtcNow.ToString('o');previous=$awakePrevious}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $metadata 'phone-awake-state.json') -Encoding utf8
    }finally{
        if($pauseAttempted -and $searchOriginal.State -eq 'Running'){
            $service=Get-Service WSearch
            if($service.Status -eq 'StopPending'){$service.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(20))}
            if((Get-Service WSearch).Status -eq 'Stopped'){Start-Service WSearch; (Get-Service WSearch).WaitForStatus('Running',[TimeSpan]::FromSeconds(20))}
        }
        $searchAfter=Get-CimInstance Win32_Service -Filter "Name='WSearch'"
        @{originalState=$searchOriginal.State;originalStartMode=$searchOriginal.StartMode;pauseAttempted=$pauseAttempted;paused=$paused;finalState=$searchAfter.State;finalStartMode=$searchAfter.StartMode}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $metadata 'phone-search-state.json') -Encoding utf8
    }
}
exit $batchCode
