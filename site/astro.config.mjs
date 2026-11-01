// The public website of DZ Legal Assistant: a static site served by src/DzLegal.Web at the same address as the demo.
// Arabic first (the product's language), English as the second locale; every page lives under /ar/ or /en/, and the
// server chooses between them for "/". The output must contain no inline scripts (the server's CSP allows only its
// own files), so asset inlining is switched off and scripts/check-dist.mjs verifies the result after every build.
import { defineConfig } from "astro/config";
import sitemap from "@astrojs/sitemap";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    site: "https://dz-legal-assistant-ai.onrender.com",
    output: "static",
    trailingSlash: "always",
    compressHTML: true,
    i18n: {
        defaultLocale: "ar",
        locales: ["ar", "en"],
        routing: { prefixDefaultLocale: true, redirectToDefaultLocale: false },
    },
    devToolbar: { enabled: false },
    // The stylesheet is inlined: one render-blocking round trip fewer on a far-away host (inline styles are allowed by
    // the server's policy; inline scripts are not).
    build: { inlineStylesheets: "always", assets: "_astro" },
    integrations: [
        sitemap({
            i18n: { defaultLocale: "ar", locales: { ar: "ar-DZ", en: "en" } },
            filter: (page) => !page.includes("/404/"),
        }),
    ],
    vite: {
        plugins: [tailwindcss()],
        build: { assetsInlineLimit: 0 },
        server: { fs: { allow: [".."] } },
    },
});
