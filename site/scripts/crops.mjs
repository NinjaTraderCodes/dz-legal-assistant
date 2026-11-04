// Cuts the proof pictures the home page shows out of the desktop app's own frames: the app rendered at scale 2
// (3840 x 2160 for its 1920 x 1080 layout) by a capture tool outside the repository, with the local model's real output,
// in both languages and themes. The crops are PNG; the build turns them into WebP. Arabic frames mirror the English
// ones, so their boxes are given separately. Usage:
//   node scripts/crops.mjs [--captures C:\Users\TheShadow\dz-laptop-shots\scale2-run-20261104-205808] [--only desktop]
import { mkdirSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const only = arg("only", "desktop").split(",");
const captures = resolve(arg("captures", "../../dz-laptop-shots/scale2-run-20261104-205808"));
const out = resolve(siteDir, "src/assets/shots");

// A box is given by its left, top, right and bottom edges in the frame's pixels.
const box = (left, top, right, bottom) => ({ left, top, width: right - left, height: bottom - top });
const themes = ["light", "dark"];
const langs = ["ar", "en"];

const crops = [];
if (only.includes("desktop")) {
    // The desktop app rendered at scale 2 (3840 x 2160 for a 1920 x 1080 layout) by the capture tool outside the repository,
    // with the local model gemma3:4b on the «Unpaid loan» sample (docs/website/phase-6/REPORT.md, push 2 follow-up, says
    // which run). Both languages and themes; the Arabic layout mirrors the English one, so its boxes are its own. The
    // paper is cut inside its white sheet, so no grey canvas shows.
    const boxes = {
        en: { reading: box(50, 1215, 760, 1572), proposed: box(50, 1590, 760, 1905), confirm: box(50, 862, 760, 1120), paper: box(1298, 740, 2676, 1770) },
        ar: { reading: box(3077, 1273, 3790, 1665), proposed: box(3077, 1688, 3790, 1885), confirm: box(3090, 900, 3790, 1172), paper: box(1177, 790, 2551, 1810) },
    };
    for (const lang of langs) for (const theme of themes) for (const [name, b] of Object.entries(boxes[lang])) {
        crops.push({ out: `desktop/${name}-${lang}-${theme}.png`, src: `${captures}/app-${lang}-${theme}.png`, box: b });
    }
}

let failed = 0;
for (const c of crops) {
    if (!existsSync(c.src)) { console.error(`missing source: ${c.src}`); failed++; continue; }
    const target = resolve(out, c.out);
    mkdirSync(dirname(target), { recursive: true });
    await sharp(c.src).extract(c.box).png({ compressionLevel: 9, palette: false }).toFile(target);
    console.log(`${c.out} ${c.box.width}x${c.box.height}`);
}
if (failed) process.exit(1);
