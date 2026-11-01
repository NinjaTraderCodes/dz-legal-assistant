// Text access for the site: the product's own strings (generated from the app's resources) and the site's copy.
// Arabic is written first and is the default; English is the second locale.
import generated from "./strings.generated.json";
import ar from "./site.ar.json";
import en from "./site.en.json";

export type Lang = "ar" | "en";
export const langs: Lang[] = ["ar", "en"];
export const dirOf = (lang: Lang) => (lang === "ar" ? "rtl" : "ltr");
export const otherLang = (lang: Lang): Lang => (lang === "ar" ? "en" : "ar");
export const localeOf = (lang: Lang) => (lang === "ar" ? "ar-DZ" : "en");

type Strings = Record<string, { ar: string; en: string }>;
const product = generated as unknown as Strings;
const site: Record<Lang, Record<string, string>> = { ar, en };

/** A string from the app's own resources (Strings.resx), by key. */
export function s(lang: Lang, key: string): string {
    const entry = product[key];
    if (!entry) throw new Error(`product string ${key} is not exported (site/scripts/strings.mjs)`);
    return entry[lang];
}

/** A string of the site's own copy (site.ar.json / site.en.json), by key. */
export function t(lang: Lang, key: string): string {
    const value = site[lang][key];
    if (value === undefined) throw new Error(`site string ${key} is missing in site.${lang}.json`);
    return value;
}

/** Formats {0}, {1}… as the app's string.Format does. */
export function fmt(text: string, ...args: (string | number)[]): string {
    return text.replace(/\{(\d+)\}/g, (_, i) => String(args[Number(i)] ?? ""));
}
