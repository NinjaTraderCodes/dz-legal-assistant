// The private-window check: a fresh browser context with no cookies or storage opens every page of the site as a
// first-time visitor would, follows every internal link once (pages, files, the demo, the language switch) and
// reports each outbound link's status with a plain request. Exit code 1 on any broken internal link or page error.
// Usage: node scripts/links.mjs [--base http://localhost:5080] [--out ../docs/website/phase-4/links.md]
import { writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { chromium } from "@playwright/test";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = arg("out", "");
const starts = ["/", "/ar/", "/en/"];
const seen = new Map(); // path -> status
const external = new Map(); // url -> { status, from }
const errors = [];
const queue = [...starts];

const browser = await chromium.launch();
const context = await browser.newContext({ locale: "en-GB" }); // no cookies, no storage: a private window
const page = await context.newPage();
page.on("pageerror", (e) => errors.push(`page error: ${e.message}`));
page.on("console", (m) => { if (m.type() === "error") errors.push(`console error on ${page.url()}: ${m.text()}`); });

while (queue.length) {
    const path = queue.shift();
    if (seen.has(path)) continue;
    if (/\.(pdf|txt|svg|xml|png|jpg|woff2|css|js)$/i.test(path)) {
        // A file, not a page: fetched as a download would be (a PDF makes the browser download, not navigate).
        const r = await context.request.get(base + path, { maxRedirects: 0 }).catch(() => null);
        const fileStatus = r?.status() ?? 0;
        seen.set(path, fileStatus);
        if (fileStatus !== 200) errors.push(`${path}: HTTP ${fileStatus}`);
        continue;
    }
    const response = await page.goto(base + path, { waitUntil: "domcontentloaded" }).catch(() => null);
    const status = response?.status() ?? 0;
    seen.set(path, status);
    if (status !== 200) { errors.push(`${path}: HTTP ${status}`); continue; }
    if (path.startsWith("/app")) {
        // The demo: the shell must show, then the circuit must connect; its own links are not crawled.
        await page.locator(".app").waitFor({ timeout: 60_000 }).catch(() => errors.push(`${path}: the demo did not connect`));
        continue;
    }
    const hrefs = await page.locator("a[href]").evaluateAll((as) => as.map((a) => a.getAttribute("href")));
    for (const href of hrefs) {
        if (!href || href.startsWith("#") || href.startsWith("mailto:")) continue;
        if (/^https?:\/\//.test(href)) { if (!external.has(href)) external.set(href, { status: null, from: path }); continue; }
        const clean = href.split("#")[0];
        if (clean.startsWith("/lang/")) {
            // The switch sets the cookie and comes back: checked once per language with a plain request, not followed.
            const r = await context.request.get(base + clean, { maxRedirects: 0 });
            if (![302, 307].includes(r.status())) errors.push(`${clean}: HTTP ${r.status()} (expected a redirect)`);
            seen.set(clean, r.status());
            continue;
        }
        if (!seen.has(clean) && !queue.includes(clean)) queue.push(clean);
    }
}
// Outbound links: a plain GET, no cookies. GitHub answers 429 to a burst from one address, so its requests are spaced
// and a 429 is retried once after a pause; a slow official site is reported, not failed.
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
for (const [url, info] of external) {
    for (let attempt = 0; attempt < 2; attempt++) {
        try {
            const r = await context.request.get(url, { timeout: 20_000, maxRedirects: 5 });
            info.status = r.status();
        } catch (e) {
            info.status = `error: ${String(e.message).split("\n")[0].slice(0, 80)}`;
        }
        if (info.status !== 429) break;
        await sleep(15_000);
    }
    await sleep(url.includes("github.com") ? 1500 : 200);
}
await browser.close();

const lines = ["| Internal path | Status |", "|---|---:|", ...[...seen].map(([p, s]) => `| \`${p}\` | ${s} |`), "", "| Outbound link | First seen on | Status |", "|---|---|---:|", ...[...external].map(([u, i]) => `| ${u} | \`${i.from}\` | ${i.status} |`)];
if (errors.length) lines.push("", "Problems:", ...errors.map((e) => "- " + e));
const report = lines.join("\n") + "\n";
console.log(report);
if (out) writeFileSync(resolve(out), report);
// A 429 from GitHub is its rate limit answering a burst of checks, not a broken link: listed, not counted.
const broken = errors.length + [...external.values()].filter((i) => typeof i.status === "number" && i.status >= 400 && i.status !== 429).length;
console.log(`links: ${seen.size} internal, ${external.size} outbound, ${broken} problem(s)`);
process.exit(broken ? 1 : 0);
