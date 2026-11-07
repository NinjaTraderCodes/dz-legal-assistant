# Frozen fitting references

Run the existing Alpha-01 reader/provider/parser/evaluator unchanged on the 96 fitting members of the reviewed second freeze. Confirmed claim types, 300-token cap, CPU-only context 4096, temperature 0.2, repeat penalty 1.1 and normal 15-minute residence are retained. `gemma3:4b` has no thinking capability and omits that option; `gemma4:e4b` uses the provider's `Think = false`. No scoring prompt or candidate reader is used here.

Run models serially, each starting unloaded; retain the runner across that model's cases, with no per-case reset or retry. Cold first-load time is included. Preserve cap hits/partial output and all failures in the record; do not raise the cap. A transport failure, context truncation or measurement precondition failure stops the batch and prevents the next model/candidate step. Semantic critical errors are reference results, not a reason to change the reader.

An idle real app must remain resident. Build first and shut down build servers before timing. The existing resource monitor samples CPU/memory/paging/power throughout. Before each request the recorder requires a fresh sample, mains and actual/effective/configured Best performance, app residence, at least 512 MiB available physical memory, and model private bytes plus app working set no larger than visible physical RAM. This conservative operational guard protects headroom; an interruption does not by itself measure the maximum feasible allocation. No cause investigation of the earlier resident-gemma3 growth is undertaken.

```powershell
dotnet build tools/alpha-03/FittingReferences/FittingReferences.csproj -c Release
# Start the existing resource monitor and idle app, then verify quiet conditions:
& tools/alpha-03/Measure-FittingReferences.ps1 -RunRoot $externalRunRoot
```

The host pins the reviewed case-file/content hashes and selects manifest fitting IDs before deserializing cases. No audit case is sent to a model. Existing-evaluator summaries are saved overall and separately for polarity/binding families; exact case times, native timings, facts, raw answers and outgoing wire requests remain external. Resource phases identify model, family kind and case. Per-kind memory peaks come from a resident chronological batch, not isolated measurements of family complexity.

Reference supplementary analysis is declared before inference: report all asked event statuses, explicit absences versus omitted/unresolved decisions, non-affirmed errors, false affirmations, known-value omissions/wrong attachments, request presence/amounts and evidence coverage separately from the unchanged critical-error score. A missing/invalid decision is never imputed as a correct absent label. Original sparse evaluator results retain their original meaning; no retrospective scoring change follows model answers. There is no audit/validation run or candidate code in this reference host.
