# Owner-assisted M5 acceptance only. No model request, user case-file access or authentication.
[CmdletBinding()]
param(
    [ValidateSet('FirstOnline', 'ContinueOnline', 'Offline')][string]$Phase = 'FirstOnline',
    [string]$PackageFolder = 'C:\Users\Public\Documents\DzLegalAssistant-M5-c0486b9',
    [ValidatePattern('^[0-9a-fA-F]{64}$')][string]$PackageSha256 = '716c1306d55a5420a996d4cdff24e9a70665036fbbe78b3f588d8a38dcc0fa89',
    [ValidatePattern('^[A-Za-z0-9-]+$')][string]$InstallationName = 'DzLegalAssistant-M5-c0486b9',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$handoff = [IO.Path]::GetFullPath($PackageFolder)
$archive = Join-Path $handoff 'DzLegalAssistant-0.10.0-win-x64.zip'
$expectedHash = $PackageSha256
$install = Join-Path (Join-Path $env:LOCALAPPDATA 'Programs') $InstallationName
$appRoot = Join-Path $env:LOCALAPPDATA 'DzLegalAssistant'

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class M5AcceptanceResources {
 [StructLayout(LayoutKind.Sequential)] public struct Power { public byte Ac, Flags, Percent, Reserved; public uint Life, FullLife; }
 [StructLayout(LayoutKind.Sequential)] public struct Memory { public uint Length, Load; public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtended; }
 [DllImport("kernel32.dll")] public static extern bool GetSystemPowerStatus(out Power value);
 [DllImport("kernel32.dll")] public static extern bool GlobalMemoryStatusEx(ref Memory value);
 [DllImport("kernel32.dll")] public static extern bool GetPhysicallyInstalledSystemMemory(out ulong value);
 [DllImport("powrprof.dll")] public static extern uint PowerGetActualOverlayScheme(out Guid value);
 [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
}
"@
function Test-Power {
    $p = New-Object M5AcceptanceResources+Power
    $scheme = [Guid]::Empty
    return ([M5AcceptanceResources]::GetSystemPowerStatus([ref]$p) -and $p.Ac -eq 1 -and
        [M5AcceptanceResources]::PowerGetActualOverlayScheme([ref]$scheme) -eq 0 -and
        $scheme -eq [Guid]'ded574b5-45a0-4f42-8737-46345c09c238')
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Package hash mismatch. Stop.' }
if ($ValidateOnly) { 'PASS: package hash checked; app not launched.'; exit 0 }
if (-not (Test-Power)) { throw 'Mains and Best performance are required.' }
if (@(Get-Process -Name brave,chrome,msedge,firefox,opera,vivaldi,Code -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'Close browsers and VS Code before this check.'
}
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$standard = -not (@($identity.Groups | ForEach-Object { $_.Value }) -contains 'S-1-5-32-544')
if (-not $standard) { throw 'Use the new Standard Windows account, not an administrator account.' }
if (@(Get-Process -Name DzLegalAssistant,ollama -ErrorAction SilentlyContinue).Count -gt 0 -or
    @(Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'Close existing app and Ollama instances before this check.'
}
$freshRoot = -not (Test-Path -LiteralPath $appRoot)
$normalOllamaAbsent = -not (Test-Path -LiteralPath (Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama.exe'))
if ($Phase -eq 'FirstOnline') {
    if (-not $freshRoot -or -not $normalOllamaAbsent -or (Test-Path -LiteralPath $install)) {
        throw 'This account is not fresh for the first-start check. Nothing was changed.'
    }
    Expand-Archive -LiteralPath $archive -DestinationPath $install
} elseif ($Phase -eq 'ContinueOnline') {
    if ($freshRoot -or -not $normalOllamaAbsent) { throw 'Continuation requires the existing standard-account app data and no separate Ollama installation.' }
    if (-not (Test-Path -LiteralPath $install)) { Expand-Archive -LiteralPath $archive -DestinationPath $install }
}
# Reuse only this exact package. Never overwrite an installation or touch its model/draft data.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    foreach ($entry in $zip.Entries) {
        if ([string]::IsNullOrEmpty($entry.Name)) { continue }
        $target = [IO.Path]::GetFullPath((Join-Path $install $entry.FullName))
        $prefix = [IO.Path]::GetFullPath($install) + [IO.Path]::DirectorySeparatorChar
        if (-not $target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid packaged path.' }
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or (Get-Item -LiteralPath $target).Length -ne $entry.Length) {
            throw 'Installation does not match this package. Nothing was overwritten.'
        }
        $source = $entry.Open()
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $expected = [BitConverter]::ToString($hash.ComputeHash($source)).Replace('-','') }
        finally { $source.Dispose(); $hash.Dispose() }
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) { throw 'Installed package check failed.' }
    }
} finally { $zip.Dispose() }
if (Test-Path -LiteralPath (Join-Path $install 'appsettings.local.json')) { throw 'Personal overrides are not allowed in the acceptance package.' }
$exe = Join-Path $install 'DzLegalAssistant.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'The acceptance package is not installed in this account.' }
$installedKiB = [UInt64]0
if (-not [M5AcceptanceResources]::GetPhysicallyInstalledSystemMemory([ref]$installedKiB)) { throw 'Installed RAM check failed.' }
$events = [Collections.Generic.List[object]]::new()
$samples = [Collections.Generic.List[object]]::new()
$recordPath = Join-Path $handoff ('M5-' + $Phase + '-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N') + '.json')
$record = [ordered]@{
    schema = 1; phase = $Phase; packageSha256 = $expectedHash.ToLowerInvariant()
    standardAccount = $standard; freshAppDataAtStart = $freshRoot; normalOllamaAbsent = $normalOllamaAbsent
    installedMemoryBytes = $installedKiB * 1024; powerMode = 'Best performance'; acAtStart = $true
    sampleIntervalSeconds = 5; networkObservation = 'Sampled owned-process connection counts only; not packet capture'
    startUtc = [DateTime]::UtcNow.ToString('o'); endUtc = $null; exitCode = $null
    peakWorkingSetBytes = 0L; peakPrivateBytes = 0L; minimumAvailableMemoryBytes = [long]::MaxValue
    events = $events; samples = $samples
}
function Save-Record {
    [IO.File]::WriteAllText($recordPath, ($record | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
}
$markers = @{
    I='idle-before-download'; D='download-click'; C='cancelled'; R='retry-click'; T='templates-during-transfer'
    V='model-ready'; A='first-analysis-complete'; E='pack-export-complete'; O='offline-import-complete'; F='functional-failure'
}
'Only use fictitious built-in samples. No model download is started by this launcher.'
'Optional milestone keys, pressed in THIS console after each app action:'
'I idle; D Download; C cancelled; R Retry; T templates during transfer; V ready; A analysis; E export; O offline import; F failure.'
'Close the app normally when finished. Keep this console open until it records the result.'
if ([M5AcceptanceResources]::SetThreadExecutionState([uint32]2147483651) -eq 0) { throw 'Cannot prevent automatic sleep. App not started.' }
try { $app = Start-Process -FilePath $exe -WorkingDirectory $install -PassThru }
catch { [void][M5AcceptanceResources]::SetThreadExecutionState([uint32]2147483648); throw 'App launch failed.' }
$owned = @{}
$owned[$app.Id] = $app.StartTime.ToUniversalTime().Ticks
$nextSample = [DateTime]::UtcNow
try {
    while (-not $app.HasExited) {
        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true).KeyChar.ToString().ToUpperInvariant()
            if ($markers.ContainsKey($key)) {
                $events.Add([ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); event=$markers[$key] })
                'Recorded: ' + $markers[$key]
                Save-Record
            }
        }
        if ([DateTime]::UtcNow -ge $nextSample) {
            if (-not (Test-Power)) {
                $events.Add([ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); event='measurement-stopped-power-condition' })
                Write-Warning 'Power condition changed. Cancel any download in the app. Measurements have stopped; the app is left open.'
                break
            }
            if (@(Get-Process -Name brave,chrome,msedge,firefox,opera,vivaldi,Code -ErrorAction SilentlyContinue).Count -gt 0) {
                $events.Add([ordered]@{utc=[DateTime]::UtcNow.ToString('o');event='measurement-stopped-browser-or-vscode'})
                Write-Warning 'A browser or VS Code opened. Measurements have stopped; close the app normally and report this.'
                break
            }
            # Walk only descendants of the launched app; never inspect command lines or other users' files.
            $queue = [Collections.Generic.Queue[int]]::new()
            $queue.Enqueue($app.Id)
            $active = [Collections.Generic.List[object]]::new()
            $visited = [Collections.Generic.HashSet[int]]::new()
            while ($queue.Count -gt 0) {
                $processId = $queue.Dequeue()
                if (-not $visited.Add($processId)) { continue }
                $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
                if ($null -eq $process) { continue }
                $ticks = $process.StartTime.ToUniversalTime().Ticks
                if ($owned.ContainsKey($processId) -and $owned[$processId] -ne $ticks) { continue }
                $owned[$processId] = $ticks
                $active.Add($process)
                foreach ($child in @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $processId" -Property ProcessId)) {
                    $queue.Enqueue([int]$child.ProcessId)
                }
            }
            $working = 0L; $private = 0L
            foreach ($process in $active) { $working += $process.WorkingSet64; $private += $process.PrivateMemorySize64 }
            $memory = New-Object M5AcceptanceResources+Memory
            $memory.Length = [Runtime.InteropServices.Marshal]::SizeOf($memory)
            if (-not [M5AcceptanceResources]::GlobalMemoryStatusEx([ref]$memory)) { throw 'Memory counter unavailable.' }
            $externalCount = $null; $connectionCheck = 'unavailable'
            try {
                $connections = @(Get-NetTCPConnection -ErrorAction Stop |
                    Where-Object { $visited.Contains([int]$_.OwningProcess) -and $_.State -in @('Established','SynSent') -and $_.RemoteAddress -notmatch '^(127\.|::1$|0\.0\.0\.0$|::$)' })
                $externalCount = $connections.Count; $connectionCheck = 'sampled'
            } catch { }
            $freeDisk = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($appRoot)).AvailableFreeSpace
            $samples.Add([ordered]@{utc=[DateTime]::UtcNow.ToString('o'); workingSetBytes=$working; privateBytes=$private;
                availableMemoryBytes=$memory.AvailablePhysical; freeDiskBytes=$freeDisk; ownedProcessCount=$active.Count;
                externalConnectionCount=$externalCount; connectionCheck=$connectionCheck})
            $record.peakWorkingSetBytes = [Math]::Max($record.peakWorkingSetBytes, $working)
            $record.peakPrivateBytes = [Math]::Max($record.peakPrivateBytes, $private)
            $record.minimumAvailableMemoryBytes = [Math]::Min($record.minimumAvailableMemoryBytes, [long]$memory.AvailablePhysical)
            if ($memory.AvailablePhysical -lt 512MB) {
                $events.Add([ordered]@{utc=[DateTime]::UtcNow.ToString('o');event='measurement-stopped-512-MiB-floor'})
                Write-Warning 'Available memory is below 512 MiB. Cancel the operation in the app. The app is left open.'
                break
            }
            Save-Record
            $nextSample = [DateTime]::UtcNow.AddSeconds(5)
        }
        Start-Sleep -Milliseconds 200
        $app.Refresh()
    }
    if ($app.HasExited) { $app.WaitForExit(); $record.exitCode = $app.ExitCode }
} catch {
    $events.Add([ordered]@{utc=[DateTime]::UtcNow.ToString('o');event='monitor-failure';category=$_.Exception.GetType().Name})
    Write-Warning 'The acceptance monitor failed. Close the app normally and report that fact; no model process was killed.'
} finally {
    $record.endUtc = [DateTime]::UtcNow.ToString('o')
    Save-Record
    [void][M5AcceptanceResources]::SetThreadExecutionState([uint32]2147483648)
    'Acceptance record saved in the public M5 handoff folder. No case text was recorded.'
}