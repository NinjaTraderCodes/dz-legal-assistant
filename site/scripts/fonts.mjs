// Prepares the web fonts (runs before every build; the output folder is not committed):
//  - Inter (OFL): the woff2 files the web demo already ships (src/DzLegal.Web/wwwroot/ui-fonts) and their licence;
//  - Noto Naskh Arabic (OFL): the desktop's Arabic face (assets/fonts/*.ttf) subset to Arabic script, digits and
//    punctuation and converted to woff2, with its licence next to it.
import { readFileSync, writeFileSync, mkdirSync, copyFileSync, existsSync, statSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import subsetFont from "subset-font";

const siteDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repo = process.env.DZ_REPO_ROOT ? resolve(process.env.DZ_REPO_ROOT) : resolve(siteDir, "..");
const out = resolve(siteDir, "src/assets/fonts");
mkdirSync(out, { recursive: true });

const ranges = [[0x0600, 0x06ff], [0x0750, 0x077f], [0x08a0, 0x08ff], [0xfb50, 0xfdff], [0xfe70, 0xfeff], [0x0020, 0x007e], [0x00a0, 0x00bf], [0x2010, 0x2027], [0x2030, 0x203a], [0x200c, 0x200f], [0x202a, 0x202e], [0x2066, 0x2069]];
let text = "";
for (const [a, b] of ranges) for (let c = a; c <= b; c++) text += String.fromCodePoint(c);

for (const weight of ["Regular", "Bold"]) {
    const src = resolve(repo, `assets/fonts/NotoNaskhArabic-${weight}.ttf`);
    const dst = resolve(out, `NotoNaskhArabic-${weight}.woff2`);
    if (existsSync(dst) && statSync(dst).mtimeMs >= statSync(src).mtimeMs) continue;
    const woff2 = await subsetFont(readFileSync(src), text, { targetFormat: "woff2" });
    writeFileSync(dst, woff2);
    console.log(`fonts: ${weight} ${Math.round(woff2.length / 1024)} KB`);
}
copyFileSync(resolve(repo, "assets/fonts/OFL.txt"), resolve(out, "OFL-NotoNaskhArabic.txt"));
for (const name of ["Inter-latin.woff2", "Inter-latin-ext.woff2", "OFL-Inter.txt"]) {
    copyFileSync(resolve(repo, "src/DzLegal.Web/wwwroot/ui-fonts", name), resolve(out, name));
}
console.log("fonts: ready in " + out);
