# DZ Legal Assistant

**The Grammarly for the Algerian statement of claim, which never leaves the lawyer's laptop.**

A desktop app that helps Algerian lawyers draft the *requête introductive d'instance* (statement of claim) in formal Arabic, checks that every element the law requires is present, and proposes the articles from the official texts. It runs offline: client files stay on the lawyer's own computer.

| 🌐 [Website](https://dz-legal-assistant-ai.onrender.com) | ▶ [Live demo](https://dz-legal-assistant-ai.onrender.com/app) | ⬇ [Download for Windows](https://github.com/NinjaTraderCodes/dz-legal-assistant/releases/latest) | 🎬 [Demo video](https://drive.google.com/file/d/1zFbKYu2HWJIQ3-o1_QB_Zs6JGhvXqLFW/view?usp=sharing) | 📄 [Pitch deck](https://drive.google.com/file/d/1k9K6tWyQsFWDLof60efMlhcdFuaJivwY/view?usp=sharing) |
|---|---|---|---|---|

![The desktop app in English, dark theme: the Unpaid loan sample with its legal grounds drafted by the local model gemma3:4b, the review box ticked, 9 of 9 mandatory elements present](docs/images/readme/desktop-en-dark.png)

*The desktop app, offline, with the local model (Gemma 3 4B): the legal grounds drafted and reviewed, 9 of 9 mandatory elements present. The same screen [in Arabic](docs/images/readme/desktop-ar-dark.png), the app's default language.*

## The problem

In Algeria, a statement of claim that omits one element required by the Code of Civil and Administrative Procedure (CPCA) can be rejected on form, before a judge reads the case. Lawyers check these elements from memory, under time pressure. Finding the right articles for the facts takes most of the drafting time. And online AI tools are not an option: sending a client's file outside the office breaks professional secrecy.

## How it works

1. **The lawyer enters the case**: court, parties, facts and requests, in nine guided steps. The facts and requests stay exactly as the lawyer wrote them.
2. **The AI reads the facts** and proposes the type of claim and the articles, chosen only from the official texts loaded in the app.
3. **The lawyer confirms the claim type and ticks the articles.** Nothing is used before that confirmation.
4. **The app drafts the legal grounds.** The code quotes each rule from the official text; the AI writes only how the facts meet it. Every AI-written section is marked and must be reviewed before the statement of claim can be exported.
5. **A rule engine, with no AI, checks every mandatory element** and names the article behind each missing one. Then the statement of claim is exported to PDF or Word.

### The AI at work: a real run on a laptop

Frames from the [demo video](https://drive.google.com/file/d/1zFbKYu2HWJIQ3-o1_QB_Zs6JGhvXqLFW/view?usp=sharing), recorded live with the local model (Gemma 3 4B) on an ordinary laptop without a graphics card. The waits are real and shown sped up; the banner gives the true duration.

| | |
|---|---|
| ![The local model reading the facts: the three analysis stages and the true duration, 3 min 17 s](docs/images/readme/live-ai-reading-facts.jpg) | ![The AI's reading of the facts and the question «Is this the type of claim?»: nothing is ticked until the lawyer confirms](docs/images/readme/live-ai-proposal.jpg) |
| **1. Reading the facts.** The local model reads the case and proposes the claim type (3 min 17 s on this laptop). | **2. The lawyer decides.** The AI's reading is shown with the question "Is this the type of claim?"; nothing is ticked before "Yes". |
| ![The local model drafting the legal grounds from the confirmed articles: 2 min 33 s](docs/images/readme/live-ai-drafting.jpg) | ![The drafted legal grounds in Arabic, with the review box the lawyer must tick before export](docs/images/readme/live-ai-drafted-grounds.jpg) |
| **3. Drafting the legal grounds** from the confirmed articles (2 min 33 s). The code quotes each rule; the AI links the facts to it. | **4. The lawyer reviews.** The AI-written section must be ticked "I have reviewed this section and take responsibility for it" before any export. |

The app also includes a first-run setup that fills the signature block of every statement of claim, a guided tour, a sample case, help pages, and a law library showing each text's official source and how it was checked. It works in Arabic and English, in light and dark themes; the statement of claim itself is always Arabic, right to left.

![The law library: each text with its official reference, its edition and how it was checked](docs/images/readme/law-library-en.png)

## Results so far

**What works without any AI**

| What | Result |
|---|---|
| Automated tests | **1,030**, all passing |
| Form checks (rule engine, no AI) | **360 of 360** correct on 24 test statements of claim |
| Law texts | **1,361 articles** from 6 texts, including the complete Family Code and the complete CPCA |
| Claim types covered | **21**: civil, commercial, family and labour |

**How well the AI reads a case**

Measured on test cases written by separate AI agents and never opened by the developers. The task: read the facts and find each party, event, date, amount and request, and whether each fact is stated, denied or disputed. The newest measure is a second, independent set (8 November 2026), run once per model on the laptop, on mains power, by the owner:

| Same 90 cases, claim type given | Local model **Gemma 4 E4B** | Local model Gemma 3 4B |
|---|---:|---:|
| Plaintiff's position right | 100% | 94% |
| Decisive events found | 98% | 89% |
| Dates linked to the right event | 92% | 83% |
| Stated, denied or disputed read correctly | 91% | 86% |
| Requests found | 95% | 92% |
| **Cases with every detail right** | **64%** (58 of 90) | **43%** (39 of 90) |
| Time per case, laptop without a graphics card | 21 s | 28 s |

"Every detail right" is the strictest score: one wrong date or amount fails the whole case. These figures measure our fact reader (Alpha-01) in the test bench; the desktop app's own analysis and drafting with Gemma 4 are measured separately. Most of Gemma 4's remaining errors are one kind: a fact the other party disputes, read as stated.

On the first set (31 October 2026, another 90 cases), the online model (Nemotron 3 Ultra) got **71%** of cases fully right in 4 s per case, against 36% for Gemma 3 4B on battery. When it also reads the claim type itself, as in the live demo, it finds the right type in **94%** of cases (85 of 90), gets **67%** fully right, and declines **all 10** out-of-scope cases instead of guessing.

**What this means for the lawyer**

- **The parts that make a statement of claim admissible do not depend on the AI.** The form check is pure code, and the law is quoted from the official texts by code.
- **The AI only proposes.** The lawyer confirms the claim type before anything is used, and reviews every AI-written section before export. The local model reads most details right, but can still miss one per case, which is exactly what these confirmations are for.
- **Two ways to get a stronger AI, both planned:** an optional online mode, where client names are hidden on the laptop and the lawyer sees exactly what is sent before it leaves; and a larger local model for 16 GB laptops: Gemma 4 E4B, measured above.

Method and run records: [docs/HIDDEN-SET.md](docs/HIDDEN-SET.md) and [docs/baselines/](docs/baselines/).

## Try it

- **In the browser, nothing to install:** [the live demo](https://dz-legal-assistant-ai.onrender.com/app) runs the same engine, law library and screens, with four fictitious sample cases. Its AI uses a larger hosted model, so it is for fictitious data only.
- **On Windows 10 or 11:** download the zip from the [latest release](https://github.com/NinjaTraderCodes/dz-legal-assistant/releases/latest), unzip it and run `DzLegalAssistant.exe`. It works fully offline. Without a model, choose "Without AI (fill-in templates)" in the draft panel. For the AI, install [Ollama](https://ollama.com) and run `ollama pull gemma3:4b` once.
- **From the source code** (requires the [.NET 10 SDK](https://dotnet.microsoft.com/download)):

```bash
dotnet build                          # warnings are errors
dotnet test                           # all automated tests
dotnet run --project src/DzLegal.App  # the desktop app
dotnet run --project src/DzLegal.Web  # the web demo, locally
```

## Team

| Who | Role | What they did |
|---|---|---|
| **Hicham Benkaihoul** | Founder and developer | Defined the product and its legal pipeline, directed and reviewed the AI coding agents that wrote most of the code, ran the benchmarks and the test campaigns, and tested the app. |
| **Djihane Bendjeddou** | UI/UX designer | Designed and built the lawyer's journey in the desktop app: the welcome screen, the setup, the lawyer's profile, the start screen, the guided tour with its sample case, the help pages and the law library, in Arabic and English, light and dark. |
| **Khouloud Otmane Cherif** | Legal reviewer | Master's in private law, works in an Algerian law office. Reviewed legal points of the drafting and answered the team's questions on Algerian civil procedure and claim types, which shaped the family claims. |

**AI tools used to build it.** Most of the code, tests and documentation were written by AI coding agents, directed and reviewed by the team: Claude Code (most of the app), OpenAI Codex (the law corpus, part of the workbench, the independent test cases) and OpenCode (the onboarding screens). The welcome image was generated with ChatGPT. Inside the desktop app, the only AI is the local model (Gemma 3 4B through Ollama); the web demo uses Nemotron 3 Ultra through OpenRouter.

**Tech stack.** C# and .NET 10; Avalonia (desktop interface); Blazor Server (web demo); QuestPDF (PDF) and Open XML SDK (Word); Scriban (templates); Ollama (local model); OpenRouter (the web demo's hosted model).

## Status and limits

- **The legal rules and the claim catalogue are not yet verified by a practising lawyer.** Every rule is marked unverified in the data and in the app; an independent audit in October 2026 found errors in the catalogue, and corrections are under way. Checklist: [docs/LEGAL-VERIFICATION.md](docs/LEGAL-VERIFICATION.md).
- **One document type so far:** the statement of claim. The Civil, Commercial and Labour codes are only partly loaded; the complete Civil Code is being checked against the Official Journal.
- **The app drafts; it does not decide.** It does not judge the merits or guarantee a result, and the lawyer reviews and signs every statement of claim.
- **The web demo sends what is typed to a hosted model:** fictitious cases only. The desktop app sends nothing.
- **The local model is the weak point today:** it is slower and less precise than the online model (table above). The desktop app stays offline by default; the online mode will be the lawyer's choice, case by case.

## What's next

- **Month 1:** an online mode for stronger AI, with client names hidden on the laptop before sending and a preview of exactly what is sent; executable legal rules reviewed with our legal reviewer.
- **Month 2:** version 1.0 as a Windows installer, with the complete Civil Code and the texts reviewed by a practising lawyer.
- **Month 3:** a pilot in five to ten law offices, measuring the time saved and the defects caught, and preparing the yearly subscription for the AI analysis and the law updates.

## Documentation

- [docs/PRODUCT-DETAILS.md](docs/PRODUCT-DETAILS.md): the full technical description: analysis, drafting, checks, exports, local model set-up, benches and repository layout.
- [docs/website/](docs/website/): the public website (`site/`, served by the demo's app at `/ar/` and `/en/`): one report per phase with screenshots, Lighthouse and axe results. How it is wired: [docs/WEB-DEMO.md](docs/WEB-DEMO.md).
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): modules, data flow and design decisions.
- [docs/DEVELOPER-GUIDE.md](docs/DEVELOPER-GUIDE.md): which file does what, where every prompt lives.
- [docs/OFFLINE.md](docs/OFFLINE.md): what "offline" means here and how to verify it.
- [docs/LAW-LIBRARY.md](docs/LAW-LIBRARY.md): the law texts and how each was checked.
- [docs/WEB-DEMO.md](docs/WEB-DEMO.md): the web demo and how to run it.
- [docs/HIDDEN-SET.md](docs/HIDDEN-SET.md): the independent test set and its results.

*Version 0.9.1. All data shown is fictitious. "The AI at work" frames come from a live recording with the local model; the other screens are rendered by the automated UI tests. PDFs are generated with [QuestPDF](https://www.questpdf.com) under its Community license; Word files with the MIT-licensed Open XML SDK.*
