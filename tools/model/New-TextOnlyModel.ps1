<#
.SYNOPSIS
Creates the text-only model the app prefers: Gemma 3 4B's language weights, chat template, parameters and licence,
without the image (vision) part the app never uses. About 0.8 GB less memory, identical answers (CPU study of
18 October 2026: three seeded pairs, every answer and draft identical).

.DESCRIPTION
Reads the manifest of the installed source model in the local Ollama store, writes a Modelfile that points at the
same language-weights file (nothing is downloaded or copied), and runs `ollama create`. Local only: it never contacts
any server other than the local Ollama.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\model\New-TextOnlyModel.ps1
#>
param(
    [string]$Source = 'gemma3:4b',
    [string]$Name = 'dz-gemma3-4b-text'
)
$ErrorActionPreference = 'Stop'

$models = if ($env:OLLAMA_MODELS) { $env:OLLAMA_MODELS } else { Join-Path $env:USERPROFILE '.ollama\models' }
$repo, $tag = $Source.Split(':')
if (-not $tag) { $tag = 'latest' }
$manifestPath = Join-Path $models "manifests\registry.ollama.ai\library\$repo\$tag"
if (-not (Test-Path $manifestPath)) { throw "Source model '$Source' is not installed (no manifest at $manifestPath). Run: ollama pull $Source" }
$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json

function Get-Layers([string]$type) { @($manifest.layers | Where-Object { $_.mediaType -eq $type }) }
function Get-BlobPath($layer) { Join-Path $models ('blobs\' + $layer.digest.Replace(':', '-')) }

$weights = Get-Layers 'application/vnd.ollama.image.model'
if ($weights.Count -ne 1) { throw "Expected one language-weights layer in $Source, found $($weights.Count)." }
$template = Get-Layers 'application/vnd.ollama.image.template'
$params = Get-Layers 'application/vnd.ollama.image.params'
$licences = Get-Layers 'application/vnd.ollama.image.license'
$projector = Get-Layers 'application/vnd.ollama.image.projector'

$lines = @("FROM $(Get-BlobPath $weights[0])")
if ($template.Count -eq 1) {
    $text = Get-Content -Raw (Get-BlobPath $template[0])
    if ($text.Contains('"""')) { throw 'The chat template contains """; write the Modelfile by hand.' }
    $lines += "TEMPLATE `"`"`"$text`"`"`""
}
if ($params.Count -eq 1) {
    $json = Get-Content -Raw (Get-BlobPath $params[0]) | ConvertFrom-Json
    foreach ($p in $json.PSObject.Properties) {
        foreach ($v in @($p.Value)) {
            $value = if ($v -is [string]) { '"' + $v.Replace('"', '\"') + '"' } else { [string]$v }
            $lines += "PARAMETER $($p.Name) $value"
        }
    }
}
foreach ($l in $licences) {
    $text = Get-Content -Raw (Get-BlobPath $l)
    if ($text.Contains('"""')) { throw 'A licence contains """; write the Modelfile by hand.' }
    $lines += "LICENSE `"`"`"$text`"`"`""
}

$modelfile = Join-Path ([IO.Path]::GetTempPath()) "$Name.Modelfile"
Set-Content -Path $modelfile -Value ($lines -join "`n") -Encoding UTF8
Write-Host "Source: $Source (weights $($weights[0].digest); vision part left out: $($projector.Count) layer(s))"
& ollama create $Name -f $modelfile
if ($LASTEXITCODE -ne 0) { throw "ollama create failed ($LASTEXITCODE)." }

# Check: the same weights file, and no vision part.
$newManifest = Get-Content -Raw (Join-Path $models "manifests\registry.ollama.ai\library\$Name\latest") | ConvertFrom-Json
$newWeights = @($newManifest.layers | Where-Object { $_.mediaType -eq 'application/vnd.ollama.image.model' })
$newProjector = @($newManifest.layers | Where-Object { $_.mediaType -eq 'application/vnd.ollama.image.projector' })
if ($newWeights.Count -ne 1 -or $newWeights[0].digest -ne $weights[0].digest) { throw 'The new model does not use the same language weights.' }
if ($newProjector.Count -ne 0) { throw 'The new model still has a vision part.' }
Remove-Item $modelfile
Write-Host "Created ${Name}: same language weights as ${Source}, no vision part. The app uses it from its next start."
