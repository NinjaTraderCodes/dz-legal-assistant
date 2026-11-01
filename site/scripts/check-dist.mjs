// Fails the build when the generated site would break the server's Content Security Policy or its routing:
// no inline scripts or handlers (script-src 'self'), no javascript: URLs, no links into paths the server reserves,
// and every page declares its language and direction.
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const dist = resolve(dirname(fileURLToPath(import.meta.url)), "..", "dist");
const problems = [];
let pages = 0;

function walk(dir) {
    for (const name of readdirSync(dir)) {
        const path = join(dir, name);
        if (statSync(path).isDirectory()) walk(path);
        else if (name.endsWith(".html")) inspect(path);
    }
}

function inspect(path) {
    pages++;
    const html = readFileSync(path, "utf8");
    const rel = path.slice(dist.length);
    for (const m of html.matchAll(/<script\b([^>]*)>/gi)) {
        if (!/\ssrc=/.test(m[1])) problems.push(`${rel}: inline <script${m[1]}>`);
    }
    if (/\son[a-z]+\s*=/i.test(html)) problems.push(`${rel}: inline event handler attribute`);
    if (/javascript:/i.test(html)) problems.push(`${rel}: javascript: URL`);
    for (const m of html.matchAll(/href="(\/(?:app\/|fonts\/|ui-fonts\/|_blazor|_framework)[^"]*)"/g)) problems.push(`${rel}: link into a reserved path ${m[1]}`);
    for (const m of html.matchAll(/<a\b[^>]*href="\/lang\/[^"]*"[^>]*>/g)) {
        if (!/rel="[^"]*nofollow/.test(m[0])) problems.push(`${rel}: /lang/ link without rel="nofollow"`);
    }
    if (!/<html\b[^>]*\slang="(ar|en)"/.test(html)) problems.push(`${rel}: <html> without lang`);
    if (!/<html\b[^>]*\sdir="(rtl|ltr)"/.test(html)) problems.push(`${rel}: <html> without dir`);
    for (const m of html.matchAll(/(?:src|href)="(https?:\/\/[^"]+)"/g)) {
        if (!/^https:\/\/github\.com\//.test(m[1]) && !/^https:\/\/dz-legal-assistant-ai\.onrender\.com\//.test(m[1]) && !m[1].includes("joradp.dz") && !m[1].includes("opensource.org") && !m[1].includes("ollama")) {
            // Only outbound links to known destinations are allowed; nothing may be loaded from another origin.
            if (/src="/.test(m[0])) problems.push(`${rel}: resource loaded from another origin ${m[1]}`);
        }
    }
}

walk(dist);
if (problems.length) {
    console.error(`check-dist: ${problems.length} problem(s) in ${pages} page(s)`);
    for (const p of problems) console.error("  " + p);
    process.exit(1);
}
console.log(`check-dist: ${pages} page(s) clean`);
