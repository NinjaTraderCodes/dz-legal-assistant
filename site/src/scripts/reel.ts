// The guide reel of folder 05 (GuideReel.astro): five tabs and their screens. While the folder is on screen the reel
// moves on when the current tab's line has filled (a CSS animation, which hover and focus pause); a click, the arrow
// keys or the pause button stop it. With reduced motion it waits for the visitor and never moves by itself.
export function startReels(): void {
    for (const root of document.querySelectorAll<HTMLElement>("[data-reel]")) setUp(root);
}

function setUp(root: HTMLElement): void {
    const tabs = [...root.querySelectorAll<HTMLButtonElement>("[data-reel-tab]")];
    const panels = [...root.querySelectorAll<HTMLElement>("[data-reel-panel]")];
    const toggle = root.querySelector<HTMLButtonElement>("[data-reel-toggle]");
    if (tabs.length === 0 || tabs.length !== panels.length || !toggle) return;
    let current = 0;
    root.classList.add("live");

    const show = (index: number, focus = false) => {
        current = (index + tabs.length) % tabs.length;
        tabs.forEach((tab, k) => {
            tab.setAttribute("aria-selected", k === current ? "true" : "false");
            tab.tabIndex = k === current ? 0 : -1;
        });
        panels.forEach((panel, k) => panel.classList.toggle("current", k === current));
        if (focus) tabs[current].focus();
    };
    const play = (on: boolean) => {
        root.classList.toggle("playing", on);
        toggle.dataset.state = on ? "playing" : "paused";
    };

    toggle.hidden = false;
    toggle.addEventListener("click", () => play(!root.classList.contains("playing")));
    tabs.forEach((tab, k) => tab.addEventListener("click", () => { show(k); play(false); }));
    root.querySelector('[role="tablist"]')?.addEventListener("keydown", (event) => {
        const key = (event as KeyboardEvent).key;
        const rtl = getComputedStyle(root).direction === "rtl";
        const step = key === "ArrowDown" || key === (rtl ? "ArrowLeft" : "ArrowRight") ? 1 : key === "ArrowUp" || key === (rtl ? "ArrowRight" : "ArrowLeft") ? -1 : 0;
        const to = key === "Home" ? 0 : key === "End" ? tabs.length - 1 : step ? current + step : null;
        if (to === null) return;
        event.preventDefault();
        show(to, true);
        play(false);
    });
    // The current tab's line has filled: the next step.
    root.addEventListener("animationend", (event) => {
        if ((event as AnimationEvent).animationName.includes("reel-fill")) show(current + 1);
    });
    // Only while the folder is on screen, so nobody arrives at step 4 having missed the first three.
    new IntersectionObserver(([entry]) => root.classList.toggle("inview", entry.isIntersecting), { threshold: 0.35 }).observe(root);
    play(!window.matchMedia("(prefers-reduced-motion: reduce)").matches);
}
