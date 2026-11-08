param([Parameter(Mandatory=$true)][ValidateSet('gemma3:4b','gemma4:e4b')][string]$Model,
      [Parameter(Mandatory=$true)][string]$OutputRoot,
      [ValidateSet('false')][string]$Think)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\')
$output=[IO.Path]::GetFullPath($OutputRoot).TrimEnd('\')
$privateCases='C:\dz-hidden-v2'
if($output -eq $repo -or $output.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase) -or
   $output -eq $privateCases -or $output.StartsWith($privateCases+'\',[StringComparison]::OrdinalIgnoreCase) -or $output.StartsWith('\\')){
    throw 'GUARD output must be a local folder outside the repository and case folder.'
}
if(Test-Path -LiteralPath $output){throw 'GUARD use a fresh output folder; no overwrite or automatic retry.'}
if(($Model -eq 'gemma3:4b' -and $Think) -or ($Model -eq 'gemma4:e4b' -and $Think -ne 'false')){throw 'GUARD baseline omits --think; frozen gemma4 requires -Think false.'}
$binary=Join-Path $repo 'src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll'
if(-not(Test-Path -LiteralPath $binary)){throw 'GUARD build the frozen Release harness before running.'}
Assert-AlphaPower|Out-Null
if(@(Get-Process -Name brave,chrome,msedge,firefox,Code -ErrorAction SilentlyContinue).Count){throw 'GUARD owner must close browsers and VS Code.'}
if(-not @(Get-Process -Name DzLegalAssistant -ErrorAction SilentlyContinue).Count){throw 'GUARD open the desktop app and leave it idle.'}
New-Item -ItemType Directory -Path $output|Out-Null
$monitorScript=Join-Path $PSScriptRoot 'Measure-Resources.ps1'
$monitor=Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','RemoteSigned','-File',('"'+$monitorScript+'"'),'-RunRoot',('"'+$output+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'monitor-stdout.txt') -RedirectStandardError (Join-Path $output 'monitor-stderr.txt')
$code=3
try {
    $ready=$false
    for($attempt=0;$attempt -lt 60;$attempt++){
        Start-Sleep -Seconds 2
        Assert-AlphaPower|Out-Null
        $resource=Join-Path $output 'resources.jsonl'
        if(-not(Test-Path -LiteralPath $resource)){continue}
        $samples=@(Get-Content -LiteralPath $resource -Tail 6|ForEach-Object{try{$_|ConvertFrom-Json}catch{}}|Where-Object intervalSeconds -gt 1|Select-Object -Last 5)
        if($samples.Count -eq 5 -and ($samples|Measure-Object otherCores -Average).Average -le .5){$ready=$true;break}
        if($attempt % 5 -eq 0){Write-Output 'GUARD waiting for quiet background CPU.'}
    }
    if(-not $ready){throw 'GUARD quiet preflight not met; harness not started.'}
    if(@(Get-Process -Name brave,chrome,msedge,firefox,Code -ErrorAction SilentlyContinue).Count){throw 'GUARD browser/editor reopened; harness not started.'}
    Write-Output 'GUARD mains, Best performance, idle app and quiet preflight ready.'
    # Only the harness receives the fixed case path. This wrapper never opens it or consumes case/answer output.
    $harnessArgs=@($binary,'facts','--alpha','--claims','given','--models',$Model,'--cases',$privateCases,
        '--law-root',(Join-Path $repo 'data/law'),'--out',$output,'--measurement-root',$output,'--endpoint','http://127.0.0.1:11434')
    if($Think){$harnessArgs+=@('--think',$Think)}
    $argumentLine=($harnessArgs|ForEach-Object{'"'+$_+'"'})-join ' '
    # Inherit this console for the harness's counts/guard events; stderr goes directly to a private file, unread here.
    $process=Start-Process -FilePath dotnet.exe -ArgumentList $argumentLine -WorkingDirectory $repo -NoNewWindow -Wait -PassThru -RedirectStandardError (Join-Path $output 'harness-stderr.txt')
    $code=$process.ExitCode
    Write-Output ('GUARD harness exited with code '+$code+'.')
} finally {
    Set-Content -LiteralPath (Join-Path $output 'monitor.stop') -Value 'Owner command ended' -Encoding utf8
    if($monitor -and -not $monitor.HasExited){$monitor.WaitForExit(10000)|Out-Null}
}
exit $code
