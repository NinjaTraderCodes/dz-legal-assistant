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
