// "Test it in 5 minutes", followed in the demo exactly as the website's guide says, with the same button labels (the
// generated string tables the guide is built from), so the guide cannot go out of date. It runs against an instance
// whose AI is the local stand-in model (tests/mock-model.mjs), so every step completes and no credit is spent:
//   node tests/mock-model.mjs &
//   GROQ_API_KEY=x Engines__GroqEndpoint=http://127.0.0.1:5090/v1/ Engines__GroqModel=mock Site__Root=../../site/dist \
//     dotnet run --project src/DzLegal.Web --no-launch-profile --urls http://localhost:5082
//   GUIDE_BASE_URL=http://localhost:5082 npx playwright test tests/guide.spec.ts
// Skipped when GUIDE_BASE_URL is not set (the default instance may have real AI keys).
import { test, expect } from "@playwright/test";
import { readFileSync } from "node:fs";

const g = JSON.parse(readFileSync(new URL("../src/i18n/strings.generated.json", import.meta.url), "utf8")) as Record<string, { en: string; ar: string }>;
const label = (key: string) => g[key].en;

test("the five steps of the guide happen in the demo as the website says", async ({ page }) => {
    test.skip(!process.env.GUIDE_BASE_URL, "GUIDE_BASE_URL not set");
    test.setTimeout(180_000);
    const base = process.env.GUIDE_BASE_URL!;
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto(base + "/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 60_000 });
    // The first screen shows the product: the three panels and the score, the one-line disclosure, the invitation.
    for (const title of [label("CaseForm"), label("Document"), label("Compliance")]) {
        await expect(page.getByRole("heading", { name: title, exact: false }).first()).toBeInViewport();
    }
    await expect(page.locator(".score")).toBeInViewport();
    await expect(page.locator(".disclosure summary")).toContainText(label("Web.DisclosureLine"));
    await expect(page.locator(".invite")).toContainText(label("Web.GuideInvite"));
    await expect(page.locator(".guide.tour")).toHaveCount(0);

    // Step 1: open the sample "Unpaid loan".
    await page.locator("#sample-picker").selectOption({ label: label("Sample.debt") });
    await expect(page.locator(".score")).toHaveText("8 / 9");
    await expect(page.locator(".steps .step")).toHaveCount(9);
    await expect(page.locator(".paper")).toBeVisible();

    // Step 2: clear the defendant's domicile, through the guide's own button.
    await page.getByRole("button", { name: label("Web.GuideShow"), exact: true }).click();
    const guide = page.locator(".guide.tour");
    await expect(guide).toBeVisible();
    await expect(guide).toContainText(label("Web.GuideTitle"));
    await expect(guide.locator("li")).toHaveCount(5);
    await guide.getByRole("button", { name: label("Web.OpenDefendant") }).click();
    await expect(guide).toHaveCount(0);
    const domicile = page.locator("#f-defendant-domicile");
    const address = await domicile.inputValue();
    await domicile.fill("");
    await expect(page.locator(".score")).toHaveText("7 / 9");
    await expect(page.locator(".finding.mandatory").filter({ hasText: /15-3/ })).toHaveCount(1);
    // The formal check is advisory: export is not refused for a missing element (the lawyer decides).
    const earlyExport = page.waitForEvent("download", { timeout: 30_000 });
    await page.getByRole("button", { name: label("ExportPdf") }).click();
    expect((await earlyExport).suggestedFilename()).toMatch(/\.pdf$/);
    await expect(page.locator(".export-blockers")).toHaveCount(0);
    await domicile.fill(address);
    await expect(page.locator(".score")).toHaveText("8 / 9");

    // Step 3: analyse the facts; nothing is ticked before "Yes, this is the claim type".
    await page.getByRole("button", { name: label("Web.GuideShow"), exact: true }).click();
    await guide.getByRole("button", { name: label("Web.OpenGrounds") }).click();
    await page.getByRole("button", { name: label("AnalyseGrounds"), exact: true }).click();
    await expect(page.locator(".confirm-claim")).toBeVisible({ timeout: 60_000 });
    await expect(page.locator(".confirm-claim")).toContainText(label("ClaimConfirmPrompt").slice(0, 30));
    await expect(page.locator(".article-card.selected")).toHaveCount(0);
    await page.getByRole("button", { name: label("ConfirmClaim") }).click();
    await expect(page.locator(".article-card.selected").first()).toBeVisible();

    // Step 4: draft the legal grounds; the section carries the AI mark and the review box.
    await page.getByRole("button", { name: label("Generate"), exact: true }).click();
    const reviewBox = page.locator(".review-list .reviewed input");
    await expect(reviewBox).toHaveCount(1, { timeout: 90_000 });
    // The AI-written section is marked in the paper itself; selecting it shows its origin in the strip above the paper.
    await expect(page.locator(".paper .section.ai")).toHaveCount(1);
    await page.locator(".paper .section.ai").click();
    await expect(page.locator(".field-tools")).toContainText(label("NarrativeSectionHint"));
    await expect(page.locator(".review-list .reviewed")).toContainText(label("ReviewedCheck"));

    // Step 5: export is refused before the tick and downloads after it.
    await page.getByRole("button", { name: label("ExportPdf") }).click();
    const blockers = page.locator(".export-blockers[role=alert]");
    await expect(blockers).toBeVisible();
    await expect(blockers).toContainText(label("ExportBlocked"));
    await reviewBox.check();
    await expect(page.locator(".export-blockers")).toHaveCount(0);
    const download = page.waitForEvent("download", { timeout: 30_000 });
    await page.getByRole("button", { name: label("ExportPdf") }).click();
    expect((await download).suggestedFilename()).toMatch(/\.pdf$/);
});
