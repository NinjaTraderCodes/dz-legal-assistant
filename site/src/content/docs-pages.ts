// The six documents of docs/ that the site renders as its own pages (/{lang}/docs/<slug>/), and the Sätteri hast
// plugin that adapts their HTML: a link to one of the six goes to its page, any other link into the repository goes
// to GitHub, external links are left alone, images get responsive widths, code blocks become keyboard-scrollable,
// tables get a scrolling wrapper, and the document's H1 becomes the page title. Nothing else is rendered.
import { blob } from "./repo";

export const docNames = ["PRODUCT-DETAILS", "ARCHITECTURE", "OFFLINE", "LAW-LIBRARY", "HIDDEN-SET", "WEB-DEMO"] as const;

export const slugOf = (name: string) => name.toLowerCase();

/** The repository path of a document that has a page here, e.g. "docs/OFFLINE.md". */
export const isDocPage = (repoPath: string) => docNames.some((name) => repoPath === `docs/${name}.md`);

/** The page of a document, without the language prefix. */
export const docPath = (repoPath: string) => `/docs/${slugOf(repoPath.slice("docs/".length, -".md".length))}/`;

interface Node { tagName: string; properties?: Record<string, unknown> }
interface Ctx {
    fileURL: URL | undefined;
    data: { astro?: { frontmatter: Record<string, unknown> } };
    setProperty(node: Node, key: string, value: unknown): void;
    textContent(node: Node): string;
    removeNode(node: Node): void;
    wrapNode(node: Node, parent: { type: "element"; tagName: string; properties: Record<string, unknown>; children: never[] }): void;
}

export function docsHastPlugin() {
    return {
        name: "docs-pages",
        element: {
            filter: ["a", "img", "pre", "table", "h1"],
            visit(node: Node, ctx: Ctx): void {
                // Only the six documents; another Markdown file would keep its links as written.
                const file = ctx.fileURL?.pathname.split("/").pop() ?? "";
                if (!docNames.some((name) => file === `${name}.md`)) return;
                const props = node.properties ?? {};
                switch (node.tagName) {
                    case "a": {
                        const href = typeof props.href === "string" ? props.href : "";
                        if (!href || href.startsWith("#")) return;
                        if (/^[a-z][a-z0-9+.-]*:/i.test(href)) { ctx.setProperty(node, "rel", "noopener"); return; }
                        // Written relative to docs/: "ARCHITECTURE.md", "./X.md", "../README.md", "../src/…".
                        const target = new URL(href, "https://repository.invalid/docs/");
                        const repoPath = decodeURIComponent(target.pathname.slice(1));
                        if (isDocPage(repoPath)) {
                            // Language-neutral, so one rendering serves /ar/ and /en/: the pages are siblings under docs/.
                            ctx.setProperty(node, "href", `../${slugOf(repoPath.slice("docs/".length, -".md".length))}/${target.hash}`);
                        } else {
                            ctx.setProperty(node, "href", blob(repoPath) + target.hash);
                            ctx.setProperty(node, "rel", "noopener");
                        }
                        return;
                    }
                    case "img":
                        ctx.setProperty(node, "widths", [720, 1440]);
                        ctx.setProperty(node, "sizes", "(min-width: 900px) 720px, calc(100vw - 32px)");
                        return;
                    case "pre":
                        ctx.setProperty(node, "tabIndex", 0);
                        return;
                    case "table":
                        ctx.wrapNode(node, { type: "element", tagName: "div", properties: { className: ["table-wrap"] }, children: [] });
                        return;
                    case "h1":
                        if (ctx.data.astro) ctx.data.astro.frontmatter.title = ctx.textContent(node);
                        ctx.removeNode(node);
                        return;
                }
            },
        },
    };
}
