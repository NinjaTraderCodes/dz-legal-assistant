// The product's fixed facts the pages share: its version (from Directory.Build.props through strings.mjs), where it
// lives on GitHub, and the pages of this site. One place, so a release or a renamed page changes one line.
import generated from "../i18n/strings.generated.json";
import { t, type Lang } from "../i18n";

import { repoUrl } from "./repo";

export const version = (generated as { _version: string })._version;
export { repoUrl, blob } from "./repo";
export const releaseUrl = `${repoUrl}/releases/latest`;
export const releasesUrl = `${repoUrl}/releases`;

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
