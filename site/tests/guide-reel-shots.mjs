// Captures the home's guide reel (folder 05): the demo's own guide at one moment of each of its five steps, the gold
// ring on what it points at and its card beside it, in a fixed 16:10 frame around the two, dark and then light (the
// demo's theme button: no second model call), with the hint the card shows at that moment, verbatim, and the engine
// that answered, in src/content/guide-reel.json. Steps 3 and 5 show the model's real output (step 4 is the moment
// before drafting), so run it against an instance with the owner's keys, never against the stand-in model (its text is
// not AI output); --mock allows the stand-in for a trial run into another folder. A full run per language makes three
// model calls (reading, choosing the articles, drafting); --until 4 stops before the drafting (two calls), --until 2
// before any call, and --also <lang> takes each moment a second time in that language through the demo's language
// button (no call). Each step's notes carry the date it was taken.
//   node tests/guide-reel-shots.mjs --base http://localhost:5080 --langs ar,en [--steps 1,2,3,4,5] [--until 5]
//     [--also en] [--out src/assets/shots/reel] [--mock]          (CHROMIUM_PATH selects a browser binary if needed)
import { mkdirSync, readFileSync, writeFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(arg("out", "src/assets/shots/reel"));
const langs = arg("langs", "ar,en").split(",");
const keep = arg("steps", "1,2,3,4,5").split(",").map(Number);
const until = Number(arg("until", "5"));
const also = arg("also", "");
const mock = process.argv.includes("--mock");
const notes = resolve(arg("notes", mock ? `${out}/guide-reel.json` : "src/content/guide-reel.json"));
mkdirSync(out, { recursive: true });

// The frame: 16:10 in page pixels, drawn at scale 2. It grows (same ratio) when the ring and the card need more room.
const frame = { w: 800, h: 500 };
const viewport = { width: 1366, height: 768 };

const browser = await chromium.launch(process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH, args: ["--no-proxy-server"] } : {});
const reel = existsSync(notes) ? JSON.parse(readFileSync(notes, "utf8")) : {};
const date = new Date().toISOString().slice(0, 10);

/** Keeps a step's notes, replacing that step's earlier ones. */
function note(lang, entry, answeredBy) {
    const r = (reel[lang] ??= { steps: [] });
    r.steps = [...r.steps.filter((s) => s.step !== entry.step), { ...entry, date }].sort((a, b) => a.step - b.step);
    if (answeredBy) r.answeredBy = answeredBy;
    delete r.date;
    delete r.engine;
}

for (const lang of langs) {
    // Reduced motion: the ring and the card stand still (no pulse, no slide), as a picture should.
    const context = await browser.newContext({ viewport, deviceScaleFactor: 2, reducedMotion: "reduce", locale: lang === "ar" ? "ar-DZ" : "en-GB" });
    await context.addCookies([{ name: "dzlegal.theme", value: "dark", url: base }]);
    const page = await context.newPage();
    await page.goto(`${base}/app?lang=${lang}&guide=1`);
    await page.locator(".guide-card").waitFor({ timeout: 90_000 });
    const engine = await page.locator("#engine-picker option:checked").innerText();
    if (!mock && /mock/i.test(engine)) throw new Error("guide-reel-shots: this instance answers with the stand-in model; use one with real keys, or --mock for a trial");

    /** The frame around the ring and the card (and what else the moment needs), clamped to the window. */
    async function region(more = []) {
        const boxes = await page.evaluate((extra) => [".guide-spot:not(.empty)", ".guide-card", ...extra]
            .map((s) => document.querySelector(s)?.getBoundingClientRect()).filter((r) => r && r.width > 0)
            .map((r) => ({ left: r.left, top: r.top, right: r.right, bottom: r.bottom })), more);
        const pad = 28;
        const u = { left: Math.min(...boxes.map((b) => b.left)) - pad, top: Math.min(...boxes.map((b) => b.top)) - pad, right: Math.max(...boxes.map((b) => b.right)) + pad, bottom: Math.max(...boxes.map((b) => b.bottom)) + pad };
        const scale = Math.max(1, (u.right - u.left) / frame.w, (u.bottom - u.top) / frame.h);
        const w = Math.min(viewport.width, frame.w * scale), h = Math.min(viewport.height, frame.h * scale);
        const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);
        const x = clamp((u.left + u.right - w) / 2, 0, viewport.width - w), y = clamp((u.top + u.bottom - h) / 2, 0, viewport.height - h);
        return { x: Math.round(x), y: Math.round(y), width: Math.round(w), height: Math.round(h) };
    }

    /** One language's pictures of the moment, dark then light through the demo's own theme button. */
    async function take(n, as, more) {
        await page.waitForTimeout(600); // the card has been placed beside its target
        const hint = (await page.locator(".guide-hint").innerText()).trim();
        const title = (await page.locator(".guide-step > span").first().innerText()).trim();
        const answered = (await page.locator(".answered-by").first().innerText().catch(() => "")).replace(/^.*?:\s*/, "").trim();
        await page.screenshot({ path: `${out}/step${n}-${as}-dark.png`, clip: await region(more) });
        const theme = page.locator("header .tools.end button").first();
        await theme.click(); await page.waitForTimeout(500);
        await page.screenshot({ path: `${out}/step${n}-${as}-light.png`, clip: await region(more) });
        await theme.click(); await page.waitForTimeout(400);
        note(as, { step: n, title, hint }, answered);
        console.log(`guide-reel-shots: ${as} step ${n}: ${hint.slice(0, 70)}…`);
    }

    async function shoot(n, more = []) {
        if (!keep.includes(n)) return;
        await take(n, lang, more);
        if (!also) return;
        // The same moment in the other language: the language button keeps the case and the guide where they are.
        const language = page.locator("header .tools.end button.lang");
        await language.click(); await page.locator(`.app[lang="${also}"]`).waitFor();
        await take(n, also, more);
        await language.click(); await page.locator(`.app[lang="${lang}"]`).waitFor();
    }

    // 1. The sample picker lit, on arrival.
    await page.locator("#sample-picker.guide-ring").waitFor();
    await shoot(1);
    if (until < 2) { await context.close(); continue; }
    // 2. The domicile cleared: the score and its red cards lit, the card beside them.
    await page.locator("#sample-picker").selectOption("debt");
    await page.locator('.steps .step[data-step="defendant"].guide-ring').click();
    const domicile = page.locator("#f-defendant-domicile");
    const address = await domicile.inputValue();
    await domicile.fill("");
    await page.locator(".score-card.guide-ring").waitFor();
    await shoot(2);
    await domicile.fill(address);
    if (until < 3) { await context.close(); continue; }
    // 3. The model's reading, and the confirmation it waits for.
    await page.locator('.steps .step[data-step="grounds"].guide-ring').click();
    await page.locator(".grounds button.ai.guide-ring").click();
    await page.locator(".confirm-claim button.primary.guide-ring").waitFor({ timeout: 180_000 });
    await shoot(3, [".grounds .reading"]);
    await page.locator(".confirm-claim button.primary").click();
    // 4. The claim type confirmed: the guide points at the drafting button.
    await page.locator(".engine-row button.ai.guide-ring").waitFor();
    await shoot(4);
    if (until >= 5) {
        // 5. The export refused before the review: the review box lit.
        await page.locator(".engine-row button.ai").click();
        await page.locator(".review-list .reviewed input").waitFor({ timeout: 300_000 });
        await page.locator('button.export[data-export="pdf"]').click();
        await page.locator(".review-list .reviewed.guide-ring").waitFor();
        await shoot(5, [".export-blockers"]);
    }
    await context.close();
}

writeFileSync(notes, JSON.stringify(reel, null, 2) + "\n");
await browser.close();
