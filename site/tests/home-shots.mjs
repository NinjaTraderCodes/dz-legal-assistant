// Pictures of the home page for a phase report: the story frame by frame (the pinned timeline scrolled to each
// step, as a reader would see it) and the sections that carry screenshots, at laptop and desktop sizes, in both
// languages and themes. Usage:
//   node tests/home-shots.mjs --base http://localhost:5080 --out ../docs/website/phase-5 [--langs ar,en] [--themes dark,light] [--sizes 1366x768,1920x1080] [--story] [--sections hero,proofs,ai,team]
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(arg("out", "tests/reports/home"));
const langs = arg("langs", "ar,en").split(",");
const themes = arg("themes", "dark,light").split(",");
const sizes = arg("sizes", "1366x768,1920x1080").split(",").map((s) => s.split("x").map(Number));
const story = process.argv.includes("--story");
const sections = (arg("sections", "") || "").split(",").filter(Boolean);
mkdirSync(out, { recursive: true });

// The story's timeline: one unit per written step, four for the grounds, scrolled over (units + 1) * 190 px from the
// stage's pinned position (src/scripts/story.ts). The frames are taken at the end of each step's own unit.
const units = 8 + 4;
const frames = [
    ...[1, 2, 3, 4, 5, 6, 7, 8].map((k) => ({ name: `step${k}`, at: k + 0.85 })),
    { name: "grounds-rules", at: 8 + 1.8 }, { name: "grounds-ai", at: 8 + 2.9 }, { name: "grounds-review", at: 8 + 3.75 }, { name: "grounds-done", at: 11.98 },
];
const save = async (buffer, name) => { await sharp(buffer).jpeg({ quality: 60, mozjpeg: true }).toFile(resolve(out, name + ".jpg")); console.log(name + ".jpg"); };

const browser = await chromium.launch();
for (const [width, height] of sizes) for (const lang of langs) for (const theme of themes) {
    const context = await browser.newContext({ viewport: { width, height }, locale: lang === "ar" ? "ar-DZ" : "en-GB" });
    await context.addCookies([{ name: "dzlegal.theme", value: theme, url: base }]);
    const page = await context.newPage();
    await page.goto(`${base}/${lang}/`, { waitUntil: "networkidle" });
    await page.evaluate(() => document.fonts.ready);
    if (story) {
        // Scroll to the stage so the story module loads and pins, then step through the timeline: the pin starts under
        // the header and runs (units + 1) * 190 px, mapped onto the timeline's `units`.
        const headerHeight = await page.locator(".site-header").evaluate((el) => el.offsetHeight);
        const top = await page.locator("[data-story] .stage").evaluate((el, h) => el.getBoundingClientRect().top + window.scrollY - h, headerHeight + 16);
        await page.evaluate((y) => window.scrollTo(0, y), top);
        await page.waitForTimeout(1200);
        for (const f of frames) {
            await page.evaluate((y) => window.scrollTo(0, y), top + (f.at / units) * (units + 1) * 190);
            await page.waitForTimeout(900);
            await save(await page.screenshot({ animations: "allow" }), `story-${f.name}-${lang}-${theme}-${width}x${height}`);
        }
    }
    // The sticky header would cross a tall section's picture: it is hidden for these.
    if (sections.length) await page.addStyleTag({ content: ".site-header { visibility: hidden; }" });
    for (const s of sections) {
        const el = page.locator({ hero: "section.hero", proofs: ".guarantees", ai: ".page.ai", team: ".page.team", try: ".page.try" }[s] ?? s);
        await el.scrollIntoViewIfNeeded();
        await page.waitForTimeout(600);
        await save(await el.screenshot({ animations: "disabled" }), `${s}-${lang}-${theme}-${width}x${height}`);
    }
    await context.close();
}
await browser.close();
