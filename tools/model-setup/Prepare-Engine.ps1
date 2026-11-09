# Explicit developer packaging action. The installed application never downloads its engine.
[CmdletBinding()]
param([string]$Archive)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$expected = '9ca74a7ab66ec86870b1eecac7df160df08dc4e3a291ab5d2d59bcf9310221c6'
if (-not $Archive) {
    $cache = Join-Path $env:LOCALAPPDATA 'DzLegalAssistant\model-setup-build\ollama-0.40.15'
    New-Item -ItemType Directory -Path $cache -Force | Out-Null
    $Archive = Join-Path $cache 'ollama-windows-amd64.zip'
    if (-not (Test-Path -LiteralPath $Archive)) {
        curl.exe --fail --location --output $Archive 'https://github.com/ollama/ollama/releases/download/v0.40.15/ollama-windows-amd64.zip'
        if ($LASTEXITCODE -ne 0) { throw 'Engine download failed; provide a complete archive or retry the explicit download.' }
    }
}
if ((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'The complete upstream ZIP digest does not match.' }
$inventory = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\DzLegal.App\Services\LocalModels\engine-files.json') | ConvertFrom-Json
$destination = Join-Path $repoRoot 'artifacts\ollama-0.40.15'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($Archive)
try {
    foreach ($file in $inventory.files) {
        $entry = $zip.GetEntry($file.path)
        if (-not $entry -or $entry.Length -ne $file.size) { throw ('Missing engine entry: ' + $file.path) }
        $target = Join-Path $destination $file.path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw ('Engine entry digest failed: ' + $file.path) }
    }
}
finally { $zip.Dispose() }
Write-Output 'Prepared verified Ollama 0.40.15 CPU-only payload.'