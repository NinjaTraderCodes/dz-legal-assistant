param([Parameter(Mandatory=$true)][string]$RunRoot, [Parameter(Mandatory=$true)][string]$PriorRoot)
$ErrorActionPreference = 'Stop'
$chains = Get-Content -LiteralPath (Join-Path $RunRoot 'followup-chains.json') -Encoding utf8 -Raw | ConvertFrom-Json
$prior = Get-Content -LiteralPath (Join-Path $PriorRoot 'gate2-results.json') -Encoding utf8 -Raw | ConvertFrom-Json
$names = @('loan-no-section','loan-basic','non-delivery-machines')
if ($chains.Count -ne 9 -or ($chains | Measure-Object questions -Sum).Sum -ne 51) { throw 'Incomplete bounded probe.' }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 45 -Compress }
function Require($Condition, $Message) { if (-not $Condition) { throw $Message } }
$audit = @()
$maxScoreSerializationDifference = 0.0
foreach ($chain in $chains) {
    $source = Join-Path $PriorRoot ('gate2-'+$chain.order+'-'+$chain.name)
    $template = Get-Content -LiteralPath (Join-Path $source 'q1-request.json') -Encoding utf8 -Raw | ConvertFrom-Json
    $previous = $null
    for ($q=1; $q -le $chain.questions; $q++) {
        $request = Get-Content -LiteralPath (Join-Path $chain.folder ('q'+$q+'-request.json')) -Encoding utf8 -Raw | ConvertFrom-Json
        $response = Get-Content -LiteralPath (Join-Path $chain.folder ('q'+$q+'-response.json')) -Encoding utf8 -Raw | ConvertFrom-Json
        $oldQuestion = Get-Content -LiteralPath (Join-Path $source ('q'+$q+'-request.json')) -Encoding utf8 -Raw | ConvertFrom-Json
        $row = $chain.calls[$q-1]
        foreach ($property in $template.PSObject.Properties.Name | Where-Object { $_ -notin @('messages','format') }) {
            Require ((Json $request.$property) -ceq (Json $template.$property)) ('Changed option: '+$property)
        }
        $expectedFormat = if ($chain.arm -eq 'repeat') { $template.format } else { [pscustomobject]@{type='integer';enum=@(0)} }
        Require ((Json $request.format) -ceq (Json $expectedFormat)) 'Changed output schema.'
        if ($q -eq 1) {
            Require ((Json $request.messages) -ceq (Json $template.messages)) 'Changed initial messages.'
        } else {
            Require ($request.messages.Count -eq ($previous.messages.Count+2)) 'History length mismatch.'
            Require ((Json @($request.messages | Select-Object -First $previous.messages.Count)) -ceq (Json $previous.messages)) 'Earlier messages changed.'
            Require ($request.messages[-2].role -ceq 'assistant' -and $request.messages[-2].content -ceq $chain.calls[$q-2].output) 'History is not the actual previous answer.'
        }
        Require ($request.messages[-1].role -ceq $oldQuestion.messages[-1].role -and $request.messages[-1].content -ceq $oldQuestion.messages[-1].content -and @($request.messages[-1].PSObject.Properties).Count -eq @($oldQuestion.messages[-1].PSObject.Properties).Count) ('Changed question message: '+$chain.folder+'/'+$q)
        $records = @($response.logprobs)
        Require ($row.validSingleToken -and $response.eval_count -eq 1 -and $records.Count -eq 1) 'Invalid single-token answer.'
        Require ($row.output -ceq $response.message.content -and $records[0].token -ceq $row.output) 'Output record mismatch.'
        Require (($records[0].bytes -join ',') -ceq ([Text.Encoding]::UTF8.GetBytes($row.output) -join ',')) 'Token bytes mismatch.'
        foreach ($code in @('1','2','3','4')) {
            $score = @($records[0].top_logprobs | Where-Object { $_.token -ceq $code -and ($_.bytes -join ',') -ceq ([string](48+[int]$code)) })
            Require ($score.Count -eq 1 -and $null -ne $score[0].logprob -and -not [double]::IsNaN($score[0].logprob) -and -not [double]::IsInfinity($score[0].logprob)) 'Unavailable class score.'
            $savedValue = [double]$row.scores.$code
            $rawValue = [double]$score[0].logprob
            $difference = [Math]::Abs($savedValue-$rawValue)
            $maxScoreSerializationDifference = [Math]::Max($maxScoreSerializationDifference,$difference)
            # Windows PowerShell's JSON round trip can differ in the final decimal place.
            Require ($difference -le 1e-12) ('Saved score differs from raw response: '+$chain.folder+'/'+$q+'/'+$code)
        }
        $rawArgmax = @($records[0].top_logprobs | Where-Object token -cin @('1','2','3','4') | Sort-Object { [double]$_.logprob } -Descending)[0].token
        Require ($row.argmax -ceq $rawArgmax) 'Argmax record mismatch.'
        Require (($q -eq 1 -and $response.prompt_eval_cached_count -eq 0) -or ($q -gt 1 -and $response.prompt_eval_cached_count -gt 0)) 'Cache precondition failed.'
        Require ($row.cachedTokens -eq $response.prompt_eval_cached_count -and $row.promptTokens -eq $response.prompt_eval_count) 'Cache record mismatch.'
        if ($chain.arm -eq 'blind') {
            Require ($row.output -ceq '0') 'Blind output revealed a status.'
            Require (@($request.messages | Where-Object { $_.role -eq 'assistant' -and $_.content -cne '0' }).Count -eq 0) 'Blind history contains a status choice.'
        }
        $audit += $row
        $previous = $request
    }
}
$matrix = @()
$comparisons = @(foreach ($name in $names) {
    $f0 = ($prior | Where-Object { $_.name -eq $name -and $_.order -eq 'forward' }).calls
    $r0 = ($prior | Where-Object { $_.name -eq $name -and $_.order -eq 'reverse' }).calls
    $f1 = ($chains | Where-Object { $_.name -eq $name -and $_.arm -eq 'repeat' }).calls
    $bf = ($chains | Where-Object { $_.name -eq $name -and $_.arm -eq 'blind' -and $_.order -eq 'forward' }).calls
    $br = ($chains | Where-Object { $_.name -eq $name -and $_.arm -eq 'blind' -and $_.order -eq 'reverse' }).calls
    $rows = @(foreach ($old in $f0) {
        $repeat = $f1 | Where-Object event -eq $old.event
        $reverse = $r0 | Where-Object event -eq $old.event
        $blindForward = $bf | Where-Object event -eq $old.event
        $blindReverse = $br | Where-Object event -eq $old.event
        [pscustomobject]@{name=$name;event=$old.event;oldForward=$old.sampled;repeatForward=$repeat.output;oldReverse=$reverse.sampled;oldForwardArgmax=$old.argmax;repeatArgmax=$repeat.argmax;oldReverseArgmax=$reverse.argmax;blindForwardArgmax=$blindForward.argmax;blindReverseArgmax=$blindReverse.argmax}
    })
    $matrix += $rows
    [pscustomobject]@{name=$name;positions=$rows.Count;repeatOutputChanges=@($rows | Where-Object {$_.oldForward -ne $_.repeatForward}).Count;repeatArgmaxChanges=@($rows | Where-Object {$_.oldForwardArgmax -ne $_.repeatArgmax}).Count;oldOrderOutputChanges=@($rows | Where-Object {$_.oldForward -ne $_.oldReverse}).Count;oldOrderArgmaxChanges=@($rows | Where-Object {$_.oldForwardArgmax -ne $_.oldReverseArgmax}).Count;blindOrderArgmaxChanges=@($rows | Where-Object {$_.blindForwardArgmax -ne $_.blindReverseArgmax}).Count}
})
$groups = @(foreach ($arm in @('repeat','blind')) {
    $rows = @($audit | Where-Object arm -eq $arm)
    $extensions = @($rows | Where-Object index -gt 1)
    [pscustomobject]@{arm=$arm;positions=$rows.Count;fourFinite=$rows.Count;freshCalls=@($rows | Where-Object index -eq 1).Count;extensions=$extensions.Count;cachedMin=($extensions | Measure-Object cachedTokens -Minimum).Minimum;cachedMax=($extensions | Measure-Object cachedTokens -Maximum).Maximum;maxPrompt=($rows | Measure-Object promptTokens -Maximum).Maximum}
})
$result = [ordered]@{auditedRequests=$audit.Count;mismatches=0;maxScoreSerializationDifference=$maxScoreSerializationDifference;groups=$groups;comparisons=$comparisons;eventMatrix=$matrix;chains=@($chains | Select-Object arm,order,name,mode,questions,chainSeconds);timing='Battery diagnostics; seconds informative only. Not cost measurements.'}
[IO.File]::WriteAllText((Join-Path $RunRoot 'blind-history-audit.json'), (Json $result), (New-Object Text.UTF8Encoding($false)))
$result | ConvertTo-Json -Depth 8
