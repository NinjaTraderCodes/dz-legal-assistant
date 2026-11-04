// Captures the pictures of the "Test it in 5 minutes" guide from the web demo, one browser session per language:
// the sample loaded, the red card for the missing domicile, the AI's reading with the claim-type question, the
// drafted grounds with their AI mark and review box, the refused export and the export allowed after the review.
// The AI steps are real calls to the demo's hosted model, so run it against an instance with the owner's keys and
// never against the stand-in model (its text is not AI output). Template mode (an instance without keys) is enough
// for the steps that need no model, which --mode template selects. Dark is captured first (the demo's default),
// then the demo's own theme button gives the same states in light: no second model call. Usage:
//   node tests/guide-shots.mjs --base http://localhost:5080 --out src/assets/shots/guide [--langs ar,en] [--steps 1,2,3,4,5] [--mode real|template]
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(arg("out", "src/assets/shots/guide"));
const langs = arg("langs", "ar,en").split(",");
const steps = arg("steps", "1,2,3,4,5").split(",").map(Number);
const mode = arg("mode", "real");
// The viewport height: 768 for the first screen; taller for the drafted grounds, so more of the sheet shows in the panel.
const height = Number(arg("height", "768"));
// With --dump <file>, the drafted grounds of the run are written as JSON, verbatim from the page: the model line,
// the date, the language of the run and the section's text, so the home's sheet can show real model output.
const dump = arg("dump", "");
mkdirSync(out, { recursive: true });

const labels = {
    ar: { sample: "قرض غير مسدد", analyse: "تحليل الوقائع", confirm: "نعم، هذا نوع الدعوى", draft: "صياغة الأسانيد", pdf: "تصدير PDF" },
    en: { sample: "Unpaid loan", analyse: "Analyse the facts", confirm: "Yes, this is the claim type", draft: "Draft legal grounds", pdf: "Export PDF" },
};

const browser = await chromium.launch();
for (const lang of langs) {
    const L = labels[lang];
    const context = await browser.newContext({ viewport: { width: 1366, height }, deviceScaleFactor: 2, locale: lang === "ar" ? "ar-DZ" : "en-GB", reducedMotion: "reduce", acceptDownloads: true });
    await context.addCookies([{ name: "dzlegal.theme", value: "dark", url: base }, { name: "dzlegal.lang", value: lang, url: base }]);
    const page = await context.newPage();
    // The union of some elements' boxes, kept inside the box of the panel that scrolls them (the panels have a fixed
    // height on wide screens, so an element can be partly out of view), with a small margin.
    const union = async (within, ...selectors) => {
        const boxes = [];
        for (const s of selectors) { const b = await page.locator(s).first().boundingBox(); if (b) boxes.push(b); }
        let left = Math.min(...boxes.map((b) => b.x)) - 8, top = Math.min(...boxes.map((b) => b.y)) - 8;
        let right = Math.max(...boxes.map((b) => b.x + b.width)) + 8, bottom = Math.max(...boxes.map((b) => b.y + b.height)) + 8;
        const panel = within ? await page.locator(within).first().boundingBox() : null;
        if (panel) { left = Math.max(left, panel.x); top = Math.max(top, panel.y); right = Math.min(right, panel.x + panel.width); bottom = Math.min(bottom, panel.y + panel.height); }
        return { x: left, y: top, width: right - left, height: bottom - top };
    };
    const shot = async (name, clip) => {
        const buffer = await page.screenshot({ clip, animations: "disabled" });
        const file = resolve(out, `${name}-${lang}-${await theme()}.png`);
        await sharp(buffer).png({ compressionLevel: 9 }).toFile(file);
        console.log(`${name}-${lang}-${await theme()}.png ${Math.round(clip.width)}x${Math.round(clip.height)} @2x`);
    };
    const theme = async () => (await page.evaluate(() => document.documentElement.getAttribute("data-theme") === "dark")) ? "dark" : "light";
    const toggleTheme = async () => { await page.locator(".tools.end button[aria-pressed]").click(); await page.waitForTimeout(500); };
    // The same state in both themes: dark as loaded, then light through the demo's own button, then back.
    const both = async (name, clipOf) => {
        await shot(name, await clipOf());
        await toggleTheme();
        await shot(name, await clipOf());
        await toggleTheme();
    };

    await page.goto(`${base}/app?lang=${lang}`, { waitUntil: "domcontentloaded" });
    await page.locator(".app").waitFor({ timeout: 90_000 });
    await page.locator("#sample-picker").selectOption({ label: L.sample });
    await page.waitForTimeout(800);

    if (steps.includes(1)) {
        // The first screen with the sample open: the picker in the top bar, the nine steps, the petition, the check at 8 / 9.
        await both("step1-sample", async () => { const panel = await page.locator(".draft-panel").boundingBox(); return { x: 0, y: 0, width: 1366, height: Math.min(height, panel.y + 440) }; });
    }
    if (steps.includes(2)) {
        await page.locator(".steps .step").nth(2).click();
        const domicile = page.locator("#f-defendant-domicile");
        const saved = await domicile.inputValue();
        await domicile.fill("");
        await page.locator(".finding.mandatory").first().waitFor();
        await page.locator(".finding.mandatory").first().scrollIntoViewIfNeeded();
        await page.waitForTimeout(400);
        await both("step2-missing", () => union(".check-panel", ".check-panel .score-card", ".finding.mandatory"));
        await domicile.fill(saved);
        await page.waitForTimeout(400);
    }
    if (steps.includes(3) || steps.includes(4) || steps.includes(5)) {
        await page.locator(".steps .step").nth(5).click();
        if (mode === "real" && (steps.includes(3) || steps.includes(4))) {
            await page.getByRole("button", { name: L.analyse, exact: true }).click();
            await page.locator(".confirm-claim").waitFor({ timeout: 180_000 });
            await page.waitForTimeout(600);
            if (steps.includes(3)) {
                // The reading and the question under it together: the question's bottom at the panel's bottom.
                await page.locator(".confirm-claim").evaluate((el) => el.scrollIntoView({ block: "end" }));
                await page.waitForTimeout(400);
                await both("step3-reading", () => union(".form-panel", ".grounds .reading", ".confirm-claim"));
            }
            await page.getByRole("button", { name: L.confirm, exact: true }).click();
            await page.locator(".article-card.selected").first().waitFor();
        }
        await page.getByRole("button", { name: L.draft, exact: true }).click();
        await page.locator(".review-list .reviewed input").waitFor({ timeout: 240_000 });
        await page.waitForTimeout(800);
        if (dump) {
            const text = await page.locator(".paper .section.ai textarea").first().inputValue();
            const answeredBy = (await page.locator(".answered-by").first().textContent().catch(() => "")) ?? "";
            const engine = (await page.locator("#engine-picker option:checked").first().textContent().catch(() => "")) ?? "";
            const record = { lang, date: new Date().toISOString().slice(0, 10), engine: engine.trim(), answeredBy: answeredBy.trim(), sample: L.sample, paragraphs: text.split(/\r?\n+/).map((p) => p.trim()).filter(Boolean) };
            const { writeFileSync } = await import("node:fs");
            writeFileSync(dump.replace(/\.json$/, "") + `-${lang}.json`, JSON.stringify(record, null, 2) + "\n");
            console.log(`dumped ${record.paragraphs.length} paragraph(s) of the ${lang} run: ${record.answeredBy}`);
        }
        if (steps.includes(4) && mode === "real") {
            await page.locator(".paper .section.ai").evaluate((el) => el.scrollIntoView({ block: "start" }));
            await page.locator(".paper .section.ai").click();
            await page.waitForTimeout(300);
            // The section's top, with its AI mark, inside the visible part of the sheet.
            await both("step4-grounds", async () => { const c = await union(".paper-scroll", ".paper .section.ai"); return { ...c, x: c.x - 16, width: c.width + 32, height: Math.min(c.height, 420) }; });
            await both("step4-review", () => union(null, ".review-list"));
        }
        if (steps.includes(5)) {
            await page.getByRole("button", { name: L.pdf, exact: true }).click();
            await page.locator(".export-blockers[role=alert]").waitFor();
            await page.waitForTimeout(400);
            await both("step5-refused", () => union(null, ".toolbar", ".export-blockers"));
            await page.locator(".review-list .reviewed input").check();
            await page.waitForTimeout(500);
            await both("step5-allowed", () => union(null, ".toolbar", ".review-list"));
        }
    }
    await context.close();
}
await browser.close();
