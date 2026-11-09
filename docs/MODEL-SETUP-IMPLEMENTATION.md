# Model setup implementation record

## Owner-approved scope

Work is confined to `codex/model-setup`, based on default commit `4e57c96`. The owner approved Phase 2 on 9 November 2026: CPU-only standalone Ollama, installed-RAM recommendation (16 GiB -> Gemma 4 E4B, otherwise Gemma 3 4B), installed Gemma 4 preference, bilingual non-blocking setup, cloud disabled, no tray/updater, compatible existing-server reuse, model terms before acquisition and a 2 GiB disk reserve.

The owner's amendments supersede the proposal's signing discussion: **no signing key**. The app embeds the exact expected manifest and every blob's SHA-256/size for two supported model versions. Import accepts only that built-in inventory, with archive/path safety checks. Export (“Save model to a file…”) verifies the same inventory and emits a `.dzmodel` pack suitable for USB sharing; no case files, home/config or credentials belong in it.

Delivery milestones: M1 engine ownership/coexistence and model choice; M2 setup/progress/cancel/resume/resource checks; M3 import/export; M4 offline/privacy documentation; M5 clean-account/disconnected-network acceptance and complete package sizes. Push and report each milestone, then continue without requesting another go. Protected engine/provider/harness/data areas remain unchanged unless separately approved. The known shutdown test must not be edited.

## M1 — managed engine and selection

Implemented:

- A managed engine session with serialized startup, short existing-server probe, bounded bind retries and explicit loopback ownership checks. Compatibility is deliberately restricted to the pinned tested Ollama 0.40.15 API. Unknown listeners and other server versions are left alone; the private engine uses another loopback port.
- An absolute bundled executable path, a pinned inventory for every CPU runtime file, and SHA-256/length verification before execution. Child-only settings isolate models, home/config and temp storage under the app root; inherited cloud/debug/history settings are discarded. Raw engine output is drained and discarded, never logged with case text or credentials.
- Windows job ownership closes only the app's engine and its children. Reused servers are never killed or reconfigured; the close hook does not unload a reused server's models.
- Automatic installed-model ranking: Gemma 4 E4B, then the existing text-only Gemma 3 when present, then Gemma 3 4B. An explicit advanced model keeps its setting. Selection verifies local GGUF metadata and rejects remote aliases even without a cloud suffix. `ThinkFor` is applied after every selection/fallback.
- Developer-only `Prepare-Engine.ps1`: verifies the full pinned upstream ZIP and every retained CPU file before staging it under ignored `artifacts/`; publishing refuses a missing engine. No installed-app engine download is introduced. Additional Ollama, LLVM, MinGW and winpthreads notices accompany the CPU runtime; upstream aggregate notices remain in place.

Tests added: reuse/no ownership, occupied port, bounded bind-race failure and cleanup, refusal of readiness from a foreign listener, private child settings, ranking/think after selection and remote alias rejection. Existing model-choice mock responses now include local metadata; their assertions remain intact. Additional cases cover Gemma 4 -> Gemma 3 thinking fallback and preservation of explicit advanced choices. `tools/model-setup/EngineCheck` performs a real Windows-process coexistence/disposal smoke check without pull or inference.

Files outside app/presentation/tests: engine notices under `assets/engines/ollama`, preparation/smoke-check tools under `tools/model-setup`, and this report. No protected project, legal data or prompt was edited. UI state screenshots begin with the card in M2; M1 has no new visible state or string.

Full archive acquisition is a developer packaging action, separate from app networking. The Phase 1 range-extracted CPU engine is used only for development smoke checks until the full upstream digest succeeds. Shipping preparation fails closed on a missing or mismatched full archive.

Post-commit Release build/full-suite outcomes and native smoke results are reported with the milestone push. Existing shutdown timing failures, if any, will be disclosed rather than fixed or weakened here.