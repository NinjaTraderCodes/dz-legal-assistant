// Turns the guide reel's captures (tests/guide-reel-shots.mjs, PNG at scale 2) into the sources the site builds from:
// at most 1440 px wide (twice the widest place the home shows them) as near-lossless WebP, then removes the PNG.
// Usage: node scripts/reel-sources.mjs [src/assets/shots/reel]
import { readdirSync, rmSync } from "node:fs";
import { resolve } from "node:path";
import sharp from "sharp";

const dir = resolve(process.argv[2] ?? "src/assets/shots/reel");
for (const name of readdirSync(dir).filter((f) => f.endsWith(".png"))) {
    const file = resolve(dir, name);
    await sharp(file).resize({ width: 1440, withoutEnlargement: true }).webp({ quality: 92, smartSubsample: true, effort: 6 }).toFile(file.replace(/\.png$/, ".webp"));
    rmSync(file);
}
console.log("reel-sources: done");
