# Windows (PowerShell) twin of deploy/cloud-run.sh: deploys the web demo (src/DzLegal.Web) to Google Cloud Run.
# Needs the Google Cloud CLI (https://cloud.google.com/sdk/docs/install), logged in with `gcloud auth login`.
# Run it from the repository root:
#
#   .\deploy\cloud-run.ps1                 first run: asks for the Google AI Studio key once, then deploys
#   .\deploy\cloud-run.ps1                 later runs: redeploys the current code, keeps the stored key
#   .\deploy\cloud-run.ps1 -UpdateKey      replaces the stored key
#   .\deploy\cloud-run.ps1 -MinInstances 1 keeps one instance warm (no cold start; costs a little while it is on)
#
# If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\deploy\cloud-run.ps1
# The key is kept in Secret Manager; it is never written to the repository or shown. See docs/WEB-DEMO.md.
param(
    [string]$Project = (gcloud config get-value project 2>$null),
    [string]$Region = "europe-west1",
    [string]$Service = "dz-legal-demo",
    [string]$Secret = "google-api-key",
    [int]$MinInstances = 0,
    [switch]$UpdateKey
)
$ErrorActionPreference = "Stop"

function Invoke-Gcloud {
    & gcloud @args
    if ($LASTEXITCODE -ne 0) { throw "gcloud $($args[0]) $($args[1]) failed." }
}

if (-not $Project) { throw "No Google Cloud project selected. Run: gcloud config set project YOUR_PROJECT_ID" }
if (-not (Test-Path Dockerfile) -or -not (Test-Path src/DzLegal.Web)) { throw "Run this script from the repository root." }
Write-Host "Project $Project, region $Region, service $Service"
if ($Project -like "gen-lang-client-*") {
    throw "$Project is the project Google AI Studio made for the API key. Keep the key there (free tier) and deploy to a separate project with billing: create one, link a billing account, then gcloud config set project THAT_PROJECT_ID. See docs/WEB-DEMO.md."
}

Write-Host "Enabling the APIs (first run only takes a minute)..."
Invoke-Gcloud services enable run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com secretmanager.googleapis.com --project $Project

function Save-Key([string[]]$Command) {
    $secure = Read-Host "Paste the Google AI Studio API key (input hidden), then press Enter" -AsSecureString
    $key = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    if (-not $key) { throw "No key entered." }
    # A temporary file without a trailing newline (a PowerShell pipe would add one), deleted right after.
    $file = New-TemporaryFile
    try {
        [IO.File]::WriteAllText($file.FullName, $key.Trim())
        Invoke-Gcloud @Command --project $Project "--data-file=$($file.FullName)"
    }
    finally {
        Remove-Item $file.FullName -Force
    }
}

# Windows PowerShell turns a redirected error message into an exception, hence the try.
$exists = $true
try {
    gcloud secrets describe $Secret --project $Project *> $null
    if ($LASTEXITCODE -ne 0) { $exists = $false }
}
catch { $exists = $false }

if (-not $exists) {
    Save-Key -Command @("secrets", "create", $Secret, "--replication-policy", "automatic")
}
elseif ($UpdateKey) {
    Save-Key -Command @("secrets", "versions", "add", $Secret)
}

# The service and the source build run as the project's default compute service account: it needs to read the key
# and to build the image.
$number = gcloud projects describe $Project --format "value(projectNumber)"
$account = "serviceAccount:$number-compute@developer.gserviceaccount.com"
Invoke-Gcloud secrets add-iam-policy-binding $Secret --project $Project --member $account --role roles/secretmanager.secretAccessor --condition None | Out-Null
Invoke-Gcloud projects add-iam-policy-binding $Project --member $account --role roles/run.builder --condition None | Out-Null

# Same settings as cloud-run.sh: one instance, session affinity and a one-hour timeout for Blazor Server's WebSocket.
Invoke-Gcloud run deploy $Service --project $Project --region $Region --source . `
    --allow-unauthenticated `
    --port 7860 --memory 1Gi --cpu 1 --cpu-boost `
    --min-instances $MinInstances --max-instances 1 --concurrency 200 `
    --timeout 3600 --session-affinity `
    --set-secrets "GOOGLE_API_KEY=${Secret}:latest"

$url = gcloud run services describe $Service --project $Project --region $Region --format "value(status.url)"
Write-Host ""
Write-Host "Demo URL: $url"
try { Write-Host "Health:   $((Invoke-WebRequest "$url/healthz" -UseBasicParsing).Content)" }
catch { Write-Host "Health:   not answering yet, retry in a minute" }
