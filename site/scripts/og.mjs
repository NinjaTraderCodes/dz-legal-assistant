// The Open Graph image of each home page (1200×630): the welcome screen itself, captured from the running app and
// saved into public/og/, so link previews show the product. Usage: node scripts/og.mjs [--base http://localhost:5080]
import { mkdirSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";
import sharp from "sharp";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(dirname(fileURLToPath(import.meta.url)), "..", "public", "og");
mkdirSync(out, { recursive: true });

const browser = await chromium.launch();
for (const lang of ["ar", "en"]) {
    const context = await browser.newContext({ viewport: { width: 1200, height: 630 }, deviceScaleFactor: 1, reducedMotion: "reduce" });
    const page = await context.newPage();
    await page.goto(`${base}/${lang}/`, { waitUntil: "networkidle" });
    await page.evaluate(() => document.fonts.ready);
    await page.addStyleTag({ content: ".site-header { display: none !important } .hero { min-height: 630px !important }" });
    await page.waitForTimeout(300);
    const png = await page.screenshot({ clip: { x: 0, y: 0, width: 1200, height: 630 } });
    await sharp(png).png({ palette: true, colours: 256, compressionLevel: 9 }).toFile(resolve(out, `home-${lang}.png`));
    await context.close();
    console.log(`og: home-${lang}.png`);
}
await browser.close();
