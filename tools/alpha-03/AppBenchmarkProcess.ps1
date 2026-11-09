function Get-AppBenchmarkPlan {
    param([ValidateSet('all','grounds','draft')][string]$Stage='all',[switch]$Gemma4First)
    $commands=if($Stage -eq 'all'){@('grounds','draft')}else{@($Stage)}
    $modelOrder=if($Gemma4First){@('gemma4:e4b','gemma3:4b')}else{@('gemma3:4b','gemma4:e4b')}
    foreach($command in $commands){foreach($model in $modelOrder){[pscustomobject]@{Command=$command;Model=$model}}}
}

function Get-AppSearchActivity {
    $before=@{}
    Get-Process -Name SearchIndexer,SearchProtocolHost,SearchFilterHost -ErrorAction SilentlyContinue|ForEach-Object {$before[$_.Id]=[double]$_.CPU}
    $watch=[Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds 5
    $cpu=0.0
    Get-Process -Name SearchIndexer,SearchProtocolHost,SearchFilterHost -ErrorAction SilentlyContinue|ForEach-Object {if($before.ContainsKey($_.Id)){$cpu += [Math]::Max(0,[double]$_.CPU-$before[$_.Id])}}
    $cpu/$watch.Elapsed.TotalSeconds
}

function Start-AppBenchmarkProcess {
    param([string]$FilePath,[string]$Arguments,[string]$WorkingDirectory,[string]$Stdout,[string]$Stderr)
    $child=Start-Process -FilePath $FilePath -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput $Stdout -RedirectStandardError $Stderr
    # Keep the native handle before HasExited polling: Windows PowerShell otherwise may lose ExitCode.
    $null=$child.Handle
    $child
}

function Get-AppBenchmarkExitCode {
    param([System.Diagnostics.Process]$Process)
    $Process.WaitForExit()
    $Process.Refresh()
    if($null -eq $Process.ExitCode){throw 'Native exit code unavailable; do not record success or continue.'}
    [int]$Process.ExitCode
}
