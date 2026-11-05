// Accessibility: axe-core on every page in both languages and themes. Zero violations is the bar.
import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const pages = (process.env.AXE_PAGES ?? "/ar/,/en/,/ar/try/,/en/try/,/ar/download/,/en/download/,/ar/law/,/en/law/,/ar/privacy/,/en/privacy/,/ar/about/,/en/about/,/ar/404/,/en/404/,/ar/docs/product-details/,/en/docs/product-details/,/en/docs/architecture/").split(",");
const themes = ["light", "dark"] as const;

for (const path of pages) {
    for (const theme of themes) {
        test(`${path} has no axe violations (${theme})`, async ({ page, context, baseURL }) => {
            // Dark is the default; the cookie names the theme either way, as a returning visitor's does.
            await context.addCookies([{ name: "dzlegal.theme", value: theme, url: baseURL! }]);
            // The scroll story fades sections in as the reader scrolls; the audit looks at the page's content, which
            // is what reduced motion (and no JavaScript) shows.
            await page.emulateMedia({ reducedMotion: "reduce" });
            await page.goto(path);
            if (theme === "dark") await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
            else await expect(page.locator("html")).not.toHaveAttribute("data-theme", "dark");
            const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"]).analyze();
            const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.length} × ${v.help}\n` + v.nodes.map((n) => `    ${n.target.join(" ")}: ${n.failureSummary?.split("\n").slice(1).join(" ")}`).join("\n"));
            expect(summary, summary.join("\n")).toHaveLength(0);
        });
    }
}

for (const theme of themes) {
    test(`the demo's first screen has no axe violations (${theme})`, async ({ page, context, baseURL }) => {
        await context.addCookies([{ name: "dzlegal.theme", value: theme, url: baseURL! }]);
        await page.goto("/app?lang=en");
        await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
        const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa"]).analyze();
        const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.length} × ${v.help}`);
        expect(summary, summary.join("\n")).toHaveLength(0);
    });
}

// The demo with its guide open, in both languages: the docked card and the light on the element it points at.
for (const theme of themes) {
    for (const lang of ["en", "ar"]) {
        test(`the demo with its guide open has no axe violations (${lang}, ${theme})`, async ({ page, context, baseURL }) => {
            await context.addCookies([{ name: "dzlegal.theme", value: theme, url: baseURL! }]);
            await page.goto(`/app?lang=${lang}&guide=1`);
            await expect(page.locator(".guide-card")).toBeVisible({ timeout: 30_000 });
            await expect(page.locator(".guide-ring")).toHaveCount(1);
            const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa"]).analyze();
            const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.length} × ${v.help}\n` + v.nodes.map((n) => `    ${n.target.join(" ")}`).join("\n"));
            expect(summary, summary.join("\n")).toHaveLength(0);
        });
    }
}
