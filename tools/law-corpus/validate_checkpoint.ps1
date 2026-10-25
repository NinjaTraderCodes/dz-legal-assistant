param()
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cache = [IO.Path]::GetFullPath((Join-Path $workspace 'data/law/corpus/.cache'))
$held = [IO.Path]::GetFullPath((Join-Path $workspace 'tools/law-corpus/.cache/build-held-data-cache'))
$logFolder = Join-Path $workspace 'tools/law-corpus/.cache'
foreach ($target in @($cache, $held)) {
    if (-not $target.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Cache target escaped the isolated workspace: $target"
    }
}
if (Test-Path -LiteralPath $held) { throw 'Held-cache folder already exists; recover it before validation.' }
if (-not (Test-Path -LiteralPath $cache)) { throw 'Expected source cache is absent.' }
# Run only once extraction/composition has stopped: a validation build must not
# relocate evidence while another process is reading or writing it.
Move-Item -LiteralPath $cache -Destination $held
try {
    Push-Location -LiteralPath $workspace
    try {
        # Remove only stale cache copies from this worktree's development outputs.
        foreach ($area in @('src', 'tests')) {
            foreach ($project in Get-ChildItem -LiteralPath (Join-Path $workspace $area) -Directory) {
                $output = Join-Path $project.FullName 'bin'
                if (-not (Test-Path -LiteralPath $output)) { continue }
                foreach ($copy in Get-ChildItem -LiteralPath $output -Directory -Recurse -Filter '.cache') {
                    $resolved = [IO.Path]::GetFullPath($copy.FullName)
                    if (-not $resolved.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                        throw "Build cache copy escaped workspace: $resolved"
                    }
                    if ($resolved -match '[\\/]data[\\/]law[\\/]corpus[\\/]\.cache$') {
                        Remove-Item -LiteralPath $resolved -Recurse -Force
                    }
                }
            }
        }
        dotnet build -c Release --no-restore -v minimal `
            -p:CreateHardLinksForCopyLocalIfPossible=true `
            -p:CreateHardLinksForCopyFilesToOutputDirectoryIfPossible=true `
            -p:CreateHardLinksForCopyAdditionalFilesIfPossible=true *> (Join-Path $logFolder 'resumed-full-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Full Release build failed; inspect resumed-full-build.log.' }
        dotnet test -c Release --no-build --no-restore -v minimal *> (Join-Path $logFolder 'resumed-full-tests.log')
        if ($LASTEXITCODE -ne 0) { throw 'Full regression suite failed; inspect resumed-full-tests.log.' }
    }
    finally { Pop-Location }
}
finally {
    if (Test-Path -LiteralPath $cache) { throw 'Unexpected new source-cache folder; restore held cache manually without overwriting.' }
    Move-Item -LiteralPath $held -Destination $cache
}
Write-Output 'Full Release build and regression suite passed; source cache restored.'
