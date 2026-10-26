param()
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$filterPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'validation-cache-filter.targets'))
$logFolder = Join-Path $workspace 'tools/law-corpus/.cache'
if (-not $filterPath.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Validation filter escaped the isolated workspace.'
}
# Opt-in import affects only this command, matching clean-checkout data without
# moving evidence out from under a running OCR process or editing shared targets.
Push-Location -LiteralPath $workspace
try {
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
        -p:CustomAfterMicrosoftCommonTargets=$filterPath `
        -p:CreateHardLinksForCopyLocalIfPossible=true `
        -p:CreateHardLinksForCopyFilesToOutputDirectoryIfPossible=true `
        -p:CreateHardLinksForCopyAdditionalFilesIfPossible=true *> (Join-Path $logFolder 'resumed-full-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Full Release build failed; inspect resumed-full-build.log.' }
    dotnet test -c Release --no-build --no-restore -v minimal *> (Join-Path $logFolder 'resumed-full-tests.log')
    if ($LASTEXITCODE -ne 0) { throw 'Full regression suite failed; inspect resumed-full-tests.log.' }
}
finally { Pop-Location }
Write-Output 'Full Release build and regression suite passed; source cache was not moved.'
