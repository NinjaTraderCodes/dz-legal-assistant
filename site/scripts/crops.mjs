// Cuts the pictures the home page shows out of full frames: the desktop app's own 1920 x 1080 captures (outside the
// repository; the local model's drafted grounds, both languages and themes) and, for the drafting in progress only, the
// frames of the live recording in docs/images/readme (real output of the local model). The crops are PNG; the build turns them into
// AVIF and WebP. Arabic frames mirror the English ones, so their boxes are given separately. Usage:
//   node scripts/crops.mjs --desktop C:\Users\TheShadow\dz-shots-src [--captures C:\Users\TheShadow\dz-laptop-shots] [--only ai,desktop]
import { mkdirSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const desktop = resolve(arg("desktop", "../../dz-shots-src"));
const only = arg("only", "ai,desktop").split(",");
const captures = resolve(arg("captures", "../../dz-laptop-shots"));
const live = resolve(siteDir, "../docs/images/readme");
const out = resolve(siteDir, "src/assets/shots");

// A box is [left, top, width, height] in the frame's pixels. The live frames carry burned-in captions at the top
// (the recording's own timing line) and subtitles at the bottom; the boxes stop above the subtitles.
const box = (left, top, right, bottom) => ({ left, top, width: right - left, height: bottom - top });
const themes = ["light", "dark"];
const langs = ["ar", "en"];

const crops = [];
if (only.includes("desktop")) {
    // The desktop app's own 1920 x 1080 captures (item 0 of phase 5: the «Unpaid loan» sample with the grounds drafted by
    // gemma3:4b, the review ticked, 9 of 9), in both languages and themes. The Arabic layout mirrors the English one,
    // not pixel for pixel, so its boxes are given separately.
    const boxes = {
        en: { reading: box(26, 640, 380, 824), proposed: box(26, 826, 380, 982), confirm: box(26, 458, 380, 632), paper: box(430, 436, 1550, 1026) },
        ar: { reading: box(1540, 658, 1896, 866), proposed: box(1540, 866, 1896, 976), confirm: box(1540, 470, 1896, 660), paper: box(370, 436, 1490, 1026) },
    };
    for (const lang of langs) for (const theme of themes) for (const [name, b] of Object.entries(boxes[lang])) {
        crops.push({ out: `desktop/${name}-${lang}-${theme}.png`, src: `${captures}/app-${lang}-${theme}-1920x1080.png`, box: b });
    }
}
if (only.includes("ai")) {
    crops.push({ out: "ai/stage4-drafting.png", src: `${live}/live-ai-drafting.jpg`, box: box(430, 300, 1550, 420) });
    crops.push({ out: "ai/stage4-timing.png", src: `${live}/live-ai-drafting.jpg`, box: box(640, 55, 1250, 105) });
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
