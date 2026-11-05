// Prepares the web fonts (runs before every build; the output folder is not committed):
//  - Inter (OFL): the woff2 files the web demo already ships (src/DzLegal.Web/wwwroot/ui-fonts) and their licence;
//  - Noto Naskh Arabic (OFL): the desktop's Arabic face (assets/fonts/*.ttf) subset to Arabic script, digits and
//    punctuation and converted to woff2, with its licence next to it; it sets the petition and the app's pieces;
//  - the website's own faces (site/fonts, OFL, licences beside them and in public/licences): Markazi Text for titles,
//    Readex Pro for text and controls, each cut into an Arabic and a Latin file so a page fetches only its script;
//    Aref Ruqaa for the stamps, cut to the letters of the stamp texts (stamp.* in site.ar.json).
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

// The website's own faces. Latin keeps Latin-1 and Latin Extended-A, which French will need; the Arabic files carry
// the digits and punctuation an Arabic line uses. Readex Pro's second axis (HEXP) is pinned at its default.
const latin = [[0x0020, 0x007e], [0x00a0, 0x017f], [0x2010, 0x2027], [0x2030, 0x203a], [0x20ac, 0x20ac], [0x2122, 0x2122], [0x2190, 0x2193], [0x2212, 0x2212]];
const arabic = [[0x0600, 0x06ff], [0x0750, 0x077f], [0xfb50, 0xfdff], [0xfe70, 0xfeff], [0x0020, 0x0040], [0x00a0, 0x00a0], [0x00ab, 0x00ab], [0x00bb, 0x00bb], [0x2010, 0x2027], [0x200c, 0x200f], [0x2066, 0x2069]];
// The stamps carry a few fixed words: Aref Ruqaa is cut to the letters of the Arabic stamp texts only.
const siteAr = JSON.parse(readFileSync(resolve(siteDir, "src/i18n/site.ar.json"), "utf8"));
const stampText = Object.entries(siteAr).filter(([key]) => /^stamp\.[a-z]+$/.test(key)).map(([, value]) => value).join("") + " ";
const chars = (ranges) => { let s = ""; for (const [a, b] of ranges) for (let c = a; c <= b; c++) s += String.fromCodePoint(c); return s; };
const own = [
    { src: "MarkaziText-Variable.ttf", dst: "Markazi-ar.woff2", text: chars(arabic) },
    { src: "MarkaziText-Variable.ttf", dst: "Markazi-latin.woff2", text: chars(latin) },
    { src: "ReadexPro-Variable.ttf", dst: "Readex-ar.woff2", text: chars(arabic), axes: { HEXP: 0, wght: { min: 300, max: 700 } } },
    { src: "ReadexPro-Variable.ttf", dst: "Readex-latin.woff2", text: chars(latin), axes: { HEXP: 0, wght: { min: 300, max: 700 } } },
    { src: "ArefRuqaa-Bold.ttf", dst: "Ruqaa-ar.woff2", text: stampText },
];
for (const font of own) {
    const src = resolve(siteDir, "fonts", font.src);
    const dst = resolve(out, font.dst);
    const inputs = [src, fileURLToPath(import.meta.url), resolve(siteDir, "src/i18n/site.ar.json")];
    if (existsSync(dst) && inputs.every((file) => statSync(dst).mtimeMs >= statSync(file).mtimeMs)) continue;
    const woff2 = await subsetFont(readFileSync(src), font.text, { targetFormat: "woff2", ...(font.axes ? { variationAxes: font.axes } : {}) });
    writeFileSync(dst, woff2);
    console.log(`fonts: ${font.dst} ${Math.round(woff2.length / 1024)} KB`);
}
for (const name of ["OFL-MarkaziText.txt", "OFL-ReadexPro.txt", "OFL-ArefRuqaa.txt"]) copyFileSync(resolve(siteDir, "fonts", name), resolve(out, name));
console.log("fonts: ready in " + out);
