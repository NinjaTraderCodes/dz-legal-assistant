// The home page loads its scroll story only where it runs: wide screens without reduced motion. Phones and
// reduced-motion readers never download the animation code and see the finished sheet.
const wants = window.matchMedia("(prefers-reduced-motion: no-preference) and (min-width: 900px)");
if (wants.matches && document.querySelector("[data-story]")) {
    import("./story").then((story) => story.start()).catch(() => { /* the page already shows the final state */ });
}

// The guide reel of folder 05: a few lines, loaded with the page.
import { startReels } from "./reel";
startReels();
