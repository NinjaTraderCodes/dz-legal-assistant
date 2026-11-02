// The demo in each state of the report, captured against the stand-in model instance: first screen, a missing
// element, the AI proposal, drafting in progress, the drafted grounds with the review box, Preview, export blocked.
// Usage: node tests/states.mjs --base http://localhost:5082 --out ../docs/website/phase-3c [--langs ar,en] [--themes light,dark] [--sizes 1366x768,1440x900,1920x1080]
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5082");
const out = resolve(arg("out", "tests/reports/states"));
const langs = arg("langs", "ar,en").split(",");
const themes = arg("themes", "light,dark").split(",");
const sizes = arg("sizes", "1366x768,1920x1080").split(",").map((s) => s.split("x").map(Number));
const states = arg("states", "first,missing,proposal,writing,review,preview,blocked").split(",");
mkdirSync(out, { recursive: true });
const browser = await chromium.launch();
let count = 0;
const shot = async (page, name) => { const png = await page.screenshot({ animations: "disabled" }); await sharp(png).png({ palette: true, colours: 192, compressionLevel: 9 }).toFile(resolve(out, name + ".png")); count++; };

for (const lang of langs) for (const theme of themes) for (const [width, height] of sizes) {
    const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: 1, locale: lang === "ar" ? "ar-DZ" : "en-GB", reducedMotion: "reduce" });
    await context.addCookies([{ name: "dzlegal.theme", value: theme, url: base }, { name: "dzlegal.lang", value: lang, url: base }]);
    const page = await context.newPage();
    const tag = `${lang}-${theme}-${width}x${height}`;
    const sampleLabel = lang === "ar" ? "قرض غير مسدد" : "Unpaid loan";
    const t = (en, ar) => (lang === "ar" ? ar : en);
    await page.goto(`${base}/app?lang=${lang}`, { waitUntil: "networkidle" });
    await page.locator(".app").waitFor({ timeout: 60_000 });
    await page.waitForTimeout(600);
    if (states.includes("first")) await shot(page, `demo-first-${tag}`);
    await page.locator("#sample-picker").selectOption({ label: sampleLabel });
    await page.waitForTimeout(600);
    // A missing element: the defendant's domicile cleared.
    await page.locator(".steps .step").nth(2).click();
    await page.waitForTimeout(300);
    const domicile = page.locator("#f-defendant-domicile");
    const address = await domicile.inputValue();
    await domicile.fill("");
    await page.waitForTimeout(700);
    if (states.includes("missing")) await shot(page, `demo-missing-${tag}`);
    await domicile.fill(address);
    await page.waitForTimeout(500);
    // The AI proposal.
    await page.locator(".steps .step").nth(5).click();
    await page.getByRole("button", { name: t("Analyse the facts", "تحليل الوقائع"), exact: true }).click();
    await page.locator(".confirm-claim").waitFor({ timeout: 60_000 });
    await page.waitForTimeout(500);
    if (states.includes("proposal")) await shot(page, `demo-proposal-${tag}`);
    await page.getByRole("button", { name: t("Yes, this is the claim type", "نعم، هذا نوع الدعوى") }).click();
    await page.waitForTimeout(500);
    // Drafting in progress: the stand-in model streams slowly when asked (--slow), so the writing state is visible.
    await page.getByRole("button", { name: t("Draft legal grounds", "صياغة الأسانيد"), exact: true }).click();
    if (states.includes("writing")) {
        await page.locator(".section.writing, .activity").first().waitFor({ timeout: 30_000 }).catch(() => {});
        await page.waitForTimeout(900);
        await shot(page, `demo-writing-${tag}`);
    }
    await page.locator(".review-list .reviewed input").waitFor({ timeout: 90_000 });
    await page.waitForTimeout(600);
    await page.locator(".paper .section.ai").click();
    await page.waitForTimeout(300);
    if (states.includes("review")) await shot(page, `demo-review-${tag}`);
    // Export blocked: the PDF refused before the review tick.
    await page.getByRole("button", { name: t("Export PDF", "تصدير PDF") }).click();
    await page.locator(".export-blockers[role=alert]").waitFor({ timeout: 10_000 });
    await page.waitForTimeout(300);
    if (states.includes("blocked")) await shot(page, `demo-blocked-${tag}`);
    // Preview.
    await page.getByRole("tab", { name: t("Preview", "معاينة") }).click();
    await page.locator(".page-img").first().waitFor({ timeout: 60_000 });
    await page.waitForTimeout(800);
    if (states.includes("preview")) await shot(page, `demo-preview-${tag}`);
    await context.close();
}
await browser.close();
console.log(`states: ${count} files in ${out}`);
