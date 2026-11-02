// What Lighthouse took as the largest contentful paint on a page, from the HTML report it wrote (the JSON is
// embedded in it): the element, the four LCP subparts, and the requests on the critical path. Usage:
//   node scripts/lcp.mjs ../docs/website/phase-4/lighthouse-ar-mobile.report.html [more reports…]
import { readFileSync } from "node:fs";

for (const file of process.argv.slice(2)) {
    const html = readFileSync(file, "utf8");
    const json = JSON.parse(html.match(/window\.__LIGHTHOUSE_JSON__ = (\{.*?\});<\/script>/s)[1]);
    const a = json.audits;
    // Lighthouse 13 reports the LCP element through the breakdown insight: a table of subparts, then the node.
    const items = a["lcp-breakdown-insight"]?.details?.items ?? [];
    const subparts = items.find((i) => i.type === "table")?.items ?? [];
    const element = items.find((i) => i.type === "node");
    console.log(`== ${file.split(/[\/]/).pop()}: LCP ${a["largest-contentful-paint"].displayValue}, FCP ${a["first-contentful-paint"].displayValue}`);
    console.log("   element:", element?.selector ?? "(not reported)", "|", (element?.snippet ?? "").replace(/\s+/g, " ").slice(0, 140));
    for (const p of subparts) console.log(`   ${p.label}: ${Math.round(p.duration)} ms`);
    const chains = a["network-dependency-tree-insight"]?.details?.items?.find((i) => i.type === "network-tree")?.chains ?? {};
    const walk = (node, depth) => {
        for (const c of Object.values(node)) {
            const ms = Math.round((c.navStartToEndTime ?? 0) - (c.navStartToStartTime ?? 0));
            console.log("   " + "  ".repeat(depth) + String(c.url).replace(/^https?:\/\/[^/]+/, "") + ` (${ms} ms, ${c.transferSize ?? "?"} B)`);
            if (c.children) walk(c.children, depth + 1);
        }
    };
    walk(chains, 0);
}
