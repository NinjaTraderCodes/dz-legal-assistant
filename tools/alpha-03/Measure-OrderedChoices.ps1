param([Parameter(Mandatory=$true)][string]$RunRoot, [Parameter(Mandatory=$true)][string]$FixturePath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Power.ps1')
$initialPower = Assert-AlphaPower
$utf8 = New-Object Text.UTF8Encoding($false)
function Save-Json($Path, $Value) { [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 40), $utf8) }
function Post($Path, $Body) { Invoke-RestMethod -Uri ('http://127.0.0.1:11434/' + $Path) -Method Post -ContentType 'application/json' -Body ($utf8.GetBytes(($Body | ConvertTo-Json -Depth 40))) -MaximumRedirection 0 -TimeoutSec 300 }
if (-not (Test-Path -LiteralPath $FixturePath)) { throw 'The accepted gate-1 scalar wire fixture is required.' }
if ((Get-FileHash -LiteralPath $FixturePath -Algorithm SHA256).Hash -ne 'cb95460ba73413548a706f759a54d6f149eb7d70831355788a2508f159b29be3') { throw 'Fixture differs from the accepted gate-1 request.' }
$fixture = Get-Content -LiteralPath $fixturePath -Encoding utf8 -Raw | ConvertFrom-Json
$prefix = $fixture.messages[1].content.Substring(0, $fixture.messages[1].content.IndexOf('نوع الدعوى:'))
$manifest = Get-Content -LiteralPath (Join-Path $RunRoot 'case-manifest.json') -Encoding utf8 -Raw | ConvertFrom-Json
$inputs = Get-Content -LiteralPath (Join-Path $RunRoot 'development-inputs.json') -Encoding utf8 -Raw | ConvertFrom-Json
$catalog = (Get-Content -LiteralPath data/law/claim-elements.json -Encoding utf8 -Raw | ConvertFrom-Json).claims
if ($manifest.Count -ne 40 -or @($manifest.name | Select-Object -Unique).Count -ne 40 -or ($manifest | Measure-Object eventQuestions -Sum).Sum -ne 147) { throw 'Expected the original 40-case, 147-question development manifest.' }
foreach ($m in $manifest) {
  $inputCase = @($inputs | Where-Object name -eq $m.name)
  if ($inputCase.Count -ne 1 -or $inputCase[0].partition -ne 'development') { throw 'Every selected input must be an original development case.' }
}
$orders = @($manifest | ForEach-Object { [pscustomobject]@{name=$_.name;reverse=$false} })
$reverseNames = @('loan-no-section','loan-basic','non-delivery-machines')
$orders += @($reverseNames | ForEach-Object { [pscustomobject]@{name=$_;reverse=$true} })
Save-Json (Join-Path $RunRoot 'gate2-plan.json') ([ordered]@{power=$initialPower;fixture=$fixturePath;fixtureSha256=(Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash;cases=40;eventCalls=147;reverseNames=$reverseNames;noNewPrompt=$true;freshCaseReset='explicit unload then empty CPU/context-matched warm-up, separate load cost';orders=$orders})
$rows = @()
foreach ($entry in $orders) {
  $casePower = Assert-AlphaPower
  $m = $manifest | Where-Object name -eq $entry.name
  $inputCase = $inputs | Where-Object name -eq $entry.name
  $claim = $catalog | Where-Object claim -eq $m.claim
  $label = if ($entry.reverse) { 'reverse' } else { 'forward' }
  $phase = 'gate2-' + $label + '-' + $entry.name
  Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value $phase -Encoding utf8
  $folder = Join-Path $RunRoot $phase
  New-Item -ItemType Directory -Path $folder | Out-Null
  $unload = Post 'api/generate' @{model='gemma3:4b';keep_alive=0}
  Save-Json (Join-Path $folder 'unload.json') $unload
  Save-Json (Join-Path $folder 'power.json') $casePower
  $loadClock = [Diagnostics.Stopwatch]::StartNew()
  $warm = Post 'api/generate' @{model='gemma3:4b';keep_alive='15m';options=@{num_ctx=4096;num_gpu=0}}
  $loadClock.Stop()
  Save-Json (Join-Path $folder 'warmup.json') $warm
  $lines = @($inputCase.sentences | ForEach-Object {
    $fieldLabel = switch ($_.field) { 'claim.subject' {'الموضوع'}; 'notes.facts' {'الوقائع'}; 'notes.requests' {'الطلبات'}; 'documents.list' {'المستندات'}; default {$_.field} }
    "$($_.id) [$fieldLabel] $($_.text)"
  })
  $common = $prefix + "نوع الدعوى: $($m.claim)`nنص الدعوى:`n" + ($lines -join "`n") + "`n"
  $elements = @($claim.elements)
  if ($entry.reverse) { [array]::Reverse($elements) }
  $history = @(@{role='system';content=''})
  $chainStartedUtc = [DateTime]::UtcNow.ToString('o')
  $caseClock = [Diagnostics.Stopwatch]::StartNew()
  $callRows = @()
  for ($q=0; $q -lt $elements.Count; $q++) {
    $element = $elements[$q]
    $question = "`nالسؤال: $($element.label)`nالجواب:"
    $content = if ($q -eq 0) { $common + $question } else { $question }
    $history += @{role='user';content=$content}
    $request = Get-Content -LiteralPath $fixturePath -Encoding utf8 -Raw | ConvertFrom-Json
    $request.messages = $history
    $stem = 'q' + ($q+1)
    Save-Json (Join-Path $folder ($stem + '-request.json')) $request
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $response = Post 'api/chat' $request
    $clock.Stop()
    Save-Json (Join-Path $folder ($stem + '-response.json')) $response
    $records = @($response.logprobs)
    $finite = @()
    $scores = [ordered]@{}
    foreach ($code in @('1','2','3','4')) {
      $alternative = @($records[0].top_logprobs | Where-Object { $_.token -ceq $code -and @($_.bytes).Count -eq 1 -and $_.bytes[0] -eq (48 + [int]$code) })
      if ($alternative.Count -eq 1 -and $null -ne $alternative[0].logprob -and -not [double]::IsNaN([double]$alternative[0].logprob) -and -not [double]::IsInfinity([double]$alternative[0].logprob)) {
        $finite += $code
        $scores[$code] = [double]$alternative[0].logprob
      }
    }
    $validToken = $records.Count -eq 1 -and $response.message.content -cmatch '^[1234]$' -and $records[0].token -ceq $response.message.content -and @($records[0].bytes).Count -eq 1
    $argmax = if ($finite.Count -eq 4) { ($scores.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key } else { $null }
    $row = [pscustomobject]@{case=$entry.name;order=$label;index=$q+1;event=$element.event;sampled=$response.message.content;argmax=$argmax;validSingleToken=$validToken;finiteCodes=$finite;scores=$scores;promptTokens=$response.prompt_eval_count;cachedTokens=$response.prompt_eval_cached_count;promptSeconds=$response.prompt_eval_duration/1e9;loadSeconds=$response.load_duration/1e9;writeSeconds=$response.eval_duration/1e9;answerTokens=$response.eval_count;wallSeconds=$clock.Elapsed.TotalSeconds;totalSeconds=$response.total_duration/1e9;doneReason=$response.done_reason}
    $callRows += $row
    Add-Content -LiteralPath (Join-Path $RunRoot 'calls.jsonl') -Value ($row | ConvertTo-Json -Depth 8 -Compress) -Encoding utf8
    $history += @{role='assistant';content=[string]$response.message.content}
    if (-not $validToken) { throw "Invalid scalar token at $phase/$stem; stop the probe" }
    if ($q -eq 0 -and $response.prompt_eval_cached_count -ne 0) { throw "Fresh case unexpectedly reused tokens at $phase" }
    if ($response.prompt_eval_count -ge 4096) { throw "Context limit reached at $phase/$stem" }
  }
  $caseClock.Stop()
  $chainFinishedUtc = [DateTime]::UtcNow.ToString('o')
  Save-Json (Join-Path $folder 'power-after.json') (Assert-AlphaPower)
  $caseRow = [pscustomobject]@{name=$entry.name;claim=$m.claim;order=$label;notesCharacters=$m.notesCharacters;questions=$elements.Count;warmupWallSeconds=$loadClock.Elapsed.TotalSeconds;chainWallSeconds=$caseClock.Elapsed.TotalSeconds;httpWallSeconds=($callRows | Measure-Object wallSeconds -Sum).Sum;maxPromptTokens=($callRows | Measure-Object promptTokens -Maximum).Maximum;firstCachedTokens=$callRows[0].cachedTokens;completeScoreSets=@($callRows | Where-Object {$_.finiteCodes.Count -eq 4}).Count;calls=$callRows}
  $rows += $caseRow
  $caseRow | Add-Member -NotePropertyName chainStartedUtc -NotePropertyValue $chainStartedUtc
  $caseRow | Add-Member -NotePropertyName chainFinishedUtc -NotePropertyValue $chainFinishedUtc
  Save-Json (Join-Path $RunRoot 'gate2-results.json') $rows
  Write-Output ("{0} {1}: {2} questions, {3:F3}s chain, {4:F3}s cold warm-up, {5}/{2} complete score sets, max {6} prompt tokens" -f $label,$entry.name,$elements.Count,$caseRow.chainWallSeconds,$caseRow.warmupWallSeconds,$caseRow.completeScoreSets,$caseRow.maxPromptTokens)
}
Set-Content -LiteralPath (Join-Path $RunRoot 'phase.txt') -Value 'gate2-complete' -Encoding utf8
