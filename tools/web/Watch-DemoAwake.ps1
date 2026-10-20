<#
.SYNOPSIS
  Watches the public web demo and says whether it stayed awake (the uptime monitor's pings work).

.DESCRIPTION
  Every few minutes it asks the demo's /healthz, as a visitor would, and writes one line: the time, how long the answer
  took, and when the server last started. On Render's free plan a server that went to sleep starts again on the next
  visit: its start time changes and that visit takes about a minute. If the start time never changes and every answer is
  quick, the pings keep the demo awake. Nothing is sent but the request itself; no key is needed.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\web\Watch-DemoAwake.ps1
  powershell -ExecutionPolicy Bypass -File tools\web\Watch-DemoAwake.ps1 -Hours 6 -EveryMinutes 20
#>
param(
    [string]$Url = "https://dz-legal-assistant-ai.onrender.com",
    # Longer than Render's 15 idle minutes, so this script's own visits do not keep the demo awake on their own.
    [int]$EveryMinutes = 20,
    [double]$Hours = 3,
    # An answer slower than this is counted as a wake-up.
    [int]$SlowSeconds = 10
)

$ErrorActionPreference = "Stop"
$end = (Get-Date).AddHours($Hours)
$firstStart = $null
$restarts = 0
$slow = 0
$checks = 0

Write-Host "Watching $Url/healthz every $EveryMinutes minutes until $($end.ToString('HH:mm'))."
while ($true) {
    $clock = [Diagnostics.Stopwatch]::StartNew()
    try {
        $health = Invoke-RestMethod -Uri "$Url/healthz" -TimeoutSec 120
        $seconds = [math]::Round($clock.Elapsed.TotalSeconds, 1)
        $checks++
        $note = ""
        if ($null -eq $firstStart) { $firstStart = $health.started }
        elseif ($health.started -ne $firstStart) { $restarts++; $note = "  <- the server restarted (it slept, or a new deploy)"; $firstStart = $health.started }
        if ($seconds -gt $SlowSeconds) { $slow++; $note += "  <- slow answer (a wake-up?)" }
        Write-Host ("{0}  {1,5} s  started {2}  ai={3}{4}" -f (Get-Date).ToString("HH:mm"), $seconds, $health.started, $health.ai, $note)
    }
    catch {
        Write-Host ("{0}  no answer: {1}" -f (Get-Date).ToString("HH:mm"), $_.Exception.Message)
    }

    if ((Get-Date).AddMinutes($EveryMinutes) -gt $end) { break }
    Start-Sleep -Seconds ($EveryMinutes * 60)
}

Write-Host ""
if ($checks -eq 0) { Write-Host "No answer at all: check the address." }
elseif ($restarts -eq 0 -and $slow -eq 0) { Write-Host "Stayed awake: $checks checks, no restart, every answer under $SlowSeconds s. The pings work." }
else { Write-Host "Not reliably awake: $restarts restart(s) and $slow slow answer(s) in $checks checks. Check the monitor's interval and its URL." }
