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

test("the hero shows the app on a laptop: one picture for both languages, AVIF with a WebP fallback, beside the text on a laptop screen and under it on a phone", async ({ page, request }) => {
    const files: string[] = [];
    for (const lang of ["ar", "en"]) {
        const html = await text(await request.get(`/${lang}/`));
        const picture = html.match(/<div class="device"[^>]*>\s*<picture[^>]*>([\s\S]*?)<\/picture>/)?.[1] ?? "";
        expect(picture).toMatch(/<source srcset="\/_astro\/laptop[^"]*\.avif 480w, [^"]*720w, [^"]*960w, [^"]*1440w" type="image\/avif"/);
        expect(picture).toMatch(/<img src="\/_astro\/laptop[^"]*\.webp" srcset="\/_astro\/laptop[^"]*\.webp 480w, [^"]*720w, [^"]*960w, [^"]*1440w"/);
        // Eager but behind the photograph and the fonts: the laptop is on the first screen and is not its largest element.
        expect(picture).toMatch(/loading="eager"/);
        expect(picture).toMatch(/fetchpriority="low"/);
        expect(picture).toMatch(/width="1440" height="960"/);
        expect(picture).toContain(lang === "ar" ? 'alt="التطبيق المكتبي على حاسوب محمول' : 'alt="The desktop app on a laptop');
        expect(html).toMatch(/<link rel="preload" as="image" href="\/_astro\/welcome-bg[^"]*"/);
        expect(html).not.toMatch(/<link rel="preload" as="image" href="\/_astro\/laptop/);
        files.push(picture.match(/<img src="([^"]+)"/)?.[1] ?? "");
    }
    expect(files[0]).toBe(files[1]);
    // Beside the text from 900 px, on the side the writing direction leaves free; below the buttons on a phone.
    for (const lang of ["ar", "en"]) {
        await page.setViewportSize({ width: 1366, height: 768 });
        await page.goto(`/${lang}/`);
        const laptop = await page.locator(".hero .laptop").boundingBox();
        const title = await page.locator("#hero-title").boundingBox();
        expect(laptop && title && Math.abs(laptop.y + laptop.height / 2 - (title.y + title.height / 2)) < 400).toBe(true);
        if (lang === "en") expect(laptop!.x).toBeGreaterThan(title!.x + title!.width);
        else expect(laptop!.x + laptop!.width).toBeLessThan(title!.x);
        await page.setViewportSize({ width: 390, height: 844 });
        const phoneLaptop = await page.locator(".hero .laptop").boundingBox();
        const cta = await page.locator(".hero .btn-gold").boundingBox();
        expect(phoneLaptop!.y).toBeGreaterThan(cta!.y + cta!.height);
    }
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

test("the six documents are pages in both languages, with their links adapted and their pictures optimised", async ({ page, request }) => {
    const slugs = ["product-details", "architecture", "offline", "law-library", "hidden-set", "web-demo"];
    for (const slug of slugs) {
        for (const lang of ["ar", "en"]) {
            const response = await request.get(`/${lang}/docs/${slug}/`);
            expect(response.status(), `${lang}/docs/${slug}`).toBe(200);
            csp(response);
            const html = await text(response);
            expect(html).toMatch(new RegExp(`<html[^>]*lang="${lang}"[^>]*dir="${lang === "ar" ? "rtl" : "ltr"}"`));
            // The document is English whatever the page's language, and the Arabic page says so.
            expect(html).toContain('class="section page layout" lang="en" dir="ltr"');
            if (lang === "ar") expect(html).toContain("هذه الوثيقة متاحة بالإنجليزية فقط");
            expect(html).toContain(`rel="canonical" href="https://dz-legal-assistant-ai.onrender.com/${lang}/docs/${slug}/"`);
            expect(html).toContain(`hreflang="${lang === "ar" ? "en" : "ar-DZ"}" href="https://dz-legal-assistant-ai.onrender.com/${lang === "ar" ? "en" : "ar"}/docs/${slug}/"`);
            expect(html).toContain(`og:image" content="https://dz-legal-assistant-ai.onrender.com/og/docs-${lang}.png"`);
            expect(html).toContain('<nav class="toc"');
            expect(html).not.toContain('name="robots"');
            noInlineScript(html);
            // One H1 only: the document's own is lifted into the page title.
            expect(html.match(/<h1/g)?.length).toBe(1);
        }
    }
    // Links: a sibling document goes to its page; any other file of the repository is named as text (the site
    // never links into the repository); outside links stay.
    const details = await text(await request.get("/en/docs/product-details/"));
    expect(details).toContain('href="../law-library/"');
    expect(details).not.toContain("github.com/NinjaTraderCodes/dz-legal-assistant/blob");
    expect(details).toContain("ONLINE-TESTING.md");
    expect(details).toContain("README");
    expect(details).toContain('href="https://ollama.com"');
    expect(details.match(/<img /g)?.length).toBe(9);
    expect(details.match(/loading="lazy"/g)?.length).toBe(9);
    expect(details).toMatch(/src="\/_astro\/main-window-ar\.[^"]+\.webp"/);
    expect(details.match(/<pre[^>]*tabindex="0"/g)?.length).toBeGreaterThanOrEqual(5);
    expect(details).toContain('class="table-wrap"');
    expect(details).toContain("<title>Product details — DZ Legal Assistant</title>");
    // The sibling link lands on the sibling page.
    await page.goto("/en/docs/product-details/");
    await page.locator('article.body a[href="../law-library/"]').first().click();
    await expect(page).toHaveURL(/\/en\/docs\/law-library\/$/);
    await expect(page.locator("h1")).toHaveText(/Law library/);
    const sitemap = await text(await request.get("/sitemap-0.xml"));
    for (const slug of slugs) for (const lang of ["ar", "en"]) expect(sitemap).toContain(`<loc>https://dz-legal-assistant-ai.onrender.com/${lang}/docs/${slug}/</loc>`);
});

test("the guide page shows a picture of every step, opens one larger in a native dialog and closes it with Escape", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto("/en/try/");
    await expect(page.locator(".try-step")).toHaveCount(5);
    // Every step carries at least one lazy picture in both themes; only the version of the theme shown has a box.
    // Steps 1, 3 and 4 carry real captures (a light and a dark file each); steps 2, 4 and 5 carry pieces of the interface
    // drawn in HTML, captioned "as the app shows it", never as screenshots.
    for (const [i, kind] of [[0, "img"], [1, "piece"], [2, "img"], [3, "both"], [4, "piece"]] as const) {
        const step = page.locator(".try-step").nth(i);
        if (kind !== "piece") expect(await step.locator("img").count()).toBeGreaterThanOrEqual(2);
        if (kind !== "img") expect(await step.locator(".piece-frame").count()).toBeGreaterThanOrEqual(1);
        if (kind === "piece") expect(await step.locator("img").count()).toBe(0);
    }
    for (const text of await page.locator(".piece figcaption").allTextContents()) expect(text).not.toMatch(/screenshot|لقطة/);
    const dark = page.locator(".try-step").first().locator("img.shot-dark").first();
    const light = page.locator(".try-step").first().locator("img.shot-light").first();
    await expect(dark).toBeVisible();
    await expect(light).toBeHidden();
    const dialog = page.locator("dialog[data-lightbox-dialog]");
    await expect(dialog).toBeHidden();
    await page.locator("button[data-lightbox]").first().click();
    await expect(dialog).toBeVisible();
    await expect(dialog.locator("img")).toHaveAttribute("src", /\/_astro\/step1-sample-en-dark[^"]*\.webp/);
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(page.locator("button[data-lightbox]").first()).toBeFocused();
    // The steps reveal as they scroll into view when motion is welcome, and stand visible without it.
    await expect(page.locator("html")).toHaveAttribute("data-motion", "");
    await page.locator(".try-step").last().scrollIntoViewIfNeeded();
    await expect(page.locator(".try-step").last()).toHaveClass(/in/);
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.reload();
    await expect(page.locator("html")).not.toHaveAttribute("data-motion", "");
    await expect(page.locator(".try-step").last()).toBeVisible();
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
    // On a desktop the guide waits behind its invitation; opened, it is the tour card with the AI steps tagged as the
    // desktop tags them; nothing in the bar is folded.
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto("/app?lang=en");
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator(".guide.tour")).toHaveCount(0);
    await expect(page.locator(".invite")).toBeVisible();
    await page.locator(".invite").getByRole("button", { name: "Open the guide" }).click();
    await expect(page.locator(".guide.tour")).toBeVisible();
    await expect(page.locator(".guide .tag.ai")).toHaveCount(2);
    await page.locator(".guide.tour").getByRole("button", { name: "Hide" }).click();
    await expect(page.locator(".guide.tour")).toHaveCount(0);
    // The one-line disclosure stays on screen; "Details" opens the full wording and the browser-copy button.
    const disclosure = page.locator(".disclosure");
    await expect(disclosure.locator("summary")).toContainText("fictitious cases only");
    await expect(page.getByRole("button", { name: "Clear this browser's copy" })).toBeHidden();
    await disclosure.locator("summary").click();
    await expect(page.getByRole("button", { name: "Clear this browser's copy" })).toBeVisible();
    await expect(disclosure).toContainText("hosted models");
    // Dismissing the invitation is remembered for the visit.
    await page.locator(".invite").getByRole("button", { name: "Hide" }).click();
    await expect(page.locator(".invite")).toHaveCount(0);
    await page.reload();
    await expect(page.locator(".app")).toBeVisible({ timeout: 30_000 });
    await expect(page.locator(".invite")).toHaveCount(0);
    await expect(page.locator(".disclosure")).toBeVisible();
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
