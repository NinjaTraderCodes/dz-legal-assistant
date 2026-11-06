// Finds, in each picture of the guide reel (src/assets/shots/reel/step<n>-<lang>-dark.webp), where the guide's card and
// the ring it draws are: both are outlined in the demo's gold, and they are the only long gold lines in the frame (the
// gold buttons are much shorter). The box around those lines is written into src/content/guide-reel.json as each
// step's focus (its centre and the zoom that makes it fill most of a phone's width), so a phone shows that part of the
// picture large instead of the whole screen small. Usage: node scripts/reel-focus.mjs
import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import sharp from "sharp";

const dir = resolve("src/assets/shots/reel");
const notes = resolve("src/content/guide-reel.json");
const reel = JSON.parse(readFileSync(notes, "utf8"));
const gold = [198, 167, 109];
const near = (d, i) => Math.abs(d[i] - gold[0]) < 30 && Math.abs(d[i + 1] - gold[1]) < 30 && Math.abs(d[i + 2] - gold[2]) < 30;

for (const lang of Object.keys(reel)) {
    for (const step of reel[lang].steps) {
        const { data, info } = await sharp(resolve(dir, `step${step.step}-${lang}-dark.webp`)).removeAlpha().raw().toBuffer({ resolveWithObject: true });
        const { width, height } = info;
        // Rows holding a long gold run (a fifth of the frame or more), each with that run's start and end.
        const rows = [];
        for (let y = 0; y < height; y++) {
            let run = 0, start = 0, best = 0, bestStart = 0;
            for (let x = 0; x < width; x++) {
                if (near(data, (y * width + x) * 3)) { if (run === 0) start = x; run++; if (run > best) { best = run; bestStart = start; } }
                else run = 0;
            }
            if (best >= width * 0.2) rows.push({ y, from: bestStart, to: bestStart + best });
        }
        // An outline's edge is a few rows thick; a gold button is filled over many rows: only the thin bands count.
        const bands = [];
        for (const row of rows) {
            const last = bands.at(-1);
            if (last && row.y === last.at(-1).y + 1) last.push(row); else bands.push([row]);
        }
        // (The card's border and the ring are up to 6 rows; the score's gold bar, about 10, is not an outline.)
        const edges = bands.filter((band) => band.length <= 6).flat();
        let top = height, bottom = -1, left = width, right = -1;
        for (const e of edges) { top = Math.min(top, e.y); bottom = Math.max(bottom, e.y); left = Math.min(left, e.from); right = Math.max(right, e.to); }
        if (bottom < 0) throw new Error(`reel-focus: no gold outline found in step${step.step}-${lang}-dark`);
        const w = (right - left) / width;
        step.focus = {
            x: +((left + right) / 2 / width).toFixed(3),
            y: +((top + bottom) / 2 / height).toFixed(3),
            // Enough to make the box fill about nine tenths of the phone's width, between 1.8 times (below that the
            // card's text is too small to read; a wide box loses a little of its sides) and 2.2.
            zoom: +Math.min(2.2, Math.max(1.8, 0.9 / w)).toFixed(2),
        };
        console.log(`reel-focus: ${lang} step ${step.step}: box ${left},${top} to ${right},${bottom} of ${width}x${height}, zoom ${step.focus.zoom}`);
    }
}
writeFileSync(notes, JSON.stringify(reel, null, 2) + "\n");
