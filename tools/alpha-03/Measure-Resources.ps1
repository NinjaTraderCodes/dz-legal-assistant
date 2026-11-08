param([Parameter(Mandatory=$true)][string]$RunRoot,[switch]$SharedAppend)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
if($SharedAppend){. (Join-Path $PSScriptRoot 'SharedResourceAppend.ps1')}
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class DzAlphaResourceNative {
  [StructLayout(LayoutKind.Sequential)] public class Memory {
    public uint length = (uint)Marshal.SizeOf(typeof(Memory)); public uint load;
    public ulong totalPhys, availPhys, totalPage, availPage, totalVirtual, availVirtual, availExtended;
  }
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool GlobalMemoryStatusEx([In, Out] Memory value);
}
"@
$previous = @{}
$previousTime = [DateTime]::UtcNow
$sampleIndex = 0
$paging = $null
while (-not (Test-Path -LiteralPath (Join-Path $RunRoot 'monitor.stop'))) {
  try {
  $now = [DateTime]::UtcNow
  if (($sampleIndex % 5) -eq 0) {
    $paging = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory | Select-Object PageReadsPersec,PagesInputPersec,PagesOutputPersec,AvailableBytes
  }
  $sampleIndex++
  $elapsed = ($now - $previousTime).TotalSeconds
  $processes = @(Get-Process -ErrorAction SilentlyContinue)
  $usage = @()
  foreach ($process in $processes) {
    if ($null -ne $process.CPU -and $previous.ContainsKey($process.Id) -and $elapsed -gt 0) {
      $cores = [Math]::Max([double]0, [double](($process.CPU - $previous[$process.Id]) / $elapsed))
      if ($process.ProcessName -ne 'Idle') { $usage += [pscustomobject]@{name=$process.ProcessName;id=$process.Id;cores=$cores} }
    }
  }
  $previous = @{}
  foreach ($process in $processes) { $previous[$process.Id] = $process.CPU }
  $previousTime = $now
  $memory = New-Object DzAlphaResourceNative+Memory
  if (-not [DzAlphaResourceNative]::GlobalMemoryStatusEx($memory)) { throw 'Memory query failed' }
  $models = @($processes | Where-Object ProcessName -match '^(ollama|llama-server|ollama_llama_server)$')
  $apps = @($processes | Where-Object ProcessName -eq 'DzLegalAssistant')
  $phasePath = Join-Path $RunRoot 'phase.txt'
  $phase = if (Test-Path -LiteralPath $phasePath) { [IO.File]::ReadAllText($phasePath).Trim() } else { 'preparation' }
  $other = @($usage | Where-Object name -notmatch '^(ollama|llama-server|ollama_llama_server|Idle|System|powershell)$')
  $record = [ordered]@{power=(Get-AlphaPower);utc=$now.ToString('o');phase=$phase;intervalSeconds=$elapsed;totalPhysicalBytes=$memory.totalPhys;availablePhysicalBytes=$memory.availPhys;memoryLoad=$memory.load;commitBytes=($memory.totalPage-$memory.availPage);commitLimitBytes=$memory.totalPage;powershellCores=(@($usage | Where-Object name -eq 'powershell') | Measure-Object cores -Sum).Sum;modelWorkingSetBytes=($models | Measure-Object WorkingSet64 -Sum).Sum;modelPrivateBytes=($models | Measure-Object PrivateMemorySize64 -Sum).Sum;appWorkingSetBytes=($apps | Measure-Object WorkingSet64 -Sum).Sum;modelProcesses=@($models | ForEach-Object { [pscustomobject]@{id=$_.Id;name=$_.ProcessName;workingSet=$_.WorkingSet64;peakWorkingSet=$_.PeakWorkingSet64;privateBytes=$_.PrivateMemorySize64} });otherCores=($other | Measure-Object cores -Sum).Sum;topOther=@($other | Sort-Object cores -Descending | Select-Object -First 3);monitorPid=$PID}
  $record.paging = $paging
  $record.pagingSampleUpdated = (($sampleIndex % 5) -eq 1)
  $line=$record | ConvertTo-Json -Depth 6 -Compress
  if($SharedAppend){Add-SharedResourceLine -Path (Join-Path $RunRoot 'resources.jsonl') -Line $line}
  else{Add-Content -LiteralPath (Join-Path $RunRoot 'resources.jsonl') -Value $line -Encoding utf8}
  } catch {
    Add-Content -LiteralPath (Join-Path $RunRoot 'monitor-errors.log') -Value ([DateTime]::UtcNow.ToString('o') + ' ' + $_.Exception.Message) -Encoding utf8
  }
  Start-Sleep -Seconds 2
}
