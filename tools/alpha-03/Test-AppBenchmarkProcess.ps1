$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'AppBenchmarkProcess.ps1')
. (Join-Path $PSScriptRoot 'SharedResourceAppend.ps1')
$fixtureRoot=Join-Path ([IO.Path]::GetTempPath()) ('dz-alpha03-process-unit-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot|Out-Null
try{
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
    Write-Output 'Two native exit-code fixtures and one shared-reader/writer fixture passed; zero model calls.'
} finally{
    $resolved=[IO.Path]::GetFullPath($fixtureRoot)
    $expectedRoot=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'dz-alpha03-process-unit-'))
    if(-not $resolved.StartsWith($expectedRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected fixture cleanup path.'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
