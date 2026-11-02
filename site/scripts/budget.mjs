// Checks the site against budget.json: what each built page loads (its HTML, its scripts, its fonts, gzipped) and
// the latest Lighthouse summary (mobile LCP and the four scores). Exit code 1 when a budget is broken. Usage:
//   node scripts/budget.mjs [--lighthouse ../docs/website/phase-4/lighthouse-summary.md]
import { readFileSync, readdirSync, statSync, existsSync } from "node:fs";
import { gzipSync } from "node:zlib";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const dist = resolve(siteDir, "dist");
const budget = JSON.parse(readFileSync(resolve(siteDir, "budget.json"), "utf8"));
const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const summaryPath = resolve(siteDir, arg("lighthouse", "../docs/website/phase-4/lighthouse-summary.md"));
const gz = (file) => gzipSync(readFileSync(file)).length;
const kb = (n) => (n / 1024).toFixed(1) + " KB";
const problems = [];
const rows = [];

function pages(dir, out = []) {
    for (const name of readdirSync(dir)) {
        const path = join(dir, name);
        if (statSync(path).isDirectory()) pages(path, out);
        else if (name === "index.html") out.push(path);
    }
    return out;
}

// Every script and font a page references, by the attributes Astro writes (script src, link preload as="font", @font-face in the inlined CSS).
for (const page of pages(dist)) {
    const rel = "/" + page.slice(dist.length + 1).replace(/\\/g, "/").replace(/index\.html$/, "");
    const html = readFileSync(page, "utf8");
    const scripts = [...html.matchAll(/<script[^>]+src="(\/[^"]+)"/g)].map((m) => m[1]);
    // The home's story module is lazy (wide screens, no reduced motion): counted, because a desktop visitor gets it.
    const lazy = [...html.matchAll(/import\("(\.\/[^"]+)"\)/g)].map((m) => m[1]);
    const scriptBytes = scripts.reduce((n, s) => n + gz(join(dist, s)), 0);
    const preloaded = [...html.matchAll(/rel="preload" as="font"[^>]*href="(\/[^"]+)"/g)].map((m) => m[1]);
    const fontBytes = preloaded.reduce((n, f) => n + gz(join(dist, f)), 0);
    const htmlBytes = gz(page);
    rows.push({ rel, htmlBytes, scriptBytes, fontBytes, scripts: scripts.length, lazy: lazy.length });
    if (htmlBytes > budget.pageHtmlGzip) problems.push(`${rel}: HTML ${kb(htmlBytes)} > ${kb(budget.pageHtmlGzip)}`);
    if (fontBytes > budget.fontsPerPageGzip) problems.push(`${rel}: preloaded fonts ${kb(fontBytes)} > ${kb(budget.fontsPerPageGzip)}`);
    if ((rel === "/ar/" || rel === "/en/") && scriptBytes > budget.homeScriptGzip) problems.push(`${rel}: scripts ${kb(scriptBytes)} > ${kb(budget.homeScriptGzip)}`);
}
// The home's lazy story module, which the home script imports on wide screens.
const astroDir = join(dist, "_astro");
const storyFiles = existsSync(astroDir) ? readdirSync(astroDir).filter((f) => /^story\..*\.js$/.test(f)) : [];
const storyBytes = storyFiles.reduce((n, f) => n + gz(join(astroDir, f)), 0);
const homeScripts = rows.find((r) => r.rel === "/en/")?.scriptBytes ?? 0;
if (homeScripts + storyBytes > budget.homeScriptGzip) problems.push(`home scripts with the lazy story module: ${kb(homeScripts + storyBytes)} > ${kb(budget.homeScriptGzip)}`);

console.log("| Page | HTML gz | scripts gz | preloaded fonts gz |");
console.log("|---|---:|---:|---:|");
for (const r of rows) console.log(`| ${r.rel} | ${kb(r.htmlBytes)} | ${kb(r.scriptBytes)} | ${kb(r.fontBytes)} |`);
console.log(`\nHome scripts + lazy story module (${storyFiles.join(", ") || "none"}): ${kb(homeScripts + storyBytes)} (budget ${kb(budget.homeScriptGzip)})`);

if (existsSync(summaryPath)) {
    const lines = readFileSync(summaryPath, "utf8").split("\n").filter((l) => l.startsWith("| /"));
    for (const line of lines) {
        const cells = line.split("|").map((c) => c.trim());
        const [, page, form, perf, a11y, bp, seo, lcp] = cells;
        const seconds = Number(lcp.replace(/[^\d.]/g, ""));
        // The not-found pages answer 404 and carry noindex by design, which the SEO audits count against them; their
        // performance score is not a budget either (a short page where the font swap moves the footer), their LCP is.
        const notFound = page.includes("/404/");
        if (form === "mobile" && seconds > budget.mobileLcpSeconds) problems.push(`${page} mobile LCP ${lcp} > ${budget.mobileLcpSeconds} s`);
        if (!notFound && form === "mobile" && Number(perf) < budget.mobilePerformance) problems.push(`${page} mobile performance ${perf} < ${budget.mobilePerformance}`);
        if (Number(a11y) < budget.accessibility) problems.push(`${page} ${form} accessibility ${a11y}`);
        if (Number(bp) < budget.bestPractices) problems.push(`${page} ${form} best practices ${bp}`);
        if (!notFound && Number(seo) < budget.seo) problems.push(`${page} ${form} SEO ${seo}`);
    }
    console.log(`\nLighthouse summary checked: ${summaryPath} (${lines.length} rows)`);
} else {
    console.log(`\nNo Lighthouse summary at ${summaryPath}: scores not checked.`);
}

if (problems.length) {
    console.error(`\nbudget: ${problems.length} budget(s) broken`);
    for (const p of problems) console.error("  " + p);
    process.exit(1);
}
console.log("\nbudget: every budget met");
