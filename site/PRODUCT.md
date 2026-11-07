# Product

<!-- impeccable:product-schema 1 -->

Scope: the public website in `site/` (served at `/`, `/ar/`, `/en/`), which presents the product. The desktop app and the web demo at `/app` are the product itself and are out of scope for website design work: they keep their own look.

## Platform

web

## Users

- **Primary: Algerian lawyers and small law offices.** They draft the statement of claim (عريضة افتتاح الدعوى) by hand, under time pressure, mostly in formal Arabic, and many also work in French. They meet the website on a laptop at the office or on a phone between hearings. Their job on the site: understand in seconds what the assistant does for their daily drafting, trust that client files stay on their computer, then try the demo or download the app.
- **Secondary: evaluators and partners** (competition judges, investors, incubators, bar associations). They skim in a few minutes and need the proof: what works today, how it was measured, who built it. The same pages must convince them without a separate track.

## Product Purpose

DZ Legal Assistant helps an Algerian lawyer draft the statement of claim in formal Arabic, checks that every element the Code of Civil and Administrative Procedure requires is present, and proposes the articles from the official texts. It runs on the lawyer's own computer. Success for the website: a lawyer who lands on it tries the demo or downloads the app, and an evaluator leaves able to say what is proven.

## Positioning

- **Offline by design.** The desktop app reaches its AI model only on the computer's own address; client files never leave the lawyer's machine, as professional secrecy requires. Online legal-AI products cannot truthfully say this.
- **The law is quoted by code, never by the model.** Each article's rule comes from the official text; the AI only writes how the facts meet it, and every AI-written section must be reviewed before export.
- **Form checked by code, not AI.** A rule engine checks every mandatory element and names the article behind each missing one.
- **One document, done deeply:** the Algerian statement of claim, Arabic first.

## Operating Context

- The petition itself is always Arabic and right to left, whatever the interface language.
- The lawyer reviews and signs; the app drafts and checks, it never decides the merits.
- Public demo at `/app`: same screens, hosted model, fictitious cases only. The desktop app is the product lawyers use.

## Capabilities and Constraints

- Website languages: **Arabic (default, right to left) and English**. French is a likely later addition: layouts and type choices must leave room for a third language.
- The site is static (Astro), served by the same app as the demo; **nothing may load from another origin at run time**: no web fonts from a CDN, no analytics, no trackers, no cookie banner. Every bundled font, icon and image carries its licence.
- **No links to the source repository** from the site or the demo (enforced by `site/scripts/check-dist.mjs`); the Windows download links to the release zip only.
- Every figure on the site carries its source in the repository.
- Budgets in `site/budget.json` (page weight, scripts, fonts, mobile LCP ≤ 2.6 s); accessibility at WCAG AA, axe at 0 violations.
- Undecided: pricing and subscriptions (not on the site today, must not be invented).

## Brand Commitments

- Names: **DZ Legal Assistant** / **مساعد صياغة العرائض**.
- The website may build its **own visual world**; it is not bound to the app's brown-and-gold palette. The app, the demo and every app screenshot stay exactly as they are; the site may echo the app's colours where it helps recognition.
- Voice: plain, exact, honest about limits ("The AI proposes, the lawyer decides"); no hype, no invented claims.

## Evidence on Hand

- Real product imagery: the hero laptop with a real capture (`site/src/assets/hero/laptop.png`), four real 1920×1080 and 3840×2160 captures of the desktop app with the local model's drafted grounds, demo captures from real Nemotron 3 Ultra runs (`site/src/content/grounds-run-*.json`, `desktop-run.json`), the live recording frames (`docs/images/readme/`).
- Measured results: the hidden test set (`docs/HIDDEN-SET.md`): 94% claim type, 67% fully right online, 36% fully right locally, 10/10 out-of-scope declined; Form Engine 360/360; 1,361 articles from 6 texts; 21 claim types.
- Team: Hicham Benkaihoul (lead), Djihane Bendjeddou / جهان بن جدو (UI/UX), Khouloud Otmane Cherif (legal review), with photos or avatar in `site/src/assets/team/`.
- **Absent, never to be fabricated:** testimonials, client logos, user counts, prices, press coverage, a lawyer's verification of the legal rules (not done yet: every rule is marked unverified).

## Product Principles

1. **Show the real product.** Real screens and real AI output beat illustrations; anything drawn says so.
2. **Proof over promise.** Every claim is measured or sourced, and the limits are said plainly.
3. **The lawyer stays in charge.** The site never suggests the software decides or guarantees a result.
4. **Arabic is first-class.** Arabic pages are designed, not translated afterwards; the petition is always Arabic.
5. **Privacy is part of the product.** The site itself practises it: no trackers, nothing loaded from elsewhere.

## Accessibility & Inclusion

WCAG 2.1 AA in both languages and both themes; right-to-left layouts correct by construction; reduced motion respected (the scroll story shows its final state); readable on a mid-range phone.
