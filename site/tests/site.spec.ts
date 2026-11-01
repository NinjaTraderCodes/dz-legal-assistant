// The site as served by the app: both language homes, the switch, the not-found page, the headers, the demo shell.
import { test, expect, type APIResponse } from "@playwright/test";

// Razor writes non-ASCII text as numeric entities; compare on the decoded text.
const decode = (html: string) => html.replace(/&#x([0-9a-f]+);/gi, (_, hex) => String.fromCodePoint(parseInt(hex, 16))).replace(/&#(\d+);/g, (_, dec) => String.fromCodePoint(Number(dec)));
const text = async (response: APIResponse) => decode(await response.text());

const noInlineScript = (html: string) => {
    const inline = [...html.matchAll(/<script\b([^>]*)>/gi)].filter((m) => !/\ssrc=/.test(m[1]));
    expect(inline, "scripts without src").toHaveLength(0);
};

const csp = (response: APIResponse | null) => {
    const headers = response?.headersArray().filter((h) => h.name.toLowerCase() === "content-security-policy") ?? [];
    expect(headers, "one Content-Security-Policy header").toHaveLength(1);
    expect(headers[0].value).toContain("script-src 'self'");
    expect(headers[0].value).toContain("frame-ancestors 'none'");
};

test("the Arabic home is Arabic, right to left, with the product's title", async ({ page, request }) => {
    const response = await request.get("/ar/");
    expect(response.status()).toBe(200);
    csp(response);
    const html = await text(response);
    expect(html).toMatch(/<html[^>]*lang="ar"[^>]*dir="rtl"/);
    expect(html).toContain("مساعد صياغة العرائض");
    noInlineScript(html);
    await page.goto("/ar/");
    await expect(page.locator("h1")).toContainText("مساعد صياغة العرائض");
    await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
});

test("the English home is English, left to right", async ({ request }) => {
    const response = await request.get("/en/");
    expect(response.status()).toBe(200);
    const html = await text(response);
    expect(html).toMatch(/<html[^>]*lang="en"[^>]*dir="ltr"/);
    expect(html).toContain("DZ Legal Assistant");
    expect(html).toContain('rel="canonical" href="https://dz-legal-assistant-ai.onrender.com/en/"');
    expect(html).toContain('hreflang="ar-DZ"');
    noInlineScript(html);
});

test("the submitted address keeps working and names the product", async ({ request }) => {
    const response = await request.get("/", { maxRedirects: 0 });
    expect(response.status()).toBe(200);
    expect(await text(response)).toContain("DZ Legal Assistant");
    csp(response);
});

test("the language switch remembers the choice and comes back", async ({ request }) => {
    const response = await request.get("/lang/en?to=%2Fen%2F", { maxRedirects: 0 });
    expect([302, 307]).toContain(response.status());
    expect(response.headers()["location"]).toBe("/en/");
    expect(response.headers()["set-cookie"]).toContain("dzlegal.lang=en");
    const refused = await request.get("/lang/xx", { maxRedirects: 0 });
    expect(refused.status()).toBe(404);
    const elsewhere = await request.get("/lang/ar?to=//evil.example", { maxRedirects: 0 });
    expect(elsewhere.headers()["location"]).toBe("/ar/");
});

test("a bare page path redirects to its slash and an unknown path gets the site's 404", async ({ request }) => {
    const bare = await request.get("/en/404", { maxRedirects: 0 });
    expect(bare.status()).toBe(301);
    expect(bare.headers()["location"]).toBe("/en/404/");
    const missing = await request.get("/en/nothing-here/", { headers: { "Accept-Language": "en" } });
    expect(missing.status()).toBe(404);
    expect(await text(missing)).toContain("Page not found");
    const missingAr = await request.get("/x/", { headers: { "Accept-Language": "ar-DZ,fr;q=0.8" } });
    expect(missingAr.status()).toBe(404);
    expect(await text(missingAr)).toContain("الصفحة غير موجودة");
});

test("hashed assets are immutable and pages are revalidated", async ({ request }) => {
    const home = await request.get("/en/");
    expect(home.headers()["cache-control"]).toBe("no-cache");
    // The stylesheet is inlined; any hashed asset (the home script, a font) carries the immutable cache rule.
    const hashed = (await home.text()).match(/(?:src|href)="(\/_astro\/[^"]+)"/)?.[1];
    expect(hashed).toBeTruthy();
    const asset = await request.get(hashed!);
    expect(asset.status()).toBe(200);
    expect(asset.headers()["cache-control"]).toContain("immutable");
});

test("the demo follows the browser's language and shows its welcome before the circuit", async ({ request, page }) => {
    const arabic = await request.get("/app", { headers: { "Accept-Language": "ar-DZ,fr;q=0.8" } });
    expect(arabic.status()).toBe(200);
    const html = await text(arabic);
    expect(html).toMatch(/<html[^>]*lang="ar"[^>]*dir="rtl"/);
    expect(html).toContain('id="app-loading"');
    expect(html).toContain("جارٍ تحميل العرض التجريبي");
    csp(arabic);
    const english = await request.get("/app?lang=en", { headers: { "Accept-Language": "ar" } });
    expect(await text(english)).toMatch(/<html[^>]*lang="en"/);
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator("#app-loading")).toHaveCount(0);
    await expect(page.locator(".app h1")).toContainText("DZ Legal Assistant");
});

test("the demo's theme and language buttons are remembered", async ({ page }) => {
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await page.getByRole("button", { name: /theme|dark|light/i }).first().click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    const cookies = await page.context().cookies();
    expect(cookies.find((c) => c.name === "dzlegal.theme")?.value).toBe("dark");
});
