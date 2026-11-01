// Generates the website's design tokens from the desktop app's theme (src/DzLegal.App/App.axaml and
// Styles/Workbench.axaml), so the site and the web demo carry exactly the colours, radii and shadows of the product.
// Writes site/src/styles/tokens.css and src/DzLegal.Web/wwwroot/tokens.css, plus the scales mark (ScalesMark.astro
// and wwwroot/favicon.svg) from WelcomeScalesGeometry. `--check` only verifies that the committed files are current.
import { readFileSync, writeFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { XMLParser } from "fast-xml-parser";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repo = process.env.DZ_REPO_ROOT ? resolve(process.env.DZ_REPO_ROOT) : resolve(siteDir, "..");
const check = process.argv.includes("--check");

const parser = new XMLParser({ ignoreAttributes: false, attributeNamePrefix: "@", preserveOrder: false, isArray: (name) => name === "GradientStop" });
const appXml = parser.parse(readFileSync(resolve(repo, "src/DzLegal.App/App.axaml"), "utf8"));
const workbenchXml = parser.parse(readFileSync(resolve(repo, "src/DzLegal.App/Styles/Workbench.axaml"), "utf8"));

// Avalonia colours are #RRGGBB or #AARRGGBB (alpha first), or a named colour.
function cssColor(value) {
    if (!value) return null;
    if (value === "Transparent") return "transparent";
    const hex = value.replace("#", "");
    if (hex.length === 6) return "#" + hex.toLowerCase();
    if (hex.length === 8) {
        const a = parseInt(hex.slice(0, 2), 16) / 255;
        const r = parseInt(hex.slice(2, 4), 16), g = parseInt(hex.slice(4, 6), 16), b = parseInt(hex.slice(6, 8), 16);
        return `rgb(${r} ${g} ${b} / ${a.toFixed(3).replace(/0+$/, "").replace(/\.$/, "")})`;
    }
    throw new Error("unknown colour " + value);
}

function direction(start, end) {
    if (start === "0%,0%" && end === "100%,100%") return "to bottom right";
    if (start === "0%,0%" && end === "0%,100%") return "to bottom";
    if (start === "0%,0%" && end === "100%,0%") return "to right";
    throw new Error(`gradient direction ${start} -> ${end} not mapped`);
}

function cssGradient(node) {
    const stops = node.GradientStop.map((s) => `${cssColor(s["@Color"])} ${Math.round(parseFloat(s["@Offset"]) * 100)}%`);
    return `linear-gradient(${direction(node["@StartPoint"], node["@EndPoint"])}, ${stops.join(", ")})`;
}

// Avalonia BoxShadows "x y blur spread #colour" -> CSS box-shadow.
function cssShadow(text) {
    const [x, y, blur, spread, colour] = text.trim().split(/\s+/);
    return `${x}px ${y}px ${blur}px ${spread}px ${cssColor(colour)}`;
}

function brushes(dictionary) {
    const out = {};
    const list = (node) => (node ? (Array.isArray(node) ? node : [node]) : []);
    for (const b of list(dictionary.SolidColorBrush)) out[b["@x:Key"]] = { color: cssColor(b["@Color"]) };
    for (const g of list(dictionary.LinearGradientBrush)) out[g["@x:Key"]] = { gradient: cssGradient(g), color: cssColor(g.GradientStop[Math.floor(g.GradientStop.length / 2)]["@Color"]) };
    for (const s of list(dictionary.BoxShadows)) out[s["@x:Key"]] = { shadow: cssShadow(s["#text"] ?? s) };
    return out;
}

const appThemes = appXml.Application["Application.Resources"].ResourceDictionary["ResourceDictionary.ThemeDictionaries"].ResourceDictionary;
const wbResources = workbenchXml.Styles["Styles.Resources"].ResourceDictionary;
const wbThemes = wbResources["ResourceDictionary.ThemeDictionaries"].ResourceDictionary;
const byKey = (dicts) => Object.fromEntries(dicts.map((d) => [d["@x:Key"], brushes(d)]));
const app = byKey(appThemes);
const wb = byKey(wbThemes);
const shared = brushes(wbResources);

// Token name -> source brush key. Names follow the product's own vocabulary.
const palette = [
    ["canvas", "CanvasBrush"], ["panel", "PanelBrush"], ["surface", "SurfaceBrush"], ["surface-alt", "SurfaceAltBrush"],
    ["paper", "PaperBrush"], ["paper-canvas", "PaperCanvasBrush"], ["hairline", "HairlineBrush"],
    ["text", "TextPrimaryBrush"], ["muted", "TextMutedBrush"], ["bar", "AppBarBrush"], ["bar-text", "AppBarTextBrush"],
    ["bar-muted", "AppBarMutedBrush"], ["accent", "AccentBrush"], ["accent-soft", "AccentSoftBrush"],
    ["gold", "PrimaryButtonBrush"], ["on-gold", "PrimaryButtonTextBrush"], ["input-border", "InputBorderBrush"],
    ["field-tools", "FieldToolsBrush"], ["field-tools-text", "FieldToolsTextBrush"],
    ["focus-tint", "DocumentFocusTintBrush"], ["hover-tint", "DocumentHoverTintBrush"],
    ["danger", "DangerBrush"], ["danger-soft", "DangerSoftBrush"], ["warning", "WarningBrush"], ["warning-soft", "WarningSoftBrush"],
    ["success", "SuccessBrush"], ["success-soft", "SuccessSoftBrush"],
];
const welcome = [
    ["highlight", "OnboardingHighlightBrush"], ["reading", "WelcomeReadingBrush"], ["overlay", "WelcomeOverlayBrush"],
    ["welcome-text", "WelcomeTextBrush"], ["welcome-secondary", "WelcomeSecondaryBrush"], ["welcome-control", "WelcomeControlBrush"],
    ["setup-surface", "SetupSurfaceBrush"], ["tour-card", "OnboardingTourCardBrush"], ["tour-card-shadow", "OnboardingTourCardShadow"],
    ["edge", "WelcomeEdgeBrush"], ["welcome-focus", "WelcomeFocusBrush"], ["welcome-button-border", "WelcomeButtonBorderBrush"],
    ["mark", "WelcomeMarkBrush"], ["logo", "WelcomeLogoBrush"],
];

function lines(theme) {
    const out = [];
    const emit = (name, entry) => {
        if (!entry) throw new Error(`missing brush for --${name} in ${theme}`);
        if (entry.shadow) out.push(`  --${name}: ${entry.shadow};`);
        else {
            out.push(`  --${name}: ${entry.color};`);
            out.push(`  --${name}-image: ${entry.gradient ?? "none"};`);
        }
    };
    for (const [name, key] of palette) emit(name, app[theme][key] ?? app.Light[key]);
    for (const [name, key] of welcome) emit(name, wb[theme][key]);
    return out;
}

const sharedLines = [
    `  --welcome-gold: ${shared.WelcomeGoldBrush.color};`,
    `  --welcome-button-text: ${shared.WelcomeButtonTextBrush.color};`,
    `  --tour-backdrop: ${shared.OnboardingTourBackdropBrush.gradient};`,
    "  --radius-card: 12px;", "  --radius-control: 8px;", "  --radius-onboarding-card: 16px;", "  --radius-button: 10px;",
    "  --shadow-card: 0 6px 16px 0 rgb(0 0 0 / 0.14);", "  --shadow-paper: 0 2px 10px 0 rgb(0 0 0 / 0.15);",
    "  --size-onboarding-title: 26px;", "  --size-onboarding-body: 16px;", "  --lh-onboarding-body: 29px;",
    "  --size-onboarding-secondary: 13px;", "  --size-onboarding-button: 15px;", "  --h-onboarding-button: 44px;",
    "  --w-tour: 420px;", "  --size-icon: 20px;", "  --blur-background: 8px;",
    "  --font-ui: \"Inter\", \"Noto Naskh Arabic\", system-ui, \"Segoe UI\", sans-serif;",
    "  --font-doc: \"Noto Naskh Arabic\", \"Inter\", \"Times New Roman\", serif;",
];

const css = [
    "/* Generated by site/scripts/tokens.mjs from src/DzLegal.App/App.axaml and Styles/Workbench.axaml. Do not edit:",
    "   change the desktop theme and run `npm run tokens` in site/. Light is the default; dark applies with data-theme. */",
    ":root {", ...sharedLines, ...lines("Light"), "  color-scheme: light;", "}",
    ":root[data-theme=\"dark\"] {", ...lines("Dark"), "  color-scheme: dark;", "}", "",
].join("\n");

const geometry = wbResources.StreamGeometry["#text"] ?? wbResources.StreamGeometry;
const svgMark = (cls) => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"${cls}><path d="${geometry}"/></svg>`;
const favicon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96"><rect width="96" height="96" rx="20" fill="#786039"/><g fill="none" stroke="#f2ede5" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" transform="translate(8 8) scale(0.8333)"><path d="${geometry}"/></g></svg>\n`;
const astroMark = `---
// Generated by site/scripts/tokens.mjs from WelcomeScalesGeometry (Styles/Workbench.axaml). Do not edit.
interface Props { class?: string; size?: number }
const { class: cls = "", size = 88 } = Astro.props;
---
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96" width={size} height={size} fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" class={cls} aria-hidden="true"><path d="${geometry}"/></svg>
`;

const outputs = [
    [resolve(siteDir, "src/styles/tokens.css"), css],
    [resolve(repo, "src/DzLegal.Web/wwwroot/tokens.css"), css],
    [resolve(siteDir, "src/components/ScalesMark.astro"), astroMark],
    [resolve(siteDir, "public/favicon.svg"), favicon],
    [resolve(repo, "src/DzLegal.Web/wwwroot/favicon.svg"), favicon],
];
let stale = 0;
for (const [file, content] of outputs) {
    const current = existsSync(file) ? readFileSync(file, "utf8") : null;
    if (current === content) continue;
    if (check) { console.error(`tokens: ${file} is out of date`); stale++; continue; }
    mkdirSync(dirname(file), { recursive: true });
    writeFileSync(file, content);
    console.log("tokens: wrote " + file);
}
if (check && stale) process.exit(1);
if (check) console.log("tokens: up to date");
void svgMark;
