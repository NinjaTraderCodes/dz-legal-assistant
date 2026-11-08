param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputRoot){throw 'Use a fresh external fixture directory.'}
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$resolved=[IO.Path]::GetFullPath($OutputRoot)
if($resolved.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture output must be external.'}
New-Item -ItemType Directory -Path $resolved|Out-Null
# These functions replace the native command and HTTP cmdlet in this test process only.
function dotnet {
    $global:Alpha03FixtureNativeCalls++
    if($global:Alpha03FixtureKind -eq 'thrown'){throw 'Synthetic native pipeline failure'}
    $global:LASTEXITCODE=3
    Write-Output 'Synthetic returned precondition failure'
}
function Invoke-RestMethod {
    param($Uri,$Method,$ContentType,$Body,$TimeoutSec)
    $request=$Body|ConvertFrom-Json
    if($request.keep_alive -ne 0){throw 'Fixture attempted a generation request.'}
    $global:Alpha03FixtureUnloads+=([string]$request.model)
    return [pscustomobject]@{done=$true;synthetic=$true}
}
$results=@()
foreach($kind in @('returned','thrown')){
    $global:Alpha03FixtureKind=$kind
    $global:Alpha03FixtureNativeCalls=0
    $global:Alpha03FixtureUnloads=@()
    $folder=Join-Path $resolved $kind
    New-Item -ItemType Directory -Path $folder|Out-Null
    $stopped=$false
    try { & (Join-Path $PSScriptRoot 'Measure-ScoredFitting.ps1') -RunRoot $folder -Mode fitting }
    catch { if($_.Exception.Message -notmatch 'Scored collection stopped'){throw};$stopped=$true }
    if(-not $stopped -or $global:Alpha03FixtureNativeCalls -ne 1 -or $global:Alpha03FixtureUnloads.Count -ne 1 -or $global:Alpha03FixtureUnloads[0] -ne 'gemma3:4b'){throw 'Fixture did not stop/clean up before the second model.'}
    if(-not(Test-Path -LiteralPath (Join-Path $folder 'gemma3-4b-process-completion.json'))){throw 'Failure metadata absent.'}
    if(Test-Path -LiteralPath (Join-Path $folder 'gemma4-e4b-process-completion.json')){throw 'Second model was reached.'}
    $results+=@{kind=$kind;nativeCalls=1;simulatedUnloads=1;secondModelStarted=$false;actualHttpCalls=0;passed=$true}
}
$results|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $resolved 'results.json') -Encoding utf8
$results|ConvertTo-Json
