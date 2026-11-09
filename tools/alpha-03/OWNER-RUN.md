# Frozen owner-run comparison

## 9 November phone-model extension (new owner authorisation)

Both pulls completed with exit0, serially, before timing. [PHONE-MODELS.json](PHONE-MODELS.json) records their locally installed identities and protocol, SHA256 `ba783d092f5d8ef11d177ee84a690052245c810b76796ab151a765d1e8317cda`:

| Tag | Digest | Licence | Installed bytes | Think |
|---|---|---|---:|---|
| gemma4:e2b | `b37049369adfe3d2b653af0ab301a062ca5cbe96ace6aa0d3f2559ee9b563fc2` | Apache2.0 | 4,587,156,505 | false |
| gemma3n:e2b | `dae166dc6a5e93061da12b75b887ce7c72cb91785d547b88c7503047f8411ffd` | Gemma Terms of Use | 7,836,324,540 | omitted |

After the freeze checks, `Measure-PhoneModels.ps1 -MetadataRoot <existing separate download-metadata folder>` runs the same wrapper serially, checks fresh paths and pins, keeps each whole console redirected/unread, and records only its own launch times/exits/service/awake state outside evaluation outputs. Search is temporarily stopped only if busy and restored; startup type is never changed. The original reader/settings/cap remain fixed. Exact fresh phone summary paths end in `hidden-v2-alpha01-gemma4-e2b-think-off-2026-11-09/summary.md` and `hidden-v2-alpha01-gemma3n-e2b-2026-11-09/summary.md` under the parent below. Neither command has run at this pre-timing checkpoint. Do not rerun after either folder exists.

After pushing Stage2, the owner authorises Stage3 without another wait: `ollama pull gemma4:e2b`, then `ollama pull gemma3n:e2b`, recording each installed digest before timing. The same wrapper additionally accepts these two tags, with **-Think false for gemma4:e2b** and **no Think argument for gemma3n:e2b**. Each phone command must pass `-ExpectedDigest <recorded SHA256>`; only these new approved tags accept a newly recorded pin. The two original model/digest/thinking pairs below remain pinned and cannot be overridden. All reader, parser, scorer and inference defaults are unchanged.

The wrapper opts into the repaired shared resource writer, retains the native exit handle, temporarily prevents sleep and restores the prior thread state. It rejects concurrent builds/tests and a resident model before the run. Same serial execution, fresh output, ten-case reload,512MiB guard, idle app, mains/Best performance, closed browsers/VSCode and no automatic retry. Stdout/stderr consoles remain redirected and unread. A failed precondition stops the stage, not a next-model retry.

For this Stage3 only, the agent may read/share **summary.md only**, at each newly named phone output and the two previously given output paths ending `hidden-v2-alpha01-gemma3/summary.md` and `hidden-v2-alpha01-gemma4-think-off/summary.md`, to produce the requested four-model aggregate comparison. No other owner-run output or console, no per-case file and no hidden input may be opened/listed/searched/copied. The harness alone consumes the fixed hidden path. Summary permissions do not grant general evaluation access. The historical freeze instructions below describe the original 4B runs; do not rerun them.

The owner selected **unchanged Alpha-01 with gemma4:e4b, thinking off** independently of the scored-reader diagnosis. The baseline is unchanged Alpha-01 with gemma3:4b and no thinking override. Both use the lawyer-confirmed claim type, the same original prompt/parser/scorer, CPU only, context 4096, temperature 0.2, repeat penalty 1.1 and the existing 300-token cap. No scored reader, correction or binding experiment runs here. The app's default model is unchanged.

Use the final commit supplied in the handoff, with its successful Release build. `OWNER-FREEZE.json` records the relevant source fingerprints, model digests and the exact files changed outside the harness/tools areas since the Alpha-03 starting commit. Text fingerprints normalise CRLF to LF; they contain no evaluation input. The wrapper checks the installed model digest through the harness and never downloads a model.

| Model | Thinking | Installed digest | Licence | Installed model bytes |
|---|---|---|---|---:|
| gemma3:4b | omitted, existing default | `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` | Gemma Terms | 3,349,593,375 |
| gemma4:e4b | `false` | `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c` | Apache 2.0 | 6,583,656,505 |

These are the already installed artefacts recorded during the accepted mains-powered runs, not redistributed weights. Measured Ollama version: 0.40.15. The run manifest records the installed version again.

## Before running

Connect mains power, select **Best performance**, and close browsers, VS Code and other heavy applications yourself. Leave the desktop app open and idle, using template mode; leave Ollama running with no other model job or resident model. Run from a separate PowerShell window. Do not run a build, tests, another benchmark or a second command concurrently.

The two output directories below must be new. Run the baseline once, wait for completion and model unload, then run the candidate once. A nonzero exit or guard event that stops a run means preserve the partial record and report it; do not retry or raise the cap automatically. The output is outside the repository, under the app's per-user root. The wrapper creates the directories.

Baseline:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\TheShadow\dz-legal-assistant\tools\alpha-03\Run-OwnerAlpha01.ps1" -Model "gemma3:4b" -OutputRoot "$env:LOCALAPPDATA\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma3"
```

Frozen candidate:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\TheShadow\dz-legal-assistant\tools\alpha-03\Run-OwnerAlpha01.ps1" -Model "gemma4:e4b" -Think false -OutputRoot "$env:LOCALAPPDATA\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma4-think-off"
```

Both commands use the **same wrapper**, whose fixed input path is `C:\dz-hidden-v2`. Only the harness receives and reads that path. The wrapper and its resource-monitor helper never open, list or copy case files, answers or per-case output; the wrapper reads only resource metadata. Harness stdout contains only `PROGRESS` counts and `GUARD` events; stderr goes directly to a private file without being read by the wrapper. The harness privately loads, runs, scores and writes the cases and summary.

## Measurement and limits

The wrapper checks mains/Best performance, an idle resident app, closed browsers/VS Code and a quiet preflight. The harness checks these guards before each case, unloads and reloads before cases 1, 11, 21 and so on, records each empty load separately from case time, and unloads at the end. The existing 512 MiB available-memory guard and model-private-plus-app physical-RAM guard remain in force. Browser closing is owner-only. The wrapper never changes power, services or owner processes.

`summary.md` contains the original scorer's results and error types, sampled memory peaks, power/headroom sample counts and the full amortised batch cost. `loads.json`, `measurement-completion.json`, `measurement-resources.json`, `manifest.json`, model identity and resource samples retain the supporting record. The original scorer excludes out-of-catalogue cases from its accuracy table; the amortised batch denominator explicitly includes all attempted files. Sampling can miss short memory peaks. A 300-token cap remains a scored result with its partial facts; transport/context/guard failures stop the run without a retry.

The agent has **not executed this wrapper or either command**, and has not accessed hidden v2 or its outputs. Builds, full tests and freeze review were authorised on battery; no battery benchmark contributes to the cost evidence. The 48-case audit and original validation checks are deferred by the owner's revised plan. This freeze is not a claim that the hidden-set pass mark has been met. The owner runs the comparison and may share only its summaries.
