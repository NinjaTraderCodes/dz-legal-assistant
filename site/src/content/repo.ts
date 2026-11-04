// Where the repository lives, for every link the site makes into it. Kept apart from product.ts so the Astro
// config (which renders the documents) can import it without the site's strings.
export const repoUrl = "https://github.com/NinjaTraderCodes/dz-legal-assistant";
/** A file of the repository at its default branch, for the "source" lines under every figure. */
export const blob = (path: string) => `${repoUrl}/blob/HEAD/${path}`;
