// Writes src/content/paper-ar.json: the petition exactly as the app lays it out for the fictitious "Unpaid loan"
// sample (src/DzLegal.Web/Samples/debt.json), read from the web demo's paper, which renders the desktop's section view
// models from the form definition: each section's alignment, weight, space before, heading and text. The
// home's scroll story sets its sheet from this file, so it never paraphrases the template. No model is called: opening
// a sample fills the form and the template only (the legal grounds stay empty; the story takes them from a real run,
// src/content/grounds-run-ar.json). Usage, with the app running (dotnet run --launch-profile http+site):
//   node tests/paper-dump.mjs [--base http://localhost:5080]   (CHROMIUM_PATH selects a browser binary if needed)
import { writeFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(dirname(fileURLToPath(import.meta.url)), "..", "src", "content", "paper-ar.json");
const sample = "debt";

const browser = await chromium.launch(process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH, args: ["--no-proxy-server"] } : {});
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });
await page.goto(`${base}/app?lang=ar`, { waitUntil: "networkidle" });
await page.locator(".app").waitFor({ timeout: 60_000 });
await page.selectOption("#sample-picker", sample);
await page.locator(".paper .section").first().waitFor();
await page.waitForTimeout(1500);
const sections = await page.evaluate(() => [...document.querySelectorAll(".paper .section")].map((s) => ({
    align: [...s.classList].find((c) => c.startsWith("align-"))?.slice(6) ?? "start",
    bold: s.classList.contains("bold"),
    // The demo prints the definition's space before (points) times 1.12; the file keeps the points.
    spaceBefore: Math.round((parseFloat(s.style.marginTop) || 0) / 1.12 * 100) / 100,
    heading: s.querySelector(".document-heading")?.textContent?.replace(/:\s*$/, "") ?? null,
    text: s.querySelector("textarea")?.value ?? s.querySelector(".section-text")?.textContent ?? "",
})));
const watermark = await page.evaluate(() => document.querySelector(".paper")?.classList.contains("watermark") ?? false);
await browser.close();
// The legal grounds are the section the model drafts: still empty here, under the app's own heading.
if (sections.length < 10 || !sections.some((s) => s.heading === "المناقشة القانونية" && s.text === "")) throw new Error(`unexpected paper: ${sections.length} sections`);
writeFileSync(out, JSON.stringify({ sample, source: "the web demo's paper for this sample (tests/paper-dump.mjs)", watermark, sections }, null, 2) + "\n");
console.log(`paper: ${sections.length} sections written to ${out}`);
