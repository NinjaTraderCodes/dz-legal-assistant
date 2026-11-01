// Summarises the law library for the website's law page: each text's reference, edition, article count, status and
// official sources, read from data/law/codes/*.json and data/law/sources.json, plus the claim catalogue's types
// (data/law/claims/*.json). Never the article texts. Writes src/content/law.generated.json; `--check` verifies the
// committed file is current, so the page can never show a figure the data does not hold.
import { readFileSync, writeFileSync, existsSync, readdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repo = process.env.DZ_REPO_ROOT ? resolve(process.env.DZ_REPO_ROOT) : resolve(siteDir, "..");
const check = process.argv.includes("--check");
const lawDir = resolve(repo, "data/law");
const readJson = (file) => JSON.parse(readFileSync(file, "utf8").replace(/^﻿/, ""));
const jsonFiles = (dir) => readdirSync(dir).filter((f) => f.endsWith(".json")).sort();

const registry = Object.fromEntries(readJson(resolve(lawDir, "sources.json")).codes.map((c) => [c.id, c]));
const codes = jsonFiles(resolve(lawDir, "codes")).map((file) => {
    const code = readJson(resolve(lawDir, "codes", file));
    const articles = code.articles ?? [];
    const sources = [...new Set([code.source?.url, ...articles.map((a) => a.source?.url)].filter(Boolean))];
    const amendedBy = [...new Set(articles.map((a) => a.amendedBy).filter(Boolean))].sort();
    return {
        id: code.id,
        title: code.title,
        citationName: code.citationName,
        reference: code.reference,
        edition: code.edition ?? null,
        articles: articles.length,
        partial: Boolean(code.partial),
        verified: Boolean(code.verified),
        check: code.check ?? null,
        consolidatedAsOf: code.consolidatedAsOf ?? null,
        retrievedOn: code.source?.retrievedOn ?? null,
        amendedBy,
        sources,
        joReference: registry[code.id]?.joReference ?? null,
        officialSite: registry[code.id]?.officialSite ?? null,
    };
});
const claimAreas = jsonFiles(resolve(lawDir, "claims")).map((file) => {
    const area = readJson(resolve(lawDir, "claims", file));
    return { area: area.area, claims: area.claims.map((c) => ({ id: c.id, label: c.label, verified: Boolean(c.verified) })) };
});

const out = {
    _generated: "by site/scripts/law.mjs from data/law/codes/*.json, data/law/sources.json and data/law/claims/*.json; do not edit",
    totals: {
        articles: codes.reduce((n, c) => n + c.articles, 0),
        texts: codes.length,
        complete: codes.filter((c) => !c.partial).length,
        verified: codes.filter((c) => c.verified).length,
        claimTypes: claimAreas.reduce((n, a) => n + a.claims.length, 0),
    },
    codes,
    claimAreas,
};
const content = JSON.stringify(out, null, 2) + "\n";
const file = resolve(siteDir, "src/content/law.generated.json");
const current = existsSync(file) ? readFileSync(file, "utf8") : null;
if (current === content) { console.log("law: up to date"); process.exit(0); }
if (check) { console.error("law: " + file + " is out of date"); process.exit(1); }
writeFileSync(file, content);
console.log(`law: wrote ${file} (${out.totals.articles} articles, ${out.totals.texts} texts, ${out.totals.claimTypes} claim types)`);
