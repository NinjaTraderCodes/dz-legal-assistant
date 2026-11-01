// Accessibility: axe-core on every page in both languages and themes. Zero violations is the bar.
import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const pages = (process.env.AXE_PAGES ?? "/ar/,/en/,/ar/404/,/en/404/").split(",");
const themes = ["light", "dark"] as const;

for (const path of pages) {
    for (const theme of themes) {
        test(`${path} has no axe violations (${theme})`, async ({ page, context, baseURL }) => {
            if (theme === "dark") {
                await context.addCookies([{ name: "dzlegal.theme", value: "dark", url: baseURL! }]);
            }
            // The scroll story fades sections in as the reader scrolls; the audit looks at the page's content, which
            // is what reduced motion (and no JavaScript) shows.
            await page.emulateMedia({ reducedMotion: "reduce" });
            await page.goto(path);
            const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"]).analyze();
            const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.length} × ${v.help}\n` + v.nodes.map((n) => `    ${n.target.join(" ")}: ${n.failureSummary?.split("\n").slice(1).join(" ")}`).join("\n"));
            expect(summary, summary.join("\n")).toHaveLength(0);
        });
    }
}

test("the demo's first screen has no axe violations", async ({ page }) => {
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa"]).analyze();
    const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.length} × ${v.help}`);
    expect(summary, summary.join("\n")).toHaveLength(0);
});
