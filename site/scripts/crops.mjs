// Cuts the pictures the home page shows out of full frames: the desktop app's frames rendered by the headless UI tests
// (DZLEGAL_SCREENSHOTS=<dir> dotnet test tests/DzLegal.App.Tests, outside the repository) and the frames of the live
// recording in docs/images/readme (real output of the local model). The crops are PNG; the build turns them into
// AVIF and WebP. Arabic frames mirror the English ones, so their boxes are given separately. Usage:
//   node scripts/crops.mjs --desktop C:\Users\TheShadow\dz-shots-src [--only proofs,ai]
import { mkdirSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const desktop = resolve(arg("desktop", "../../dz-shots-src"));
const only = arg("only", "proofs,ai").split(",");
const live = resolve(siteDir, "../docs/images/readme");
const out = resolve(siteDir, "src/assets/shots");

// A box is [left, top, width, height] in the frame's pixels. The live frames carry burned-in captions at the top
// (the recording's own timing line) and subtitles at the bottom; the boxes stop above the subtitles.
const box = (left, top, right, bottom) => ({ left, top, width: right - left, height: bottom - top });
const themes = ["light", "dark"];
const langs = ["ar", "en"];

const crops = [];
if (only.includes("proofs")) {
    for (const lang of langs) for (const theme of themes) {
        // The law library: the first text's card (the Code of Civil and Administrative Procedure, complete, read against
        // the Official Journal). The Arabic layout mirrors the English one.
        crops.push({ out: `proofs/library-${lang}-${theme}.png`, src: `${desktop}/phase2-library-list-${lang}-${theme}-1366x768.png`, box: lang === "en" ? box(44, 160, 664, 616) : box(702, 160, 1322, 616) });
        // The app bar's end: the offline badge beside the theme and language controls.
        crops.push({ out: `proofs/offline-${lang}-${theme}.png`, src: `${desktop}/workbench-1366-${lang}-${theme}.png`, box: lang === "en" ? box(960, 0, 1366, 54) : box(0, 0, 406, 54) });
    }
    // The refused-export notice of the desktop app (docs/images/workbench-export-blocked.png: Arabic, light, one frame).
    crops.push({ out: "proofs/export-blocked.png", src: resolve(siteDir, "../docs/images/workbench-export-blocked.png"), box: box(362, 270, 946, 350) });
    // The drafted grounds on the paper with the review box, from the live recording (one theme, shown in both).
    crops.push({ out: "proofs/grounds-live.png", src: `${live}/live-ai-drafted-grounds.jpg`, box: box(20, 120, 1800, 975) });
}
if (only.includes("ai")) {
    crops.push({ out: "ai/stage1-reading.png", src: `${live}/live-ai-reading-facts.jpg`, box: box(24, 420, 395, 800) });
    crops.push({ out: "ai/stage2-candidates.png", src: `${live}/live-ai-drafting.jpg`, box: box(24, 640, 390, 990) });
    crops.push({ out: "ai/stage3-proposal.png", src: `${live}/live-ai-proposal.jpg`, box: box(24, 560, 390, 900) });
    crops.push({ out: "ai/stage4-drafting.png", src: `${live}/live-ai-drafting.jpg`, box: box(430, 300, 1550, 420) });
    crops.push({ out: "ai/stage4-timing.png", src: `${live}/live-ai-drafting.jpg`, box: box(640, 55, 1250, 105) });
    crops.push({ out: "ai/stage4-drafted.png", src: `${live}/live-ai-drafted-grounds.jpg`, box: box(20, 120, 1800, 975) });
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
