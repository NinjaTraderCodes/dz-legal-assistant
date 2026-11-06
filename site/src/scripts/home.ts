// The home page loads its scroll story only where it runs: wide screens without reduced motion. Phones and
// reduced-motion readers never download the animation code and see the finished sheet.
const wants = window.matchMedia("(prefers-reduced-motion: no-preference) and (min-width: 900px)");
if (wants.matches && document.querySelector("[data-story]")) {
    import("./story")
        .then((story) => { story.start(); return document.fonts?.ready; })
        .then(() => requestAnimationFrame(() => requestAnimationFrame(settleHash)))
        .catch(() => { /* the page already shows the final state */ });
}

/**
 * A visitor who arrives with a section's address (/en/#how, the bar's "How it works" from another page) is taken there
 * once the story has pinned and measured itself: the browser's smooth jump would otherwise be cut short while the page
 * rearranges, leaving them at the top. Only if they have not started scrolling themselves.
 */
function settleHash(): void {
    const id = decodeURIComponent(location.hash.slice(1));
    const target = id ? document.getElementById(id) : null;
    if (!target || window.scrollY > 40) return;
    const html = document.documentElement;
    html.style.scrollBehavior = "auto";
    target.scrollIntoView({ block: "start" });
    html.style.scrollBehavior = "";
}

// The guide reel of folder 05: a few lines, loaded with the page.
import { startReels } from "./reel";
startReels();
