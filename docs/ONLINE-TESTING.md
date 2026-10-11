# Online testing (temporary)

During the testing phase we measure how well and how consistently the AI proposes articles and writes the narrative sections. The local model is slow on the team's laptops, so the same hosted engine the web demo uses (Gemma 3 4B on OpenRouter) can be used in two places:

1. **A testing build of the desktop app**, with the online engine as a second choice beside the local model.
2. **The test harness**, which runs the fictitious cases many times and reports correctness and stability.

**This is temporary and is for fictitious cases only.** With the online engine selected, the facts and notes typed into the app are sent to OpenRouter and on to the model's host. The product the lawyers receive never contains this option (see [OFFLINE.md](OFFLINE.md)).

## The desktop testing build

The online engine exists only when the app is built with `-p:OnlineTesting=true`, and it appears only when the `OPENROUTER_API_KEY` environment variable is set. The normal build doesn't reference the hosted providers at all, and `OfflineGuaranteeTests` checks this.

PowerShell:

```powershell
$env:OPENROUTER_API_KEY = "sk-or-..."     # this window only; never commit or paste the key
dotnet run --project src/DzLegal.App -c Release -p:OnlineTesting=true
```

Bash:

```bash
OPENROUTER_API_KEY=sk-or-... dotnet run --project src/DzLegal.App -c Release -p:OnlineTesting=true
```

- The engine list then offers a third option: **"Online test: google/gemma-3-4b-it via OpenRouter (case data leaves this computer)"**. The app still starts on the local model.
- While the online engine is selected, the green "offline" chip in the top bar becomes an amber warning: "Online test engine: case data is sent to OpenRouter".
- The window title ends with "(online testing build)", so screenshots and recordings from this build can't be mistaken for the product.
- It uses the desktop's temperature and token limit, so its results can be compared with the local model. There is no fallback to another model, so every run is measured on the same one.
- Optional variables:
  - `OPENROUTER_MODEL` picks another model (default `google/gemma-3-4b-it`).
  - `OPENROUTER_ENDPOINT` points to another OpenAI-compatible server.
- The testing build goes to its own folders (`bin/Release-online`, `obj/Release-online`), so it never mixes with a normal build.

**Never pass `-p:OnlineTesting=true` when publishing the app for a lawyer.**

## Batch runs in the harness

The harness (`src/DzLegal.Harness`) is a maintainer tool and is never shipped. `--models` takes a comma-separated list:

| Spec | Engine |
|---|---|
| `gemma3:4b` (any other name) | Local Ollama model |
| `stub` | Template mode, no AI |
| `openrouter` or `openrouter:<model>` | OpenRouter; key in `OPENROUTER_API_KEY` |
| `google:<model>` | Google AI Studio; key in `GOOGLE_API_KEY` |

Options:

- `--runs N`: run every case N times. The outputs go to `<out>/<model>/run-<k>/` (grounds: `<out>/run-<k>/<model>/`).
- `--case <name>`: run one case only, to test "the same case N times".
- `--delay-seconds S`: wait between model calls, to respect OpenRouter's rate limits on free or low-tier keys.
- `--rate-limit-wait-seconds S` (default 300) and `--max-consecutive-failures N` (default 3; 0 = never stop): see "Rate limits" below.

Before starting, the harness prints how many model calls the run will make.

### Grounds analysis: does it find the right claim and articles?

```bash
OPENROUTER_API_KEY=sk-or-... dotnet run --project src/DzLegal.Harness -c Release -- grounds --models openrouter --runs 5 --delay-seconds 2 --out bench-grounds
```

`bench-grounds/summary.md` holds one table per run and, for more than one run, a **stability** table per model and case:

- **Claim agreement:** share of runs giving the most common claim type.
- **Claim exact:** share of runs where that first claim type is the expected one.
- **Article agreement:** mean overlap (Jaccard) of the proposed article sets between runs. 100 % means the same articles every time.
- **Model article agreement and model recall:** the same, but only for the articles the model chose itself. The catalog's required articles are added to every analysis without the model, so the final recall alone would hide what the model contributes.
- **Failures:** runs where the engine errored. The batch carries on.

### Drafting: is the written text correct?

```bash
OPENROUTER_API_KEY=sk-or-... dotnet run --project src/DzLegal.Harness -c Release -- draft --models openrouter,gemma3:4b --runs 3 --case 01-debt-arabic --out bench
```

`bench/summary.md` has one row per model, case and run. The columns include:

- **wrong law:** articles misquoted, given content the official text doesn't support, or cited from memory;
- **wrong-law paragraphs removed;**
- **legal points:** the substance checks, for example damages asked without a formal notice;
- whether the draft fell back to the lawyer's notes;
- time per section.

For more than one run, a stability table gives the mean and range per model and case. Read the outputs as well: the automatic checks can't see swapped roles or invented reasoning.

## Rate limits ("429 … temporarily rate-limited upstream")

OpenRouter's free and shared capacity for Gemma 3 4B sits at the company that runs the model (DeepInfra, for example). When it's busy, it refuses requests for a minute or two with error 429. That is their limit, not a problem with your key or with the app.

- **Desktop testing build:** a refused request is retried automatically for up to **90 seconds**, as long as the service asks, or else after 10 s, 20 s, 40 s… If it is still refused, the app says "The online service is busy right now…" instead of showing the raw error. Wait a minute or two and press the button again, or switch to the local model.
- **Harness:** each call waits out a refusal for up to **5 minutes** (`--rate-limit-wait-seconds`) and prints `rate-limited, waiting 20 s…`. If 3 cases in a row still fail (`--max-consecutive-failures`), it stops that model's batch, skips its remaining runs, and marks the summary "stopped early after k of n cases". Calls that never ran are not counted as model failures.
- **What helps:**
  - add `--delay-seconds 2` or `3` for long batches;
  - run outside busy hours;
  - optionally, add your own provider key in OpenRouter's settings → Integrations (the error message suggests this), which gives you your own limit instead of the shared one.

## Removing it

When testing is over:

1. Delete the `OnlineTesting` block in `src/DzLegal.App/DzLegal.App.csproj`.
2. Delete the `#if ONLINE_TESTING` sections in `src/DzLegal.App/Composition.cs`.
3. Delete the testing-build test in `tests/DzLegal.App.Tests/OfflineGuaranteeTests.cs`.
4. Delete this page and its mentions in `OFFLINE.md` and the README.

The warning chip and the `SendsCaseData` flag can stay, because the web demo uses them too. The harness's online models can stay as well, since the harness is never shipped.
