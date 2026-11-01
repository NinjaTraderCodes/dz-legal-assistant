// Playwright runs against the ASP.NET Core app serving the built site (dotnet run --launch-profile http+site),
// so the tests see exactly what Render serves: the site, the demo at /app, the language switch and the headers.
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
    testDir: "./tests",
    timeout: 60_000,
    retries: 0,
    reporter: [["list"], ["html", { open: "never", outputFolder: "playwright-report" }]],
    use: {
        baseURL: process.env.BASE_URL ?? "http://localhost:5080",
        trace: "retain-on-failure",
        locale: "en-GB",
    },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
