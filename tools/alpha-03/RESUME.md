# Resume Alpha-03 from standalone PowerShell

**Handoff completed on 7 November 2026.** The owner closed VS Code, the agent resumed in standalone PowerShell, and all 40 forward chains, three reversed chains and both 40-case unchanged Alpha-01 references finished. Current results are in `docs/baselines/2026-11-07-alpha-03-gate-2/summary.md` and `docs/ALPHA-03.md`. Measurement processes/models were stopped. The current stop remains **before writing any cases**; do not rerun these instructions automatically. The text below is the historical migration checkpoint and its pending-work statements have been superseded by the completed report.

The owner asked to pause so the agent can be opened from standalone PowerShell and VS Code closed. This checkpoint is local and uncommitted. The task remains gate 2, reverse-order check, rung 0, commit/push tools and results, required checks, then stop before writing any cases.

## State and rules

- Repository: `C:\Users\TheShadow\dz-legal-assistant`.
- Work only on `codex/alpha-03`; HEAD and last successful push: `5dc52cbf24b670f6f9dad39d294676e29bd2b1ff`.
- Never merge/rebase/cherry-pick into another branch, open a PR, force-push or delete branches. Never access hidden/evaluation inputs, `C:\dz-hidden*`, or the hidden-v2 brief.
- Report 1 is approved. Gate 1 passes on append-only shape (b). Before candidate prompt/reader work, all 144 author cases must be written, checked and frozen with a hashed split manifest. **No new cases may be written until the current measurements are finished and reported.**
- Read `AGENTS.md`, the brief, the latest `docs/ALPHA-03.md` sections and the interrupted-run summary before acting. The longer diagnosis/history and owner annotation rules are already in that report. No original cases or source files have changed.

## What happened

Mains was initially disconnected. The owner connected it and requested **Best performance for every timed run**, with the mode recorded in each summary. Native Windows queries still reported efficiency mode; the requested AC mode was applied through `PowerSetUserConfiguredACPowerMode`, then actual/effective/configured AC GUIDs all verified as `ded574b5-45a0-4f42-8737-46345c09c238`. Keep this setting. A Balanced base plan is compatible with that overlay.

The timing attempt completed 21 chains / 85 status responses. All 85 had four finite single-token scores; 64/64 extensions reused 355–526 tokens, all first calls cached zero. Chains averaged 12.780 s plus separately measured 4.225 s empty warm-up. These are **contended diagnostics, not qualifying times**: C# Dev Kit/VS Code background work consumed substantial CPU. Available physical RAM briefly fell to about 46 MiB, despite model/server sampled working sets peaking at 4.019 GiB. The CPU and memory controls need a fresh check after leaving VS Code.

The benchmark was interrupted; zero reverse-order chains and zero rung-0 inferences ran. Do not treat this as a failed semantic/accuracy experiment or pool its timings with the clean run. No correctness-driven prompt changes occurred.

Raw interrupted artifacts: `%TEMP%\dzlegal-alpha03-gate2-20261107`. Accepted gate-1 scalar fixture: `%TEMP%\dzlegal-alpha03-gate1-shapes-20261106-213237\b-1-request.json`, SHA-256 `cb95460ba73413548a706f759a54d6f149eb7d70831355788a2508f159b29be3`.

## Continue

1. Confirm branch/worktree. Preserve all pre-existing owner untracked files; stage only explicit Alpha-03 files. Pending authored files are `docs/ALPHA-03.md`, the interrupted-run summary, and `tools/alpha-03/`.
2. Verify mains and **Best performance** with `Power.ps1`; check background CPU/memory after VS Code is closed. Windows blocks script files by default: use `powershell -NoProfile -ExecutionPolicy RemoteSigned` for these local scripts, or set RemoteSigned for the current process only. Never change a persistent execution policy.
3. Use a **new external output directory**. Follow the README to export original development inputs and make the same 40-case/147-question manifest. The old manifest hash is `9ff1cda372f8c9846d918b2ebc7b86945f93084a1536d11e1ab8dd1153f0581c`; it and the old development-input export can also be copied from the interrupted directory. No evaluation input is involved.
4. Run an idle desktop app and `Measure-Resources.ps1` in a hidden helper process; await a complete sample and finish app startup before timing. Previous own processes were app PID 24180, final monitor PID 23436; they were asked to stop, and the model was unloaded. Check their state rather than blindly acting on reused PIDs.
5. Run `Measure-OrderedChoices.ps1 -RunRoot <new directory> -FixturePath <accepted fixture>`. It runs all 147 event questions, then the three already selected reverse orders: `loan-no-section`, `loan-basic`, `non-delivery-machines`. All settings/wording stay as the accepted fixture, actual generated history is retained, each case starts fresh, and cold runner loading is separate. Report generated-label and four-score-argmax changes under reversal without claiming causal isolation from sampling variation.
6. Run `Measure-Alpha01.ps1 -RunRoot <new directory>` serially. It invokes unchanged Alpha-01 through the existing harness, gemma3 then gemma4, original development only: four out-of-catalogue cases excluded, **40 inference cases per model**. Preserve default thinking behaviour. Report licence, full digest, seconds/case, each critical error and memory, with **mains/Best performance in every summary**. Add the retained gemma3 Alpha-01 cost to chain cost when judging the proposed complete reader's 90-second budget.
7. Stop the own monitor via `<runRoot>\monitor.stop`, close the own idle app normally, unload own models, keep Best performance. Summarise results in `docs/baselines/` and `docs/ALPHA-03.md`; raw outputs stay external.
8. Commit and push only to `codex/alpha-03`. After every commit run `dotnet build -c Release` (zero warnings/errors) and the full Release tests. The known unload test failed under collection concurrency, passed alone, and all **990 tests passed** using `dotnet test -c Release --no-build -m:1 -- xUnit.ParallelizeTestCollections=false`. No tests are skipped/weakened and the race remains disclosed. Do not run checks concurrently with benchmarks.
9. **Stop and report before any case writing**, as the owner requested. Neither the timing gate nor rung 0 has been completed yet.

The tools are measurement fixtures, with UTF-8 BOMs for Windows PowerShell 5.1 Arabic literals; they contain no candidate reader or new prompt wording. Validation checks used: zero. Hidden data accessed: none.
