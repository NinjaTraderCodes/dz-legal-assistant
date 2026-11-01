// The Open Graph image of each page (1200×630): its first screen, captured from the running app in the default
// theme and saved into public/og/, so link previews show the product. Usage:
//   node scripts/og.mjs [--base http://localhost:5080] [--pages home,download,law,privacy,about]
import { mkdirSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const pages = arg("pages", "home,download,law,privacy,about").split(",");
const out = resolve(dirname(fileURLToPath(import.meta.url)), "..", "public", "og");
const pathOf = (page, lang) => (page === "home" ? `/${lang}/` : `/${lang}/${page}/`);
mkdirSync(out, { recursive: true });

const browser = await chromium.launch();
for (const page of pages) {
    for (const lang of ["ar", "en"]) {
        const context = await browser.newContext({ viewport: { width: 1200, height: 630 }, deviceScaleFactor: 1, reducedMotion: "reduce" });
        const tab = await context.newPage();
        await tab.goto(`${base}${pathOf(page, lang)}`, { waitUntil: "networkidle" });
        await tab.evaluate(() => document.fonts.ready);
        // The home's welcome fills the frame; a secondary page shows its opening without the site bar.
        await tab.addStyleTag({ content: ".site-header { display: none !important } .hero { min-height: 630px !important } .head { padding-top: 72px !important }" });
        await tab.waitForTimeout(300);
        const png = await tab.screenshot({ clip: { x: 0, y: 0, width: 1200, height: 630 } });
        await sharp(png).png({ palette: true, colours: 256, compressionLevel: 9 }).toFile(resolve(out, `${page}-${lang}.png`));
        await context.close();
        console.log(`og: ${page}-${lang}.png`);
    }
}
await browser.close();
