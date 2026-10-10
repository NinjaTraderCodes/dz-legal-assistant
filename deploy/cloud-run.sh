#!/usr/bin/env bash
# Deploys the web demo (src/DzLegal.Web) to Google Cloud Run. Run it from the repository root in Google Cloud Shell
# (shell.cloud.google.com: gcloud is already installed and logged in), or anywhere gcloud is set up.
#
#   ./deploy/cloud-run.sh              first deployment: asks for the Google AI Studio key once, then deploys
#   ./deploy/cloud-run.sh              later runs: redeploys the current code, keeps the stored key
#   UPDATE_KEY=1 ./deploy/cloud-run.sh replaces the stored key (e.g. after rotating it)
#   MIN_INSTANCES=1 ./deploy/cloud-run.sh  keeps one instance warm (no cold start; costs a little while it is on)
#
# The key is kept in Secret Manager and handed to the service as the GOOGLE_API_KEY environment variable.
# It is never written to the repository, to a file or to the terminal. See docs/WEB-DEMO.md.
set -euo pipefail

PROJECT="${PROJECT:-$(gcloud config get-value project 2>/dev/null)}"
REGION="${REGION:-europe-west1}"
SERVICE="${SERVICE:-dz-legal-demo}"
SECRET="${SECRET:-google-api-key}"
MIN_INSTANCES="${MIN_INSTANCES:-0}"

if [[ -z "$PROJECT" ]]; then
  echo "No Google Cloud project selected. Run: gcloud config set project YOUR_PROJECT_ID" >&2
  exit 1
fi

if [[ ! -f Dockerfile || ! -d src/DzLegal.Web ]]; then
  echo "Run this script from the repository root." >&2
  exit 1
fi

echo "Project $PROJECT, region $REGION, service $SERVICE"

echo "Enabling the APIs (first run only takes a minute)…"
gcloud services enable run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com \
  secretmanager.googleapis.com --project "$PROJECT"

read_key() {
  local key
  read -rsp "Paste the Google AI Studio API key (input hidden), then press Enter: " key
  echo >&2
  if [[ -z "$key" ]]; then
    echo "No key entered." >&2
    exit 1
  fi
  printf '%s' "$key"
}

if ! gcloud secrets describe "$SECRET" --project "$PROJECT" >/dev/null 2>&1; then
  read_key | gcloud secrets create "$SECRET" --project "$PROJECT" --replication-policy automatic --data-file=-
elif [[ "${UPDATE_KEY:-0}" == "1" ]]; then
  read_key | gcloud secrets versions add "$SECRET" --project "$PROJECT" --data-file=-
fi

# The service and the source build run as the project's default compute service account: it needs to read the key
# and to build the image.
NUMBER="$(gcloud projects describe "$PROJECT" --format 'value(projectNumber)')"
ACCOUNT="$NUMBER-compute@developer.gserviceaccount.com"
gcloud secrets add-iam-policy-binding "$SECRET" --project "$PROJECT" \
  --member "serviceAccount:$ACCOUNT" --role roles/secretmanager.secretAccessor --condition None >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT" \
  --member "serviceAccount:$ACCOUNT" --role roles/run.builder --condition None >/dev/null

# Blazor Server keeps each visitor's session in the server's memory over a WebSocket:
# - one instance at most, so every visitor shares the same queue and per-session limits, and costs stay capped;
# - session affinity and a one-hour request timeout, so a reconnecting browser finds its session again.
gcloud run deploy "$SERVICE" \
  --project "$PROJECT" --region "$REGION" --source . \
  --allow-unauthenticated \
  --port 7860 --memory 1Gi --cpu 1 --cpu-boost \
  --min-instances "$MIN_INSTANCES" --max-instances 1 --concurrency 200 \
  --timeout 3600 --session-affinity \
  --set-secrets "GOOGLE_API_KEY=$SECRET:latest"

URL="$(gcloud run services describe "$SERVICE" --project "$PROJECT" --region "$REGION" --format 'value(status.url)')"
echo
echo "Demo URL: $URL"
echo "Health:   $(curl -fsS "$URL/healthz" || echo 'not answering yet, retry in a minute')"
