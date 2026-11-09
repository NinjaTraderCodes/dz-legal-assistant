# Model setup for v0.10.0 — Phase 1 proposal

9 November 2026. Investigation only; implementation awaits the owner's go.

## Recommendation in plain words

Ship one Windows program with a **CPU-only cut of the official standalone Ollama engine**. Keep the lawyer's first decision simple: **Download the recommended model**, **Install from a file**, or **Not now**. Writing a case with templates must be possible immediately, including while a download runs.

The full engine ZIP is unnecessarily large for our CPU path. Its official archive is **1,614,941,966 bytes**; keeping the CPU files makes a working **29,790,886-byte ZIP**. Both Gemma 3 4B and Gemma 4 E4B ran from that smaller engine without elevation. There is no need to ship the tray app or ask the lawyer to type a command.

Use the owner's RAM rule as the recommendation, not a promise that every workload will fit: **16 GiB installed RAM or more → gemma4:e4b; below that → gemma3:4b**, with an available-memory check before loading. Prefer an already installed, verified local Gemma 4 over Gemma 3. Reuse a compatible existing Ollama instead of starting another on its port. Keep Gemma 4 thinking off through `OllamaModelChoice.ThinkFor`, recalculated for the model actually selected.

This removes the second installation, not the model download. The measured local payloads are about **6.58 GB** and **3.35 GB**. A slow office connection may still take hours; the USB route and continuing in template mode are essential parts of the first release.

## Scope and repository boundary

- Branch: `codex/model-setup`, created in a separate worktree from the latest fetched default commit **4e57c961ee9becd64c747df137a0ff41d17cb69d**. That source version is **0.9.1**; this document proposes 0.10.0.
- `AGENTS.md` was read completely before work. No app, legal engine, model provider, harness, prompt, data, default-branch or Alpha-03 source has been changed. No merge or pull request is part of this task.
- This phase adds only this proposal. Research downloads, archives, scripts and measurements stay outside the repository, under `%LOCALAPPDATA%\DzLegalAssistant\model-setup-probes\2026-11-09`.
- No hidden/evaluation folders or validation cases were opened. Evaluation results were not used. Inference used one generic Arabic question, twice per model; no case notes were involved.
- The offline-rule exception proposed here is **an explicit, cancellable model acquisition action**. The owner's go must precede implementation.

## Measurements on this laptop

### Machine and controls

Windows 11 Pro, build 26100; Intel Core i5-1335U, 10 cores/12 logical processors; Intel UHD graphics; **17,179,869,184 bytes installed RAM (16 GiB)**. Windows exposes about 15.68 GiB to applications. Counting OS-visible memory against a literal 16 GiB threshold would incorrectly recommend Gemma 3 on this 16 GB laptop; use installed memory for recommendation and available memory for readiness.

The owner connected mains power, selected Best performance and closed Brave. AC was verified connected; the effective overlay was `ded574b5-45a0-4f42-8737-46345c09c238` (Best performance). Browsers and VS Code were absent. A five-second CPU sample showed no other heavy job. No build, tests or other model inference ran during measurement. The 512 MiB available-physical-memory guard remained enabled; fewer than ten requests per model meant no ten-case reload boundary was reached. Ollama probes ran from **15:55:37 to 15:56:09 UTC**. These are short functionality checks, not legal quality or sustained drafting benchmarks.

Existing Ollama was version **0.40.15**, listening on `127.0.0.1:11434`, with no model loaded. It was not stopped, reconfigured or replaced. The probe used **127.0.0.1:11435**, and its owned processes were stopped afterwards. The existing server remained available and unloaded.

### Official archive and package cuts

Pinned source: [Ollama v0.40.15 release](https://github.com/ollama/ollama/releases/tag/v0.40.15), published 3 November 2026. Exact official sizes/digests came from its GitHub release API.

| Engine/package | Compressed bytes | Extracted file bytes | Contents and measurement |
|---|---:|---:|---|
| Official `ollama-windows-amd64.zip` | 1,614,941,966 | 2,263,717,391 | 80 ZIP entries; CPU, CUDA 12/13, Vulkan |
| Same archive, NVIDIA directories removed | 42,008,830 | 126,918,415 | 46 entries; retains Vulkan |
| **CPU-only cut** | **29,790,886** | **84,743,735** | 34 entries; removes CUDA and Vulkan |
| Official `OllamaSetup.exe` | 1,689,667,936 | Not installed in this investigation | Official guidance budgets at least 4 GB for the binary installation |
| Direct llama.cpp b12224 CPU x64 ZIP | 24,887,408 | 64,247,069 | 52 extracted files; separate alternative tested below |

GB is decimal; GiB/MiB are powers of 1024. ZIP counts include directory records. These are **engine increments**, not the final DZ Legal installer size. They exclude the app, packaging overhead and additional required licence notices. Models are additional. Complete package sizes before/after remain a Phase 2 deliverable.

Method: HTTPS byte-range requests read the official ZIP end record/central directory and fetched the retained local records. New ZIPs retained original compressed data and rebased central-directory offsets; both reconstructed archives passed every entry's ZIP CRC check. Extracted sizes come from actual records, not installed-folder guesses. **The full 1.615 GB archive was not downloaded; its whole-file SHA-256 was not independently checked.** Production must fetch and digest-check the complete pinned archive before applying this file selection.

- Official full ZIP SHA-256 published by GitHub: `9ca74a7ab66ec86870b1eecac7df160df08dc4e3a291ab5d2d59bcf9310221c6`.
- Reconstructed without-NVIDIA ZIP: `71c40cf2f6e501ac793729bd9786216e256d9a0eacbbcca10f7acbd49d3630ea`.
- Reconstructed CPU-only ZIP: `3c60bace0f9eaeb9a8b1f6f50a2db611d8a4322e30118ec66dc3a5c825a3dd5c`.
- Extracted `ollama.exe`: `544216ca2082b8bc19b4ba94b513f0098fa06774b232f3c140090e3449e322a5`; Windows Authenticode reported a valid Ollama Inc. signature.
- Official installer SHA-256 published by GitHub: `106ef3d2a02437810fba6f574914b1413c512ea84ddd4f184c8e7f434b6f5564`.

Exact CPU selection: remove `lib/ollama/cuda_v12/`, `lib/ollama/cuda_v13/` and `lib/ollama/vulkan/`; retain the root executable, **all** other `lib/ollama` runtime files, CPU architecture variants and bundled notices. Do not trim to this laptop's preferred CPU DLL; other lawyers need the fallback variants. Keep the directory layout. Review each future release's contents rather than silently accepting new files by wildcard.

The [official Windows guide](https://docs.ollama.com/windows) supports standalone embedding with `ollama serve` and describes per-user, non-admin installation. No normal installer, tray integration, service registration, PATH edit, registry edit or administrator prompt was used here.

### Start time, privilege and CPU operation

The parent Windows token was **not an administrator token**. The engine was launched directly as an ordinary child, without `RunAs`, from the per-user probe folder. Models, temporary files and the child's home were also under the app root. This demonstrates normal-user startup on this existing account; it does not replace the fresh-account test in Phase 2.

Child-only environment included:

```text
OLLAMA_HOST=127.0.0.1:11435
OLLAMA_MODELS=<probe root>\models
OLLAMA_NO_CLOUD=1
OLLAMA_NUM_PARALLEL=1
OLLAMA_MAX_LOADED_MODELS=1
OLLAMA_CONTEXT_LENGTH=4096
USERPROFILE=<probe root>\home
TEMP=<probe root>\temp
TMP=<probe root>\temp
```

These changed only the child environment, not the Windows account or existing server. `OLLAMA_MODELS` alone is insufficient: pinned source uses the process home for keys and server configuration. The log confirmed **cloud disabled** and literal loopback binding. Its CPU runner used `--offline`, `--no-webui` and zero GPU layers.

| Fresh engine process | API ready, seconds | Idle working set, MiB | Idle private bytes, MiB |
|---|---:|---:|---:|
| 1 | 0.541 | 27.65 | 53.31 |
| 2 | 0.529 | 26.84 | 53.46 |
| 3 | 0.544 | 26.72 | 53.18 |

Readiness was the first successful `/api/version`, polled every 50 ms. Idle memory was sampled two seconds later. These are fresh **processes**, with normal Windows file caching, not cold boots or a deliberately cold disk cache. Model loading is separate.

Existing public blobs were hard-linked into a private model store; manifests were copied. This saved disk, downloaded no model and did not edit originals. Twenty existing blobs were linked; one unavailable alternative descriptor remained unavailable. Both requested CPU models loaded without fetching it. This laboratory reuse technique is **not** the proposed USB importer.

Requests used CPU (`num_gpu:0`), context 4096, one request at a time, temperature 0 and a 32-token cap. Gemma 4 received `think:false`; Gemma 3 received no think field. Six CPU threads were reported. Question: `أجب بجملة عربية قصيرة: ما فائدة حفظ الملفات محلياً؟`

| CPU-only standalone result | gemma3:4b | gemma4:e4b |
|---|---:|---:|
| Empty-request model load, seconds | 4.606 | 8.422 |
| Two loaded request times, seconds | 2.227 / 2.668 | 2.224 / 1.912 |
| Mean loaded request, seconds | 2.448 | 2.068 |
| Aggregate decoding tokens/second | 12.88 | 11.80 |
| Peak process-tree working set, GiB | 3.472 | 6.466 |
| Peak process-tree private bytes, GiB | 3.918 | 7.271 |
| Minimum available physical memory, GiB | 6.829 | 4.223 |
| CPU / GPU allocation | CPU; zero VRAM | CPU; zero VRAM |
| Completed / cap reached | 2 / 1 | 2 / 0 |
| Thinking output | None | None |

The capped Gemma 3 answer is disclosed; this smoke-test cap is not a product answer limit. Throughput is total generated tokens divided by total decoding duration. Memory values are **separate sampled maxima** for the server plus descendants at 100 ms intervals; do not add working set and private bytes. No guard fired. Full legal workloads can use more memory/time.

Tags and live runner manifests differ in this release. Before inference, installed tag digests were Gemma 3 `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` and Gemma 4 `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. Live CPU runners reported `4794e21ef424939c86535c12407e6f8182fd36b730118ad88c26e738d1ebe910` and `ccd2e5fbe4af129173bfdbabe85a93e195c01460860a8a2066efe4d9362dc6f2`. Loaded weight blobs were respectively `aeda25e63ebd698fab8638ffb778e68bed908b960d39d0becc650fa981609d25` and `370c2879f17648d337b0ab4e208fdce348e24d1060064e93d7645daafd56d39c`; Gemma 4 also used its installed draft helper. Pin the complete runner-specific model closure in Phase 2, not just a tag.

No physical network disconnection or packet capture was performed. This proves CPU functionality and process isolation, **not** empirical network silence on every startup path. Source review and offline/network-observation acceptance tests remain necessary. No installer or clean-account behaviour is inferred from these starts.

### Direct lighter-engine probe

Pinned [llama.cpp b12224](https://github.com/ggml-org/llama.cpp/releases/tag/b12224), a 9 November prerelease; CPU x64 archive SHA-256 independently verified: `d7a7a3e014a02dbfce46977f9da6d9b58196b9c83b80bcf6ef2337134103c3d6`.

It was launched on `127.0.0.1:11436`, with `--offline`, no UI, CPU, six threads, one slot and context 4096. It **failed to load the installed Gemma 3 GGUF**: missing metadata key `gemma3.attention.layer_norm_rms_epsilon`. It exited before readiness; there is no valid generation speed or steady-state memory result. The Gemma 4 direct run was not reached. No metadata override, conversion or alternate model download was attempted.

This is bounded to these weights/release, not a claim that llama.cpp cannot run Gemma. Its [server documentation](https://raw.githubusercontent.com/ggml-org/llama.cpp/b12224/tools/server/README.md) provides offline mode and OpenAI-style endpoints. Qualifying our integration, templates, thinking, speculative decoding and structured answers would be extra work. Roughly 4.9 MB compressed saving against CPU Ollama does not justify making that a v0.10.0 prerequisite.

## Options compared

| Option | Size and first use | Speed evidence | Reliability / maintenance | Licence and offline behaviour |
|---|---|---|---|---|
| **A: guided normal Ollama install** | App adds little; separate 1.690 GB official installer plus model; a second install remains | Same pinned CPU backend should be similar with identical settings; no installer-based A/B timing performed | Least engine packaging work; separate tray/startup lifecycle and versions remain outside our control | MIT engine plus dependencies and model terms. Normal tray app downloads updates automatically; cannot offer click-only model networking for that independent app |
| **B: complete official standalone** | Adds 1.615 GB compressed / 2.264 GB extracted before app/model | CPU functionality measured on retained files; full CUDA bundle was not loaded for an A/B speed claim | Existing provider retained; app owns startup/shutdown/version; ships unused GPU libraries | MIT does not license every DLL. CUDA redistribution terms also apply. Can omit tray/updater, disable cloud and control our pull requests |
| **B improved: CPU-only standalone — recommended** | Adds 29.79 MB compressed / 84.74 MB extracted, plus notices, app and model | API ready ~0.54 s; both proposed models work; load/memory above | Matches existing `GpuLayers=0` default. Keep CPU variants; qualify runtime DLLs/pinned release; no dependency on a lawyer's GPU driver | Preserve Ollama, llama.cpp/vendor/Go and runtime notices. No CUDA payload. Managed cloud features off; acquisition on click; installed inference needs no Internet |
| **C: direct llama.cpp** | 24.89 MB compressed / 64.25 MB extracted plus app/model | No throughput result: supplied Gemma 3 weights failed before ready | Extra provider, model acquisition, template and compatibility work; not an Ollama API replacement | MIT core plus dependency notices; explicit offline execution; acquisition/import would be our responsibility |

The [Ollama FAQ](https://docs.ollama.com/faq) documents automatic updates in the normal Windows application and the `OLLAMA_NO_CLOUD=1` local-only setting. Reusing a user-owned Ollama does **not** give our app control of its updater. Disclose that boundary instead of silently changing it. Normal Ollama remains an advanced recovery route, not primary onboarding.

### Licence handling

The [pinned Ollama licence](https://raw.githubusercontent.com/ollama/ollama/v0.40.15/LICENSE) is MIT. Keep its notice adjacent to the engine, plus `GO_LICENSE`, `CPP_HTTPLIB_LICENSE`, `LLAMA_CPP_LICENSE` and `LLAMA_CPP_VENDORS_LICENSE` from the archive. Those aggregate files contain third-party terms. Audit the LLVM/OpenMP/MinGW runtime DLLs and add required notices not covered there; an upstream ZIP is not a blanket redistribution licence. CPU-only avoids CUDA; full B also needs the applicable [NVIDIA redistribution terms](https://docs.nvidia.com/cuda/eula/index.html) and runtime notices. No binaries/licence bundles are committed in Phase 1.

Models have separate licences. The installed Gemma 4 E4B licence blob is Apache 2.0, matching [Google's Gemma 4 licence](https://ai.google.dev/gemma/apache_2). Gemma 3 remains under [Gemma Terms of Use](https://ai.google.dev/gemma/terms), including referenced use restrictions and distribution notice requirements. Show a locally shipped copy of the relevant terms before download/import; preserve NOTICE/attribution files in offline packs. Do not label every model MIT or apply Gemma 3's terms to Gemma 4.

## Proposed first-start experience

Add a non-blocking **Local AI** card after existing lawyer/office setup, rather than a mandatory wizard page. Template work opens immediately when AI is unavailable. A persistent status area reopens the card from settings. If a compatible local model works, collapse directly to Ready without a download prompt. Preserve explicit advanced model choices; ordinary automatic choice prefers verified Gemma 4, then Gemma 3. No download begins on discovery, recommendation, reopening settings or a timer.

At 1366×768 the card scrolls internally and keeps **Not now / Continue with templates** reachable. At 1920×1080 it stays a bounded readable card. Arabic is default RTL; English LTR; model tags, bytes and paths use isolated LTR spans. Announce progress accessibly at useful intervals, with keyboard-reachable actions. Reuse workbench light/dark styles. No screenshot claims are made for unbuilt screens.

### States and proposed visible copy

Braces are formatted values, not raw engine errors. Implementation must put every string in both `Strings.resx` and `Strings.en.resx`. Shared state must not start desktop processes in the web demo.

| State / control | العربية | English |
|---|---|---|
| Card title | إعداد الذكاء الاصطناعي المحلي | Set up local AI |
| Checking | جارٍ التحقق من الجهاز والنماذج المثبتة… | Checking this computer and installed models… |
| Recommendation | نوصي بالنموذج {model} لهذا الجهاز. حجم التنزيل نحو {size}، والمساحة المتاحة {free}. | We recommend {model} for this computer. Download: about {size}. Free space: {free}. |
| Recommendation note | يعمل النموذج على جهازك. قد يستغرق تنزيله وقتاً طويلاً حسب سرعة الاتصال. | The model runs on your computer. Downloading may take a long time on a slow connection. |
| Download disclosure | عند الضغط على «تنزيل النموذج»، يتصل المحرك المحلي بخوادم تنزيل النموذج. لا تُرسل ملفات القضايا ولا النصوص التي تكتبها. | When you click “Download model”, the local engine contacts model download servers. Case files and the text you write are not sent. |
| Download | تنزيل النموذج ({size}) | Download model ({size}) |
| Terms link | شروط استخدام النموذج | Model terms |
| Offline route | تثبيت نموذج من ملف… | Install a model from a file… |
| Skip | ليس الآن — المتابعة بالقوالب | Not now — continue with templates |
| Preparing engine | جارٍ تشغيل المحرك المحلي… | Starting the local engine… |
| Discovering download | جارٍ التحقق من ملفات النموذج وحجم التنزيل… | Checking model files and download size… |
| Downloading | جارٍ تنزيل {model}: ‏{percent}٪ — ‏{downloaded} من {total} | Downloading {model}: {percent}% — {downloaded} of {total} |
| Speed and ETA | السرعة {speed} — الوقت المتبقي نحو {time} | {speed} — about {time} remaining |
| Unknown ETA / total | جارٍ التنزيل… {downloaded} — جارٍ حساب الوقت المتبقي | Downloading… {downloaded} — estimating time remaining |
| Cancel | إلغاء التنزيل | Cancel download |
| Background action | متابعة العمل بالقوالب | Continue working with templates |
| Paused / cancelled | توقف التنزيل. يمكنك استئنافه لاحقاً؛ ستُستخدم الأجزاء المحفوظة متى أمكن. | Download stopped. Retry later to reuse saved parts where possible. |
| Retry | إعادة المحاولة | Retry |
| Connection error | تعذر الاتصال بخادم التنزيل. تحقق من اتصال الإنترنت ثم أعد المحاولة. يمكنك استخدام القوالب الآن. | Could not reach the download server. Check your Internet connection and retry. You can use templates now. |
| Disk space | المساحة غير كافية. يلزم {required} والمتاح {free}. حرّر مساحة ثم أعد المحاولة. | Not enough disk space. Required: {required}; available: {free}. Free some space and retry. |
| Verification | اكتمل التنزيل. جارٍ التحقق من ملفات النموذج… | Download complete. Verifying model files… |
| Failed check | لم ينجح التحقق من ملفات النموذج. لن يُستخدم قبل اكتمال التحقق. أعد المحاولة أو ثبّته من ملف. | Model verification failed. It will not be used until verification succeeds. Retry or install from a file. |
| Import selected | تثبيت {model} من الملف المحدد — لا يلزم اتصال بالإنترنت | Install {model} from the selected file — no Internet required |
| Import progress | جارٍ نسخ الملفات والتحقق منها: {percent}٪ | Copying and verifying files: {percent}% |
| Bad/incomplete import | حزمة النموذج غير متوافقة أو تنقصها ملفات. اختر حزمة كاملة متوافقة مع هذا الإصدار. | This model pack is incompatible or incomplete. Choose a complete pack compatible with this version. |
| Loading test | جارٍ اختبار النموذج محلياً… يمكنك متابعة العمل بالقوالب. | Testing the model locally… You can continue with templates. |
| Ready | النموذج {model} جاهز. يعمل الذكاء الاصطناعي على هذا الجهاز دون اتصال بالإنترنت. | {model} is ready. AI runs on this computer without Internet access. |
| Use AI | استخدام الذكاء الاصطناعي المحلي | Use local AI |
| Existing engine/model | تم العثور على محرك محلي ونموذج متوافقين. لا يلزم تنزيل جديد. | A compatible local engine and model are already available. No download is needed. |
| Existing engine, missing model | المحرك المحلي موجود، لكن النموذج المطلوب غير مثبت. | The local engine is available, but the required model is not installed. |
| Occupied port | العنوان المحلي مستخدم. سنحاول عنواناً محلياً آخر دون إيقاف البرنامج الموجود. | The local address is in use. We will try another local address without stopping the other program. |
| Engine error | تعذر تجهيز المحرك المحلي. يمكنك متابعة العمل بالقوالب وإعادة المحاولة من الإعدادات. | Could not prepare the local engine. Continue with templates and retry from settings. |
| Low memory | الذاكرة المتاحة غير كافية لتشغيل النموذج الآن. أغلق البرامج الأخرى أو تابع بالقوالب. | Not enough memory is available to load this model now. Close other programs or continue with templates. |
| Engine lost | انقطع الاتصال بالمحرك المحلي. حُفظ عملك؛ يمكنك المتابعة بالقوالب أو إعادة المحاولة. | The local engine is unavailable. Your work is kept; continue with templates or retry. |

For an uncertain network error say **could not connect**, not **you have no Internet**: DNS, proxy, TLS, registry outage and disconnection are different causes. A sanitized details expander may show a code/recovery instructions, never a prompt, credential or raw log. Blame disk space only when the check or an out-of-space error supports it.

### Download and disk behaviour

1. Before any network request, show the shipped model identity, locally available terms, known payload estimate and disk requirement. The click authorizes that acquisition. No registry request just to display the card.
2. Call loopback `/api/pull` with an allowlisted local model and selected CPU runner; never pass lawyer text, document paths, arbitrary URLs or case files. After the click, obtain/validate exact runner-specific manifest and size through a supported pinned-engine flow; fail closed if the intended version cannot be identified. A mutable tag must not silently select the larger alternate MLX payload shown on today's model page.
3. Parse streamed newline-delimited JSON incrementally, including messages split across reads. Count bytes **per digest**, handling duplicate/out-of-order counters. A blob reaching 100% is not the entire model reaching 100%. Keep total indeterminate until a trusted complete manifest establishes it. Smooth speed over a recent window and suppress ETA until meaningful. Verification and initial load follow byte progress as separate states.
4. Cancel closes this request's stream and waits for work to stop. It does not delete the model or kill a reused server. The pinned [pull handler](https://github.com/ollama/ollama/blob/v0.40.15/server/routes.go) ties work to the HTTP context; [blob download source](https://github.com/ollama/ollama/blob/v0.40.15/server/download.go) retains partial metadata and cancels when its last waiter releases it. Another client sharing a blob may continue its own transfer. Retry reuses usable partials; corrupt/incompatible parts may not resume. Real interrupted-download testing remains required.
5. Disk budget: **remaining missing unique blob bytes + actual temporary/staging needs + reserve**. For USB, count the missing copied payload while the source pack remains in place. Do not always require twice the model: resumable blob files are not necessarily a second full copy. Recheck during acquisition. Propose a 2 GiB safety reserve initially, plus extracted engine storage; confirm against real peak disk use in Phase 2. Show the calculated requirement rather than a generic “10 GB”.
6. Initial measured local tag payloads are **3,349,593,375 bytes** for Gemma 3 and **6,583,656,505 bytes** for Gemma 4. These are not permanent promises about future tags. C: had about 9.47 GB free at task start; even this 16 GiB laptop needs a real disk gate. Do not begin an acquisition that cannot finish safely.
7. Pull success still requires all required files verified, an installed-model probe and a brief built-in local test using no case data. Never use the open case for setup testing or automatically switch its provider after a download. Offer **Use local AI** and preserve review/unsaved-work rules.

## Managed engine, existing engines and offline import

### Engine ownership and isolation

Package the pinned engine in the application's read-only payload, with licences beside it. Put mutable home, models, partials, bounded logs and temporary files under `AppPaths.Default.Root`. The probe used a user-folder executable; the release also needs the read-only-installation-folder test. Set child-only environment and a known working directory. Never change global/user `OLLAMA_HOST`, model paths or PATH. Launch the bundled absolute executable with fixed `serve` arguments, not a shell, bare `ollama`, tray program, welcome launcher, `ollama launch` or updater.

Set `OLLAMA_NO_CLOUD=1`, one loaded model and bounded context/parallelism; preserve current CPU defaults. Explicitly override inherited request-debug/history settings (`OLLAMA_DEBUG_LOG_REQUESTS=false`, no prompt/history logging) and sanitize credential-bearing proxy/auth fields before retaining diagnostics. Keep engine files/config accessible only under the ordinary user account; never persist raw prompts or authentication headers. Track process identity/lifetime and runner children with a Windows job/ownership mechanism. Drain bounded log pipes to prevent deadlock. Never kill a reused server, unload its unrelated models, delete its data or disable Windows services/security to make setup work.

Probe the existing loopback endpoint first with short timeouts. Reuse only a compatible Ollama whose chosen model resolves to local, complete, supported weights. Check metadata/identity before sending notes: rejecting names with a cloud suffix alone cannot establish an alias's locality. If locality cannot be established, use the managed engine or templates. A setup client can reuse the public `LoopbackGuard.CreateHandler()` without adding remote app HTTP or modifying the provider.

If 11434 is occupied, recognise/reuse compatible Ollama or start our engine on another available **literal 127.0.0.1 port**. Never stop the listener. Bound bind retries, handle the discovery/bind race, and verify that our process owns the listener before trusting readiness. Pass that endpoint to the existing provider. No `0.0.0.0`, service registration or hosted fallback. Loopback is a routing boundary, not authentication against malicious software running as the same Windows user.

Never silently pull into or reconfigure the user's model store. If its model is missing, an acquisition click must say which store receives it; recommend managed acquisition by default. Do not duplicate a model already usable through that server merely to gain ownership. Our app cannot promise that the independently installed Ollama has no update traffic.

Installed-model ranking belongs in app composition/choice, not the legal pipeline. Current code prefers `dz-gemma3-4b-text` when installed and does not automatically rank Gemma 4. Deliberately test the owner's Gemma 4 → Gemma 3 rule while preserving explicit advanced choices. Apply `ThinkFor(selectedModel, configuredThink)` **after** selection/fallback, including warm-up; a Gemma 3 startup must not leave a later Gemma 4 selection thinking by default.

### Offline file route

Use a documented, versioned **model pack**, provisionally `.dzmodel`, chosen through a file picker. A bare `.gguf` is not the supported one-click route for this release: templates, parameters, licence, config, projector and Gemma 4 draft-helper blobs can matter. [Ollama's import guide](https://docs.ollama.com/import) supports GGUF creation, but that alone does not preserve our qualified multi-file model.

Distribute complete packs prepared on an Internet-connected machine, or provide a future explicit export action. This phase builds no pack and initiates no model download. Packs contain an app-format manifest, engine/version/runner compatibility, canonical identity, byte sizes, the transitive required content-addressed blobs and notices. Public packs should carry an application-trusted authenticated manifest: SHA-256 values inside an untrusted archive alone detect corruption, not malicious replacement.

Validate entirely locally: refuse rooted/parent-relative archive paths, symlinks/reparse escapes, executable imports, excessive entry counts and decompression bombs. Stage missing blobs under the managed root, verify lengths/digests, then atomically expose a complete model. Existing models survive cancellation, disk-full, bad packs and power loss. Never publish a half-imported manifest or trust pack text as UI markup. Never repair missing blobs over the Internet automatically: request a complete pack or a separate download click. Test with the network disconnected.

The probe's hard links are not the production import route. Export no existing user's credentials/config. Runner-specific manifests make engine-version qualification necessary; reject incompatible packs rather than guessing. A complete USB route also needs a documented source for verified packs; an empty “choose a file” button alone does not meet the goal.

## Privacy and offline documentation proposed for Phase 2

Keep `docs/OFFLINE.md` and the live privacy page unchanged now: they describe today's app. Revise them with the feature, including app About/privacy copy, so the shipped description stays true.

In `docs/OFFLINE.md`, separate **case work/inference** from **explicit model acquisition**. Add download/import network-table rows; describe a verified loopback port rather than always 11434. State managed cloud features off, no tray updater or scheduled downloads; engine updates arrive only through an explicitly installed app release. Document model/partial/home storage separately from drafts, and deliberate model removal that never removes cases or another Ollama's weights.

Proposed English disclosure for card, documentation and privacy page:

> Case files, prompts and answers stay on this computer. Local analysis and drafting do not need Internet access after a model is installed. Only when you choose Download model or Retry does the managed local engine contact the model registry and its download servers. They receive your IP address, requested model/version and normal download connection information, including engine/platform information and any registry authentication needed for the transfer; they do not receive your case text. Nothing downloads automatically. You can cancel, install a model pack from a file, or continue with templates.

Proposed Arabic disclosure:

> تبقى ملفات القضايا والأسئلة والأجوبة على هذا الجهاز. لا يحتاج التحليل والتحرير المحليان إلى الإنترنت بعد تثبيت النموذج. لا يتصل المحرك المحلي المُدار بسجل النماذج وخوادم تنزيله إلا عند اختيار «تنزيل النموذج» أو «إعادة المحاولة». تتلقى تلك الخوادم عنوان اتصال الإنترنت واسم النموذج وإصداره وبيانات الاتصال المعتادة للتنزيل، ومنها معلومات إصدار المحرك والنظام وما يلزم للتحقق من طلب التنزيل؛ ولا تتلقى نصوص قضاياك. لا يُنزّل شيء تلقائياً. يمكنك إلغاء التنزيل أو تثبيت حزمة نموذج من ملف أو متابعة العمل بالقوالب.

Network semantics: **the app** still sends HTTP only to guarded `127.0.0.1`; **the engine** acquires model bytes over HTTPS after the click, following the registry's delivery flow. `OLLAMA_NO_CLOUD` disables remote inference/search, not model downloads. Do not describe download packets as never leaving the machine. Pinned [registry-request code](https://github.com/ollama/ollama/blob/v0.40.15/server/images.go) sends engine/platform User-Agent and may authenticate registry transfers. The setup card collects no lawyer account information; keys never enter diagnostics or support exports.

Update `site/src/pages/[lang]/privacy.astro`, `site/src/i18n/site.ar.json`, `site/src/i18n/site.en.json` and corresponding app resources together. Keep website/hosted-demo disclosures separate; neither acquires a desktop engine. Preserve **Official source** links: opening those fixed external websites already needs another click and carries no case data. Model acquisition is the new app-managed network action; an intentionally opened official website is not offline. Explain the independent updater boundary when reusing normal Ollama.

Verification: disconnect networking and exercise templates, imported-model inference and restart. Reconnect and verify startup/idle/reopening setup initiates no download. Click Download and observe registry/CDN traffic, then cancel and verify it stops when this is the sole pull. Verify real local inference produces no outbound engine connection. Exercise DNS/proxy/TLS errors without weakening `LoopbackGuard` or installing certificates. A failure to demonstrate the promised boundary blocks release, not template use.

## Phase 2 plan and acceptance — only after the owner's go

1. **Package:** fully download and SHA-256-check the pinned upstream release; create an explicit CPU-file manifest and dependency/licence inventory. Resolve binaries by packaged absolute path, never PATH. Updates remain manual through the app package. Measure complete package size before/after, extracted storage, installation peak and acquisition peak.
2. **Implement:** desktop engine/process, RAM/disk and acquisition services; a testable setup-state interface in Presentation; composition/views in App. Reuse `AppPaths.Default.Root` and public `LoopbackGuard`. Keep pull/import logic outside legal providers and preserve the inference contract and every case-safety behaviour.
3. **Test:** fragmented/invalid NDJSON, per-digest aggregate progress, unknown totals, duplicate counters, speed/ETA, cancellation/resume; exact 16 GiB installed-RAM boundary and OS-reserved memory; all states/Not now; disk and hash failures; incomplete/unsafe packs; read-only payload; startup timeout/recovery; existing compatible/incompatible Ollama, a non-Ollama listener and bind races; ownership on exit; cloud alias refusal; selected-model thinking off. Delete, skip or weaken no existing test.
4. **Network acceptance:** prove discovery/startup cannot invoke pull and only acquisition actions can. No case text enters setup/diagnostics. Mock loopback proxy/redirect tests supplement, not replace, guard tests. Run actual interrupted-download, offline USB and network-observation checks; unit tests alone do not prove silence.
5. **UI/web:** keep the web demo's service graph inert for desktop setup and all web tests passing. Every visible string goes into both resource files; compiled bindings, RTL Arabic default. Capture and personally inspect **every state** in Arabic/English, light/dark, 1366×768 and 1920×1080, outside the repo. Deterministic service fakes cover rare errors alongside real download/import checks.
6. **Clean account:** on a clean non-admin Windows user, install one program with no pre-existing Ollama, open, download on click, complete first analysis on fictitious notes, restart offline, cancel/retry and import USB offline. Test existing Ollama separately. Confirm no tray auto-start, remote inference, case upload or automatic download. Record download/load versus inference times, peak memory/disk and complete package sizes. Timings use mains, Best performance and no other heavy work.
7. **Repository checks:** after every commit, Release build with zero warnings/errors and complete Release tests all green. Preserve export blockers, AI authorship, lawyer review, recovery, unsaved-work prompts and whole long texts.

**Protected areas:** this plan does not require edits to `src/DzLegal.ModelProviders.*`, Drafting, Law, Domain, FormEngine, Documents, Harness or `data/`. If implementation finds a need, stop and ask first with the file, reason and alternative. Calling the guard's public API does not require changing it. Any storage helpers must stay under the existing app root. No legal/prompt change is implied.

### Evidence limits and decisions

- CPU-only startup and two-model smoke tests passed on the existing ordinary account. Clean-account installation, a real pull cancel/resume cycle, complete USB import, read-only install, packet-level/offline acceptance and the proposed screens are **not implemented or claimed tested**.
- Whole upstream-ZIP digest verification and dependency-specific notices remain packaging prerequisites. Byte-range extraction plus a valid signed executable describe this investigation, not the production supply-chain procedure.
- A's similar CPU speed is an inference about retaining the backend, not fresh installer-versus-bundle measurement. C has a recorded compatibility failure, not invented throughput.
- Smoke-test memory peaks are lower bounds for real work. Keep the recommendation but check available RAM before loading and retain templates on small/busy machines. Hardware below 8 GiB needs qualification before advertising local AI support; the “less than 16 GB” rule does not prove Gemma 3 fits every such machine.
- Pin CPU runner and complete identity. The [current Gemma 4 model page](https://ollama.com/library/gemma4:e4b) exposes alternative runner payloads, so a tag alone is not immutable size/compatibility.
- Product choices proposed for approval: CPU-only B, app-owned mutable storage, a versioned authenticated offline pack and the non-blocking bilingual card. This report changes no runtime model or defaults.

### Phase 1 change and verification record

Only `docs/MODEL-SETUP-PROPOSAL.md` changes. No files outside the requested report change; no tests are added or updated for this investigation. The untouched base built in Release with **0 warnings, 0 errors**. Required post-commit Release build and complete test results accompany the delivered commit. Local evidence files include `archive-entries.json`, `archive-measurements.json`, `engine-measurements.json` and the failed direct-engine log; probe scripts are retained beside them outside the repository.

The Windows computer-use helper could not connect to its native pipe, including prescribed retry/reset. The owner was asked to open this branch's built app, use template mode and exercise a fictitious sample, and replied **“Ready”**. This is owner-assisted manual confirmation, not an agent-observed screenshot or a test of the unbuilt setup. Screenshots/clean-account testing belong to Phase 2.

Stop after pushing this report. Await the owner's go before changing the application.
