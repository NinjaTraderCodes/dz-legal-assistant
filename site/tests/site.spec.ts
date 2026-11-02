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

test("the submitted address serves the website in the visitor's language, English by default", async ({ request }) => {
    // No language header at all: English, with the product's name on the page (what the uptime check greps for).
    const plain = await request.get("/", { maxRedirects: 0, headers: { "Accept-Language": "" } });
    expect(plain.status()).toBe(200);
    const html = await text(plain);
    expect(html).toMatch(/<html[^>]*lang="en"[^>]*dir="ltr"/);
    expect(html).toContain("DZ Legal Assistant");
    expect(html).toContain('rel="canonical" href="https://dz-legal-assistant-ai.onrender.com/en/"');
    expect(html).not.toContain('name="robots"');
    expect(plain.headers()["vary"]).toContain("Accept-Language");
    csp(plain);
    // An Arabic browser gets the Arabic home.
    const arabic = await request.get("/", { maxRedirects: 0, headers: { "Accept-Language": "ar-DZ,fr;q=0.8" } });
    expect(arabic.status()).toBe(200);
    expect(await text(arabic)).toMatch(/<html[^>]*lang="ar"[^>]*dir="rtl"/);
    // A remembered choice wins over the browser.
    const remembered = await request.get("/", { maxRedirects: 0, headers: { "Accept-Language": "ar", Cookie: "dzlegal.lang=en" } });
    expect(await text(remembered)).toMatch(/<html[^>]*lang="en"/);
    // A French browser gets English.
    const french = await request.get("/", { maxRedirects: 0, headers: { "Accept-Language": "fr-FR,fr;q=0.9" } });
    expect(await text(french)).toMatch(/<html[^>]*lang="en"/);
});

test("every demo link on the site points to /app, and the demo keeps its own address and health check", async ({ page, request }) => {
    for (const lang of ["ar", "en"]) {
        await page.goto(`/${lang}/`);
        const links = await page.locator('a[href^="/app"]').evaluateAll((as) => as.map((a) => a.getAttribute("href")));
        expect(links.length, `${lang}: demo links`).toBeGreaterThanOrEqual(3);
        for (const href of links) expect(href).toMatch(new RegExp(`^/app\\?lang=${lang}$`));
    }
    const app = await request.get("/app?lang=ar", { maxRedirects: 0 });
    expect(app.status()).toBe(200);
    expect(await text(app)).toMatch(/<html[^>]*lang="ar"[^>]*dir="rtl"/);
    expect(await text(app)).toContain('name="robots" content="noindex"');
    const health = await request.get("/healthz");
    expect(health.status()).toBe(200);
    expect(await health.json()).toMatchObject({ status: "ok", site: true });
});

// The fallback: an app started with a wrong Site__Root (no website folder) must still answer "/" with the demo.
// Run it with FALLBACK_BASE_URL pointing at such an instance; it is skipped otherwise.
test("without the website folder, the submitted address falls back to the demo", async ({ request }) => {
    test.skip(!process.env.FALLBACK_BASE_URL, "FALLBACK_BASE_URL not set");
    const base = process.env.FALLBACK_BASE_URL!;
    const root = await request.get(base + "/", { maxRedirects: 0 });
    expect(root.status()).toBe(200);
    const html = await text(root);
    expect(html).toContain('id="app-loading"');
    expect(html).toContain("DZ Legal Assistant");
    const health = await request.get(base + "/healthz");
    expect(await health.json()).toMatchObject({ status: "ok", site: false });
    const app = await request.get(base + "/app");
    expect(app.status()).toBe(200);
    const site = await request.get(base + "/en/", { maxRedirects: 0 });
    expect(site.status()).toBe(404);
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

test("the demo's theme and language buttons are remembered; dark is its default", async ({ page, request }) => {
    // A first visit is dark on the site and on the demo alike; a remembered "light" lifts it on both.
    expect(await text(await request.get("/app?lang=en"))).toMatch(/<html[^>]*data-theme="dark"/);
    expect(await text(await request.get("/app?lang=en", { headers: { Cookie: "dzlegal.theme=light" } }))).not.toContain('data-theme="dark"');
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator(".app")).toHaveClass(/dark/);
    await page.getByRole("button", { name: /theme|dark|light/i }).first().click();
    await expect(page.locator(".app")).not.toHaveClass(/dark/);
    await expect(page.locator("html")).not.toHaveAttribute("data-theme", "dark");
    const cookies = await page.context().cookies();
    expect(cookies.find((c) => c.name === "dzlegal.theme")?.value).toBe("light");
});

test("the site is dark by default, and a chosen light theme is remembered for the demo too", async ({ page, request }) => {
    for (const path of ["/en/", "/ar/download/", "/en/law/", "/ar/privacy/", "/en/about/"]) {
        expect(await text(await request.get(path)), path).toMatch(/<html[^>]*data-theme="dark"/);
    }
    await page.goto("/en/");
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    const toggle = page.locator("[data-theme-toggle]");
    await expect(toggle).toHaveAttribute("aria-pressed", "true");
    await toggle.click();
    await expect(page.locator("html")).not.toHaveAttribute("data-theme", "dark");
    await expect(toggle).toHaveAttribute("aria-pressed", "false");
    expect((await page.context().cookies()).find((c) => c.name === "dzlegal.theme")?.value).toBe("light");
    // The choice survives a reload (theme-init.js reads it before the first paint) and reaches the demo.
    await page.reload();
    await expect(page.locator("html")).not.toHaveAttribute("data-theme", "dark");
    await page.goto("/app?lang=en");
    await expect(page.locator("html")).not.toHaveAttribute("data-theme", "dark");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator(".app")).not.toHaveClass(/dark/);
});

test("the four secondary pages exist in both languages, with their canonical and alternate links", async ({ request }) => {
    for (const [path, arTitle, enTitle] of [["download", "التطبيق المكتبي لويندوز", "The desktop app for Windows"], ["law", "المكتبة القانونية", "Law library"], ["privacy", "بياناتك لا تغادر حاسوبك", "your data stays on your computer"], ["about", "حول التطبيق", "About"]]) {
        for (const [lang, title] of [["ar", arTitle], ["en", enTitle]]) {
            const response = await request.get(`/${lang}/${path}/`);
            expect(response.status(), `${lang}/${path}`).toBe(200);
            csp(response);
            const html = await text(response);
            expect(html).toMatch(new RegExp(`<html[^>]*lang="${lang}"[^>]*dir="${lang === "ar" ? "rtl" : "ltr"}"`));
            expect(html).toContain(title);
            expect(html).toContain(`rel="canonical" href="https://dz-legal-assistant-ai.onrender.com/${lang}/${path}/"`);
            expect(html).toContain(`hreflang="${lang === "ar" ? "en" : "ar-DZ"}" href="https://dz-legal-assistant-ai.onrender.com/${lang === "ar" ? "en" : "ar"}/${path}/"`);
            expect(html).toContain(`og:image" content="https://dz-legal-assistant-ai.onrender.com/og/${path}-${lang}.png"`);
            expect(html).not.toContain('name="robots"');
            noInlineScript(html);
        }
    }
    const sitemap = await text(await request.get("/sitemap-0.xml"));
    for (const path of ["download", "law", "privacy", "about"]) for (const lang of ["ar", "en"]) expect(sitemap).toContain(`<loc>https://dz-legal-assistant-ai.onrender.com/${lang}/${path}/</loc>`);
    expect(sitemap).not.toContain("/404/");
});

test("the law library page counts the data files and every official source it links opens", async ({ page, request }) => {
    await page.goto("/en/law/");
    // 1,361 articles from 6 texts, 2 of them complete: the figures of data/law/codes, never typed by hand.
    await expect(page.locator("h1")).toContainText("1,361 articles from 6 legal texts, 2 of them complete");
    await expect(page.locator("article.law")).toHaveCount(6);
    const links = await page.locator("article.law .links a").evaluateAll((as) => as.map((a) => a.getAttribute("href")!));
    expect(links.length).toBeGreaterThanOrEqual(6);
    for (const href of links) {
        if (href.startsWith("/law-sources/")) {
            const pdf = await request.get(href, { maxRedirects: 0 });
            expect(pdf.status(), href).toBe(200);
            expect(pdf.headers()["content-type"]).toContain("application/pdf");
        } else {
            expect(href, href).toMatch(/^https:\/\/www\.joradp\.dz\//);
        }
    }
    const arabic = await text(await request.get("/ar/law/"));
    expect(arabic).toContain("1,361 مادة من 6 نصوص قانونية، منها 2 كاملة");
    expect(arabic).toContain("21 نوعاً من الدعاوى");
});

test("the demo link skips the page transition, so Chrome logs no aborted-transition error", async ({ page }) => {
    const errors: string[] = [];
    page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); });
    page.on("pageerror", (error) => errors.push(error.message));
    await page.goto("/en/");
    await page.locator("header .nav a.nav-demo").click();
    await expect(page).toHaveURL(/\/app\?lang=en$/);
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    expect(errors.filter((e) => /transition/i.test(e)), errors.join("\n")).toHaveLength(0);
});

test("the demo's bar folds New, Open, Save and Guide behind More on a phone, and shows them on a desktop", async ({ page }) => {
    // On a desktop the guide is the tour card, with the AI steps tagged as the desktop tags them, and nothing is folded.
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator(".guide.tour")).toBeVisible();
    await expect(page.locator(".guide .tag.ai")).toHaveCount(2);
    const more = page.locator(".appbar .more-toggle");
    await expect(more).toBeHidden();
    await expect(page.getByRole("button", { name: "New" })).toBeVisible();
    // On a phone the bar keeps the sample picker, the theme and the language; the rest waits behind More.
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(more).toBeVisible();
    await expect(page.getByRole("button", { name: "New" })).toBeHidden();
    await more.click();
    await expect(page.getByRole("button", { name: "New" })).toBeVisible();
    await expect(more).toHaveAttribute("aria-expanded", "true");
});
