// The product's fixed facts the pages share: its version (from Directory.Build.props through strings.mjs), the
// address of the Windows download, and the pages of this site. One place, so a release or a renamed page changes one line.
import generated from "../i18n/strings.generated.json";
import { t, type Lang } from "../i18n";

export const version = (generated as { _version: string })._version;
/**
 * The Windows zip itself, so the button downloads it at once. The site links nowhere else on GitHub (no repository,
 * no release pages); scripts/check-dist.mjs fails the build if a page does. Change it with each release.
 */
export const downloadUrl = "https://github.com/NinjaTraderCodes/dz-legal-assistant/releases/download/v0.9.1/DzLegalAssistant-0.9.1-win-x64.zip";

export interface SitePage { key: string; path: string; href: string; label: string }
/** The site's pages after the home, in the order the header and the footer show them. */
export function pages(lang: Lang): SitePage[] {
    const page = (key: string, path: string) => ({ key, path, href: `/${lang}${path}`, label: t(lang, `nav.${key}`) });
    return [
        page("try", "/try/"),
        { key: "how", path: "/#how", href: `/${lang}/#how`, label: t(lang, "nav.how") },
        page("library", "/law/"),
        page("download", "/download/"),
        page("privacy", "/privacy/"),
        page("about", "/about/"),
    ];
}
