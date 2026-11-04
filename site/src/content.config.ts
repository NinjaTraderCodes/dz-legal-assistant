// The site's content collections: the six documents of the repository's docs/ folder, read from outside the site
// (vite.server.fs.allow lets the dev server read them; the Docker site stage copies exactly these files), and the short
// summary the site writes for each of them, one file per document and language (src/content/docs-summaries/).
import { defineCollection } from "astro:content";
import { glob } from "astro/loaders";
import { z } from "astro/zod";
import { docNames, slugOf } from "./content/docs-pages";

export const collections = {
    docs: defineCollection({
        loader: glob({ base: "../docs", pattern: docNames.map((name) => `${name}.md`), generateId: ({ entry }) => slugOf(entry.replace(/\.md$/, "")) }),
    }),
    // "<slug>.<lang>.md": the key points as a list, each taken from a sentence of the document, and the one line for a lawyer.
    docSummaries: defineCollection({
        loader: glob({ base: "./src/content/docs-summaries", pattern: "*.md", generateId: ({ entry }) => entry.replace(/\.md$/, "") }),
        schema: z.object({ lawyer: z.string().min(1) }),
    }),
};
