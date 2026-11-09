$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'AppBenchmarkProcess.ps1')
. (Join-Path $PSScriptRoot 'SharedResourceAppend.ps1')
. (Join-Path $PSScriptRoot 'AppBenchmarkAwake.ps1')
$fixtureRoot=Join-Path ([IO.Path]::GetTempPath()) ('dz-alpha03-process-unit-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot|Out-Null
try{
    $stageOne=@(Get-AppBenchmarkPlan -Stage grounds -Gemma4First)
    if($stageOne.Count -ne 2 -or ($stageOne.Model -join ',') -ne 'gemma4:e4b,gemma3:4b' -or @($stageOne|Where-Object Command -ne 'grounds').Count){throw 'Stage-one plan fixture failed.'}
    $legacy=@(Get-AppBenchmarkPlan)
    if($legacy.Count -ne 4 -or $legacy[0].Model -ne 'gemma3:4b' -or $legacy[2].Command -ne 'draft'){throw 'Default plan fixture failed.'}
    $previous=Enter-AppBenchmarkAwake
    Exit-AppBenchmarkAwake -Previous $previous
    foreach($expected in @(0,7)){
        $child=Start-AppBenchmarkProcess -FilePath powershell.exe -Arguments ('-NoProfile -Command "exit '+$expected+'"') -WorkingDirectory $fixtureRoot -Stdout (Join-Path $fixtureRoot ('stdout-'+$expected+'.txt')) -Stderr (Join-Path $fixtureRoot ('stderr-'+$expected+'.txt'))
        while(-not $child.HasExited){Start-Sleep -Milliseconds 20}
        $actual=Get-AppBenchmarkExitCode -Process $child
        if($actual -ne $expected){throw ('Exit-code fixture failed: expected '+$expected+', got '+$actual)}
        $child.Dispose()
    }
    $resource=Join-Path $fixtureRoot 'resource.jsonl'
    Add-SharedResourceLine -Path $resource -Line '{"sample":1}'
    $reader=[IO.FileStream]::new($resource,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try{
        Add-SharedResourceLine -Path $resource -Line '{"sample":2}'
        $text=[IO.StreamReader]::new($reader)
        try{$lines=@($text.ReadToEnd().Split("`n")|Where-Object {$_.Trim()})}finally{$text.Dispose()}
        if($lines.Count -ne 2 -or ($lines[1]|ConvertFrom-Json).sample -ne 2){throw 'Shared append fixture failed.'}
    }finally{$reader.Dispose()}
    Write-Output 'Native exits, shared I/O, stage ordering and sleep-inhibition fixtures passed; zero model calls.'
} finally{
    $resolved=[IO.Path]::GetFullPath($fixtureRoot)
    $expectedRoot=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'dz-alpha03-process-unit-'))
    if(-not $resolved.StartsWith($expectedRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected fixture cleanup path.'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
