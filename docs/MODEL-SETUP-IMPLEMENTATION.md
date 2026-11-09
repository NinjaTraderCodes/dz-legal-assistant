# Model setup implementation record

## Owner-approved scope

Work is confined to `codex/model-setup`, based on default commit `4e57c96`. The owner approved Phase 2 on 9 November 2026: CPU-only standalone Ollama, installed-RAM recommendation (16 GiB -> Gemma 4 E4B, otherwise Gemma 3 4B), installed Gemma 4 preference, bilingual non-blocking setup, cloud disabled, no tray/updater, compatible existing-server reuse, model terms before acquisition and a 2 GiB disk reserve.

The owner's amendments supersede the proposal's signing discussion: **no signing key**. The app embeds the exact expected manifest and every blob's SHA-256/size for two supported model versions. Import accepts only that built-in inventory, with archive/path safety checks. Export (“Save model to a file…”) verifies the same inventory and emits a `.dzmodel` pack suitable for USB sharing; no case files, home/config or credentials belong in it.

Delivery milestones: M1 engine ownership/coexistence and model choice; M2 setup/progress/cancel/resume/resource checks; M3 import/export; M4 offline/privacy documentation; M5 clean-account/disconnected-network acceptance and complete package sizes. Push and report each milestone, then continue without requesting another go. Protected engine/provider/harness/data areas remain unchanged unless separately approved. The known shutdown test must not be edited.

## M1 — managed engine and selection

Implemented:

- A managed engine session with serialized startup, short existing-server probe, bounded bind retries and explicit loopback ownership checks. Compatibility accepts recognised stable versions **0.30.9 and newer**. Older or unrecognised versions and unknown listeners are left alone; the private engine uses another loopback port.
- An absolute bundled executable path, a pinned inventory for every CPU runtime file, and SHA-256/length verification before execution. Child-only settings isolate models, home/config and temp storage under the app root; inherited cloud/debug/history settings are discarded. Raw engine output is drained and discarded, never logged with case text or credentials.
- Windows job ownership closes only the app's engine and its children. Reused servers are never killed or reconfigured; the close hook sends `keep_alive: 0` for only the model currently selected by this app, including on a reused server. Other installed/loaded models are never enumerated for unloading.
- Automatic installed-model ranking: Gemma 4 E4B, then the existing text-only Gemma 3 when present, then Gemma 3 4B. An explicit advanced model keeps its setting. Selection verifies local GGUF metadata and rejects remote aliases even without a cloud suffix. `ThinkFor` is applied after every selection/fallback.
- Developer-only `Prepare-Engine.ps1`: verifies the full pinned upstream ZIP and every retained CPU file before staging it under ignored `artifacts/`; publishing refuses a missing engine. No installed-app engine download is introduced. Additional Ollama, LLVM, MinGW and winpthreads notices accompany the CPU runtime; upstream aggregate notices remain in place.

Tests added: reuse/no ownership, occupied port, bounded bind-race failure and cleanup, refusal of readiness from a foreign listener, private child settings, ranking/think after selection and remote alias rejection. Existing model-choice mock responses now include local metadata; their assertions remain intact. Additional cases cover Gemma 4 -> Gemma 3 thinking fallback and preservation of explicit advanced choices. `tools/model-setup/EngineCheck` performs a real Windows-process coexistence/disposal smoke check without pull or inference.

Files outside app/presentation/tests: engine notices under `assets/engines/ollama`, preparation/smoke-check tools under `tools/model-setup`, and this report. No protected project, legal data or prompt was edited. UI state screenshots begin with the card in M2; M1 has no new visible state or string.

Full archive acquisition is a developer packaging action, separate from app networking. The Phase 1 range-extracted CPU engine is used only for development smoke checks until the full upstream digest succeeds. Shipping preparation fails closed on a missing or mismatched full archive.

Post-commit Release build/full-suite outcomes and native smoke results are reported with the milestone push. Existing shutdown timing failures, if any, will be disclosed rather than fixed or weakened here.
## Owner review corrections to M1

The previous exact-version restriction was too narrow for an independently updated Ollama. `LocalOllamaApi.MinimumVersion` is now 0.30.9, the `requires` value in the pinned Gemma 4 CPU config [7ca1ae56…](https://registry.ollama.ai/v2/library/gemma4/blobs/sha256:7ca1ae564b24fefd47da34cc579b906b75024860ed4e5bfbfa6d6cbd9777cee5). This is the minimum for the **exact supported model inventory**, rather than the first release supporting any Gemma 4 variant. [Ollama 0.30.9](https://github.com/ollama/ollama/releases/tag/v0.30.9) is a published stable release; Gemma 3 was already supported in [0.6.0](https://github.com/ollama/ollama/releases/tag/v0.6.0). The local public config was read and its SHA-256 matches its content address. No inference, timing or memory measurement was needed to determine this requirement. Stable numeric versions at or above the floor are accepted; missing, malformed and unrecognised prerelease/development strings fail closed to the private engine. Local GGUF/remote-alias checks still apply after version compatibility.

Release/close keeps the existing provider's single-model unload request, regardless of engine ownership. A loopback integration test probes two installed models, selects Gemma 4, then releases on a reused session: it observes one unload for Gemma 4 and none for Gemma 3. It does not kill or reconfigure the external server.

### Proxy decision for M2

The child environment remains an allowlist. Copy **HTTPS_PROXY** and **NO_PROXY** from the launching user's process environment into the managed engine only, adding `127.0.0.1,localhost,::1` to the bypass list. Do not copy HTTP_PROXY, ALL_PROXY, arbitrary certificate-file overrides, inherited OLLAMA settings or unrelated variables. This follows the [official Ollama proxy guidance](https://docs.ollama.com/faq#how-do-i-use-ollama-behind-a-proxy): model pulls use HTTPS; a corporate proxy certificate must already be trusted by Windows. The app does not install certificates or bypass TLS checks. Restart the app after changing user proxy settings.

The app's setup and inference clients still use `LoopbackGuard` with proxies and redirects disabled. Their case requests never enter the office proxy. Only a lawyer-initiated engine model download may use the proxy, and it contains model identifiers and public model bytes, not case data. Proxy values may contain credentials: do not display, persist or log them, process start arguments, raw engine output or raw pull errors. Raw stdout/stderr remains drained and discarded; the card uses localised error categories. An independently running Ollama keeps its own environment; it is not reconfigured. There is no automatic download, connection test or proxy discovery on startup. Offline packs remain the alternative when proxy/certificate setup is unavailable.

Added tests cover an older/equal/newer version, missing/malformed version responses, selected-model-only release on a reused server, and proxy allowlisting/bypass entries without weakening loopback. The known shutdown timing test failed unchanged in the prior unpublished M2 suite (1 failure; 1,061 passed); it is not changed in this correction.
