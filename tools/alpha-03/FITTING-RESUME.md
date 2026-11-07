# Stopped fitting-stage checkpoint

Branch `codex/alpha-03`; source base `e539b8e13c075974ca8c778c90b6e88b6905be99`. Frozen corpus hash `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642`; never edit its payload.

The first fitting-reference run stopped on its runtime precondition. External root: `%TEMP%\dzlegal-alpha03-fitting-20261107-211459`. Gemma3 completed 25/96 fitting cases; request 26 was blocked before HTTP/native timing because the latest available-memory sample was 507.668 MiB, below the 512 MiB guard. The wrapper exited nonzero and never started gemma4. No scored candidate/calibration/binding/audit exists. Do not resume automatically, retry completed calls or treat this as a full reference comparison. Read the interrupted-run summary and owner's decision first.

Cleanup completed: models unloaded; owned app PID 12396 closed normally; monitor PID 17352 stopped using `monitor.stop`; Windows Search restored Running/Automatic after its authorised temporary pause. Best performance retained, owner processes untouched. These process IDs are historical, never authority to close a future process. No heavy benchmark remains active.

The reference guard checks fresh samples, mains/Best performance/app residence, available physical >=512 MiB, model-private + app-working-set <=physical. Its interruption is operational headroom failure, not proof of maximum model capacity. The separate resident-gemma3 growth issue was not investigated. No rescue reset, cap increase, new residence protocol or guard relaxation followed. Preserve partial evidence separately if continuation is authorised.

Owner clarifications: a model passing score/binding gates may continue if the other fails those quality gates. Mandatory missing-score/cache/context/runtime precondition failures still stop/report. Final audit is one locked batch on both models, but primary must be chosen from fitting and recorded before audit; only primary is gated, secondary cannot replace it afterward. No original validation check is authorised. Audit remains untouched by inference.

No hidden paths, hidden-v2 brief or external history folders may be opened. Owner's pre-existing untracked files remain untouched. New measurement source/docs and required verification/push are the only remaining handoff work for this stopped stage.
