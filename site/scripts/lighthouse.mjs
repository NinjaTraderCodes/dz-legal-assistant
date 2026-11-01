// Lighthouse for a phase report: each page in mobile and desktop form, through the Edge installed on this machine
// (no browser download). Writes the HTML reports and a summary table. Usage:
//   node scripts/lighthouse.mjs --out ../docs/website/phase-1 [--base http://localhost:5080] [--pages /ar/,/en/]
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { spawnSync } from "node:child_process";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const base = arg("base", "http://localhost:5080");
const out = resolve(arg("out", "tests/reports/lighthouse"));
const pages = arg("pages", "/ar/,/en/").split(",");
const chrome = process.env.CHROME_PATH ?? "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
mkdirSync(out, { recursive: true });

const rows = [];
for (const path of pages) {
    for (const form of ["mobile", "desktop"]) {
        const name = (path.replace(/\//g, "-").replace(/^-|-$/g, "") || "root") + "-" + form;
        const target = resolve(out, `lighthouse-${name}`);
        const args = ["lighthouse", base + path, "--output=html", "--output=json", `--output-path=${target}`, "--quiet",
            "--chrome-flags=--headless=new --no-first-run", "--only-categories=performance,accessibility,best-practices,seo"];
        if (form === "desktop") args.push("--preset=desktop");
        const result = spawnSync("npx", args, { stdio: "inherit", shell: true, env: { ...process.env, CHROME_PATH: chrome } });
        if (result.status !== 0) { console.error(`lighthouse failed for ${path} (${form})`); process.exit(1); }
        const fs = await import("node:fs");
        const json = JSON.parse(fs.readFileSync(`${target}.report.json`, "utf8"));
        fs.unlinkSync(`${target}.report.json`); // the HTML report embeds the same data; the JSON would double the weight
        const c = json.categories;
        const audits = json.audits;
        rows.push(`| ${path} | ${form} | ${Math.round(c.performance.score * 100)} | ${Math.round(c.accessibility.score * 100)} | ${Math.round(c["best-practices"].score * 100)} | ${Math.round(c.seo.score * 100)} | ${audits["largest-contentful-paint"].displayValue} | ${audits["cumulative-layout-shift"].displayValue} | ${audits["total-blocking-time"].displayValue} |`);
    }
}
const summary = ["| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |", "|---|---|---|---|---|---|---|---|---|", ...rows].join("\n") + "\n";
writeFileSync(resolve(out, "lighthouse-summary.md"), summary);
console.log(summary);
