param([Parameter(Mandatory=$true)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
$folder = Join-Path $RunRoot 'rung0-gemma4-e4b-think-off'
$cases = Get-Content -LiteralPath (Join-Path $folder 'case-results.json') -Encoding utf8 -Raw | ConvertFrom-Json
$completion = Get-Content -LiteralPath (Join-Path $folder 'completion.json') -Encoding utf8 -Raw | ConvertFrom-Json
$process = Get-Content -LiteralPath (Join-Path $folder 'process-completion.json') -Encoding utf8 -Raw | ConvertFrom-Json
if ($process.exitCode -ne 0 -or $cases.Count -ne $completion.AttemptedCases) { throw 'Incomplete or failed measurement process.' }
$requests = @(Get-ChildItem -LiteralPath $folder -Filter 'call-*-request.json' -File)
if ($requests.Count -ne $cases.Count) { throw 'Request count differs from attempted cases.' }
foreach ($file in $requests) {
    $r = Get-Content -LiteralPath $file.FullName -Encoding utf8 -Raw | ConvertFrom-Json
    if ($r.model -cne 'gemma4:e4b' -or $r.think -ne $false -or $r.stream -ne $true -or $r.options.num_predict -ne 300 -or $r.options.num_ctx -ne 4096 -or $r.options.num_gpu -ne 0) { throw 'Unexpected wire request.' }
}
$samples = @(Get-Content -LiteralPath (Join-Path $RunRoot 'resources.jsonl') -Encoding utf8 | ForEach-Object { $_ | ConvertFrom-Json } | Where-Object { $_.phase -eq 'rung0-gemma4-e4b-think-off' -and [DateTime]$_.utc -ge [DateTime]$process.startedUtc -and [DateTime]$_.utc -le [DateTime]$process.finishedUtc })
if ($samples.Count -eq 0) { throw 'No resource samples during the measurement.' }
$best = 'ded574b5-45a0-4f42-8737-46345c09c238'
$violations = @($samples | Where-Object { $_.power.acLineStatus -ne 1 -or $_.power.actualOverlayGuid -ne $best -or $_.power.effectiveOverlayGuid -ne $best -or $_.power.configuredAcGuid -ne $best })
$times = @($cases.Seconds | Sort-Object)
$median = if (($times.Count % 2) -eq 0) { ($times[$times.Count/2-1]+$times[$times.Count/2])/2 } else { $times[[int][Math]::Floor($times.Count/2)] }
$paging = @($samples | Where-Object pagingSampleUpdated)
$errors = @($cases.Errors | Where-Object { $null -ne $_ })
$errorCounts = @(foreach ($group in $errors | Group-Object Type,Critical) { [pscustomobject]@{type=$group.Group[0].Type;critical=$group.Group[0].Critical;occurrences=$group.Count} })
$result = [ordered]@{
    planned=$completion.PlannedCases;attempted=$cases.Count;unattempted=$completion.UnattemptedCases;stop=$completion.StopReason
    failures=@($cases | Where-Object Failure).Count;capHits=@($cases | Where-Object ReachedCap).Count;criticalCases=@($cases | Where-Object HasCritical).Count;errorTypes=$errorCounts
    wireRequestsChecked=$requests.Count;powerSamples=$samples.Count;powerViolations=$violations.Count
    meanSeconds=($times | Measure-Object -Average).Average;medianSeconds=$median;p95Seconds=$times[[Math]::Ceiling(0.95*$times.Count)-1];maxSeconds=$times[-1];batchSeconds=$completion.BatchSeconds;processSeconds=$process.wallSeconds;processSecondsPerAttempt=$process.wallSeconds/$cases.Count
    minAnswerTokens=($cases.Timings | Measure-Object AnswerTokens -Minimum).Minimum;maxAnswerTokens=($cases.Timings | Measure-Object AnswerTokens -Maximum).Maximum
    modelPeakGiB=($samples | Measure-Object modelWorkingSetBytes -Maximum).Maximum/1GB;modelAppPeakGiB=(@($samples | ForEach-Object {$_.modelWorkingSetBytes+$_.appWorkingSetBytes}) | Measure-Object -Maximum).Maximum/1GB;modelPrivatePeakGiB=($samples | Measure-Object modelPrivateBytes -Maximum).Maximum/1GB
    minAvailableGiB=($samples | Measure-Object availablePhysicalBytes -Minimum).Minimum/1GB;maxCommitGiB=($samples | Measure-Object commitBytes -Maximum).Maximum/1GB;totalPhysicalGiB=$samples[0].totalPhysicalBytes/1GB
    meanOtherCores=($samples | Measure-Object otherCores -Average).Average;maxOtherCores=($samples | Measure-Object otherCores -Maximum).Maximum
    pagingUpdates=$paging.Count;maxPageReads=($paging.paging | Measure-Object PageReadsPersec -Maximum).Maximum;maxPageOutputs=($paging.paging | Measure-Object PagesOutputPersec -Maximum).Maximum
    runnerIds=@($samples.modelProcesses | Where-Object name -ne 'ollama' | ForEach-Object {$_.id} | Sort-Object -Unique)
}
[IO.File]::WriteAllText((Join-Path $folder 'audit.json'), ($result | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding($false)))
$result | ConvertTo-Json -Depth 8
