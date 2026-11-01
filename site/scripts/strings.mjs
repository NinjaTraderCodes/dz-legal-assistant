// Exports the product's own wording (src/DzLegal.Presentation/Resources/Strings.resx = Arabic, Strings.en.resx = English)
// for the keys the website reuses, so the site never paraphrases the app, and its version (Directory.Build.props).
// Writes src/i18n/strings.generated.json; `--check` verifies the committed file is current.
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { XMLParser } from "fast-xml-parser";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repo = process.env.DZ_REPO_ROOT ? resolve(process.env.DZ_REPO_ROOT) : resolve(siteDir, "..");
const check = process.argv.includes("--check");

const KEYS = [
    "AppTitle", "WelcomeTitle", "WelcomeTagline", "WelcomeStart", "WelcomeLanguage", "WelcomeLogo", "OfflineStatus",
    "DraftBadge", "Compliance", "ComplianceScore", "StartNew", "StartOpen", "StartSample", "StartRecent", "StartTour",
    "LibraryComplete", "LibraryPartial", "SwitchLanguage", "CaseForm",
    "AboutPurpose", "AboutDraftTitle", "AboutDraft", "AboutPrivacyTitle", "AboutPrivacy", "AboutModelNote", "AboutSources",
    "AboutTeamLead", "AboutTeamDesign", "AboutTeamLegal", "AboutThirdUni", "AboutLicencesIntro", "AboutOwnImage", "AboutWelcomeImage",
    "AboutTitle", "AboutVersion", "AboutTeamTitle", "AboutSourcesTitle", "AboutLicencesTitle", "AboutNotices", "AboutOpenLibrary",
    "LibraryTitle", "LibraryIntro", "LibrarySummary", "LibraryArticles", "LibraryReference", "LibraryEdition", "LibraryAsOf", "LibraryAmendedBy",
    "LibraryOfficial", "LibraryPartialHint", "LibraryNotReviewed", "LibraryReviewed", "LibraryNote", "LibraryMonths", "LibraryWhat",
    "LibraryCheckJournal", "LibraryCheckJournalHint", "LibraryCheckPdf", "LibraryCheckPdfHint", "LibraryCheckImages", "LibraryCheckImagesHint",
    "Help7Title", "Help7Body1", "Help7Body2", "Help7Body3", "Help7Body4", "Help1Body3", "Help2Body3", "Help2Body4", "Help2Body5", "Help4Body1",
    "Help5Body2", "Help5Body3", "PrivacyNote",
];

const parser = new XMLParser({ ignoreAttributes: false, attributeNamePrefix: "@" });
function load(file) {
    const xml = parser.parse(readFileSync(resolve(repo, "src/DzLegal.Presentation/Resources", file), "utf8"));
    const data = Array.isArray(xml.root.data) ? xml.root.data : [xml.root.data];
    return Object.fromEntries(data.map((d) => [d["@name"], typeof d.value === "string" ? d.value : String(d.value ?? "")]));
}
const ar = load("Strings.resx");
const en = load("Strings.en.resx");
const out = {};
for (const key of KEYS) {
    if (!(key in ar) || !(key in en)) throw new Error(`strings: key ${key} missing in a resx`);
    out[key] = { ar: ar[key], en: en[key] };
}
// The version comes from the one place the product declares it.
const version = readFileSync(resolve(repo, "Directory.Build.props"), "utf8").match(/<Version>([^<]+)<\/Version>/)?.[1];
if (!version) throw new Error("strings: no <Version> in Directory.Build.props");
// Plain JSON (Astro imports it), so the provenance note travels as a key rather than a comment.
const content = JSON.stringify({ _generated: "by site/scripts/strings.mjs from Strings.resx, Strings.en.resx and Directory.Build.props; do not edit", _version: version, ...out }, null, 2) + "\n";
const file = resolve(siteDir, "src/i18n/strings.generated.json");
const current = existsSync(file) ? readFileSync(file, "utf8") : null;
if (current === content) { console.log("strings: up to date"); process.exit(0); }
if (check) { console.error("strings: " + file + " is out of date"); process.exit(1); }
writeFileSync(file, content);
console.log("strings: wrote " + file);
