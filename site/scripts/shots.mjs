// Screenshots for a phase report: every page at 390, 768, 1366 and 1920 px, Arabic and English, light and dark,
// named <view>-<ar|en>-<light|dark>-<WxH>.png as docs/images does. Full page for the site, the first screen for the
// demo. Quantised PNGs keep the repository light. Usage:
//   node scripts/shots.mjs --out ../docs/website/phase-1 [--base http://localhost:5080] [--views home,app]
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(arg("out", "tests/reports/shots"));
const views = arg("views", "home,app").split(",");
const sizes = [[390, 844], [768, 1024], [1366, 768], [1920, 1080]];
const langs = ["ar", "en"];
const themes = ["light", "dark"];
const paths = { home: (lang) => `/${lang}/`, "not-found": (lang) => `/${lang}/404/`, app: (lang) => `/app?lang=${lang}`, root: () => "/" };
// "/" is captured on a phone and a desktop only: it serves the same page as /ar/ or /en/, chosen by the cookie.
const sizesOf = (view) => (view === "root" ? sizes.filter(([w]) => w === 390 || w === 1920) : sizes);
mkdirSync(out, { recursive: true });

const browser = await chromium.launch();
let count = 0;
for (const view of views) {
    for (const lang of langs) for (const theme of themes) for (const [width, height] of sizesOf(view)) {
        // Reduced motion: the scroll story then shows its final state, which is what the report should show.
        const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: 1, locale: lang === "ar" ? "ar-DZ" : "en-GB", reducedMotion: "reduce" });
        await context.addCookies([{ name: "dzlegal.theme", value: theme, url: base }, { name: "dzlegal.lang", value: lang, url: base }]);
        const page = await context.newPage();
        await page.goto(base + paths[view](lang), { waitUntil: "networkidle" });
        if (view === "app") {
            await page.locator(".app").waitFor({ timeout: 60_000 });
            await page.waitForTimeout(800);
        } else {
            await page.evaluate(() => document.fonts.ready);
            // Phones lay sections out only when they come near the screen (content-visibility); a full-page capture
            // enlarges the viewport without waking them, so the capture switches that off: the report shows the page
            // as a reader scrolling through it sees it.
            await page.addStyleTag({ content: ".section { content-visibility: visible !important; contain-intrinsic-size: none !important; }" });
            await page.waitForTimeout(300);
        }
        // Full pages of the site go to JPEG (a long photo-led page is 4× smaller); the demo's screens stay crisp PNGs.
        const fullPage = view !== "app";
        const file = resolve(out, `${view}-${lang}-${theme}-${width}x${height}.${fullPage ? "jpg" : "png"}`);
        const shot = await page.screenshot({ fullPage, animations: "disabled" });
        if (fullPage) await sharp(shot).jpeg({ quality: 70, mozjpeg: true }).toFile(file);
        else await sharp(shot).png({ palette: true, colours: 192, compressionLevel: 9 }).toFile(file);
        count++;
        await context.close();
    }
}
await browser.close();
console.log(`shots: ${count} files in ${out}`);
