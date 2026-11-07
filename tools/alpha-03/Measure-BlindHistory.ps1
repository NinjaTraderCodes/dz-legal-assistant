param([Parameter(Mandatory=$true)][string]$RunRoot, [Parameter(Mandatory=$true)][string]$PriorRoot, [switch]$AllowBatteryDiagnostic)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$utf8 = New-Object Text.UTF8Encoding($false)
function Save-Json($Path, $Value) { [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 45), $utf8) }
function Post($Path, $Body) { Invoke-RestMethod -Uri ('http://127.0.0.1:11434/' + $Path) -Method Post -ContentType 'application/json' -Body ($utf8.GetBytes(($Body | ConvertTo-Json -Depth 45))) -MaximumRedirection 0 -TimeoutSec 300 }
function Probe-Power {
    if (-not $AllowBatteryDiagnostic) { return Assert-AlphaPower }
    $state = Get-AlphaPower
    if ($state.acLineStatus -notin @(0,1)) { throw 'Unknown power source.' }
    return $state
}
if ((Get-FileHash -LiteralPath (Join-Path $PriorRoot 'case-manifest.json') -Algorithm SHA256).Hash -ne '9ff1cda372f8c9846d918b2ebc7b86945f93084a1536d11e1ab8dd1153f0581c') { throw 'The accepted original-development run is required.' }
$prior = Get-Content -LiteralPath (Join-Path $PriorRoot 'gate2-results.json') -Encoding utf8 -Raw | ConvertFrom-Json
$names = @('loan-no-section','loan-basic','non-delivery-machines')
$plan = @($names | ForEach-Object { [pscustomobject]@{arm='repeat';order='forward';name=$_} })
$plan += @($names | ForEach-Object { [pscustomobject]@{arm='blind';order='forward';name=$_} })
$plan += @($names | ForEach-Object { [pscustomobject]@{arm='blind';order='reverse';name=$_} })
Save-Json (Join-Path $RunRoot 'blind-history-plan.json') ([ordered]@{power=(Probe-Power);batteryDiagnostic=[bool]$AllowBatteryDiagnostic;timingQualification=$(if($AllowBatteryDiagnostic){'informational seconds only; no cost qualification'}else{'mains and Best performance required'});priorRoot=$PriorRoot;entries=$plan;plannedStatusPositions=51;firstNeutral='null';fallback='0 only if null is not a usable single-token constant or its schema is rejected';stop='missing scores, invalid token or lost extension reuse stop that arm';promptChanges='none; reuse accepted question messages';history='actual generated answers, never argmaxes or old-run answers'})
$script:results = @()

function Invoke-Chain($Entry, [string]$Mode) {
    $power = Probe-Power
    $old = $prior | Where-Object { $_.name -eq $Entry.name -and $_.order -eq $Entry.order }
    $source = Join-Path $PriorRoot ('gate2-'+$Entry.order+'-'+$Entry.name)
    $first = Get-Content -LiteralPath (Join-Path $source 'q1-request.json') -Encoding utf8 -Raw | ConvertFrom-Json
    $phase = $Entry.arm+'-'+$Entry.order+'-'+$Entry.name
    if ($Entry.arm -eq 'blind') { $phase += '-'+$Mode }
    Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value $phase -Encoding utf8
    $folder = Join-Path $RunRoot $phase
    New-Item -ItemType Directory -Path $folder | Out-Null
    Save-Json (Join-Path $folder 'power.json') $power
    Save-Json (Join-Path $folder 'unload.json') (Post 'api/generate' @{model='gemma3:4b';keep_alive=0})
    $loadClock = [Diagnostics.Stopwatch]::StartNew()
    $warmup = Post 'api/generate' @{model='gemma3:4b';keep_alive='15m';options=@{num_ctx=4096;num_gpu=0}}
    $loadClock.Stop()
    Save-Json (Join-Path $folder 'warmup.json') $warmup
    $history = $first.messages
    $startedUtc = [DateTime]::UtcNow.ToString('o')
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $calls = @()
    for ($q=1; $q -le $old.questions; $q++) {
        $template = Get-Content -LiteralPath (Join-Path $source ('q'+$q+'-request.json')) -Encoding utf8 -Raw | ConvertFrom-Json
        if ($q -gt 1) { $history += [pscustomobject]@{role='user';content=[string]$template.messages[-1].content} }
        $request = Get-Content -LiteralPath (Join-Path $source 'q1-request.json') -Encoding utf8 -Raw | ConvertFrom-Json
        $request.messages = $history
        if ($Entry.arm -eq 'blind') {
            $request.format = if ($Mode -eq 'null') { @{type='null'} } else { @{type='integer';enum=@(0)} }
        }
        $stem = 'q'+$q
        Save-Json (Join-Path $folder ($stem+'-request.json')) $request
        $callClock = [Diagnostics.Stopwatch]::StartNew()
        try { $response = Post 'api/chat' $request }
        catch {
            Save-Json (Join-Path $folder ($stem+'-error.json')) @{message=$_.Exception.Message;detail=$_.ErrorDetails.Message}
            $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
            if ($Entry.arm -eq 'blind' -and $Mode -eq 'null' -and $q -eq 1 -and $status -in @(400,422) -and $_.ErrorDetails.Message -match 'schema|grammar|type|null') {
                return [pscustomobject]@{completed=$false;neutralUnusable=$true;reason='null schema rejected';folder=$folder}
            }
            throw
        }
        $callClock.Stop()
        Save-Json (Join-Path $folder ($stem+'-response.json')) $response
        $records = @($response.logprobs)
        $content = [string]$response.message.content
        $single = $records.Count -eq 1 -and $response.eval_count -eq 1 -and $records[0].token -ceq $content -and (($records[0].bytes -join ',') -ceq (($utf8.GetBytes($content)) -join ','))
        $expected = if ($Mode -eq 'null') { 'null' } else { '0' }
        $valid = if ($Entry.arm -eq 'blind') { $single -and $content -ceq $expected } else { $single -and $content -cmatch '^[1234]$' }
        $scores = [ordered]@{}
        foreach ($code in @('1','2','3','4')) {
            $value = @($records[0].top_logprobs | Where-Object { $_.token -ceq $code -and @($_.bytes).Count -eq 1 -and $_.bytes[0] -eq (48+[int]$code) })
            if ($value.Count -eq 1 -and $null -ne $value[0].logprob -and -not [double]::IsNaN([double]$value[0].logprob) -and -not [double]::IsInfinity([double]$value[0].logprob)) { $scores[$code] = [double]$value[0].logprob }
        }
        $argmax = if ($scores.Count -eq 4) { ($scores.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key } else { $null }
        $row = [pscustomobject]@{arm=$Entry.arm;order=$Entry.order;name=$Entry.name;mode=$Mode;index=$q;event=$old.calls[$q-1].event;output=$content;validSingleToken=$valid;scores=$scores;argmax=$argmax;promptTokens=$response.prompt_eval_count;cachedTokens=$response.prompt_eval_cached_count;promptSeconds=$response.prompt_eval_duration/1e9;writeSeconds=$response.eval_duration/1e9;wallSeconds=$callClock.Elapsed.TotalSeconds;doneReason=$response.done_reason}
        $calls += $row
        Add-Content -LiteralPath (Join-Path $RunRoot 'followup-calls.jsonl') -Value ($row | ConvertTo-Json -Depth 8 -Compress) -Encoding utf8
        if (-not $valid -and $Entry.arm -eq 'blind' -and $Mode -eq 'null' -and $q -eq 1) { return [pscustomobject]@{completed=$false;neutralUnusable=$true;reason='null did not form a single neutral token';folder=$folder} }
        if (-not $valid -or $scores.Count -ne 4) { throw ('Token/score precondition failed at '+$phase+'/'+$stem) }
        if (($q -eq 1 -and $row.cachedTokens -ne 0) -or ($q -gt 1 -and $row.cachedTokens -le 0)) { throw ('Cache precondition failed at '+$phase+'/'+$stem) }
        if ($row.promptTokens -ge 4096) { throw 'Context limit reached.' }
        $history += [pscustomobject]@{role='assistant';content=$content}
    }
    $clock.Stop()
    Save-Json (Join-Path $folder 'power-after.json') (Probe-Power)
    [Console]::WriteLine(('{0}: {1} questions, {2:F3}s, {3}/{1} four-score positions' -f $phase,$calls.Count,$clock.Elapsed.TotalSeconds,@($calls | Where-Object {$_.scores.Count -eq 4}).Count))
    return [pscustomobject]@{completed=$true;arm=$Entry.arm;order=$Entry.order;name=$Entry.name;mode=$Mode;questions=$calls.Count;startedUtc=$startedUtc;finishedUtc=[DateTime]::UtcNow.ToString('o');warmupSeconds=$loadClock.Elapsed.TotalSeconds;chainSeconds=$clock.Elapsed.TotalSeconds;folder=$folder;calls=$calls}
}

$neutral = 'null'
$neutralLocked = $false
foreach ($entry in $plan) {
    try {
        $mode = if ($entry.arm -eq 'repeat') { 'choices' } else { $neutral }
        $result = Invoke-Chain $entry $mode
        if (-not $result.completed -and $result.neutralUnusable -and -not $neutralLocked) {
            Save-Json (Join-Path $RunRoot 'neutral-fallback.json') $result
            $neutral = 'zero'
            $result = Invoke-Chain $entry $neutral
        }
        if (-not $result.completed) { throw 'No usable single-token neutral constant.' }
        if ($entry.arm -eq 'blind') { $neutralLocked = $true }
        $script:results += $result
        Save-Json (Join-Path $RunRoot 'followup-chains.json') $script:results
    }
    catch {
        Save-Json (Join-Path $RunRoot 'chain-precondition-stop.json') @{arm=$entry.arm;name=$entry.name;order=$entry.order;message=$_.Exception.Message}
        throw
    }
}
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value 'chain-followup-complete' -Encoding utf8
