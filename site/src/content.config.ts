// The site's content collections: the six documents of the repository's docs/ folder, read from outside the site
// (vite.server.fs.allow lets the dev server read them; the Docker site stage copies exactly these files).
import { defineCollection } from "astro:content";
import { glob } from "astro/loaders";
import { docNames, slugOf } from "./content/docs-pages";

export const collections = {
    docs: defineCollection({
        loader: glob({ base: "../docs", pattern: docNames.map((name) => `${name}.md`), generateId: ({ entry }) => slugOf(entry.replace(/\.md$/, "")) }),
    }),
};
