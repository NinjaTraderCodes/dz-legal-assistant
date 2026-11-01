// The scroll story of the home page: as the reader scrolls past the nine steps, each step ticks and its section
// is written on the paper; after the defendant, the domicile line is taken away and the formal check drops to 7 of 9
// with the article that requires it, then it comes back. Waiting sections are clipped, so the sheet fills from the top.
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";

export function start(): void {
    gsap.registerPlugin(ScrollTrigger);
    const mm = gsap.matchMedia();
    mm.add("(prefers-reduced-motion: no-preference) and (min-width: 900px)", () => {
        const story = document.querySelector<HTMLElement>("[data-story]");
        if (!story) return;
        const steps = Array.from(story.querySelectorAll<HTMLElement>("[data-step]"));
        const sections = Array.from(story.querySelectorAll<HTMLElement>("[data-paper-section]"));
        const domicile = story.querySelector<HTMLElement>("[data-domicile]");
        const score = story.querySelector<HTMLElement>("[data-score]");
        const bar = story.querySelector<HTMLElement>("[data-bar]");
        const present = story.querySelector<HTMLElement>("[data-present]");
        const missing = story.querySelector<HTMLElement>("[data-missing]");
        const finding = story.querySelector<HTMLElement>("[data-finding]");
        const check = present?.closest<HTMLElement>(".check");
        if (steps.length !== sections.length || !score || !bar || !present || !missing || !finding || !check) return;

        // "ko" shows the red verdict and the finding; "ok" the green verdict; neither while the form is being filled.
        const setScore = (n: number, verdict: "ok" | "ko" | "none") => {
            score.textContent = String(n);
            bar.style.width = `${Math.round((n / 9) * 100)}%`;
            check.classList.toggle("ko", verdict === "ko");
            present.hidden = verdict !== "ok";
            missing.hidden = verdict !== "ko";
            finding.hidden = verdict !== "ko";
        };
        const mark = (upto: number, current: number) => {
            steps.forEach((step, i) => {
                step.classList.toggle("done", i < upto);
                step.classList.toggle("current", i === current);
            });
        };

        // Waiting sections are clipped away (not faded): hidden text is not read as low-contrast text, and the sheet
        // writes itself from the top as the reader scrolls.
        gsap.set(sections.slice(1), { clipPath: "inset(0 0 100% 0)" });
        mark(1, 0);
        setScore(1, "none");

        const tl = gsap.timeline({
            scrollTrigger: { trigger: story.querySelector(".stage"), start: "top top+=96", end: () => "+=" + (steps.length + 1) * 190, pin: true, scrub: 0.4, anticipatePin: 1 },
        });
        steps.forEach((_, i) => {
            if (i === 0) return;
            tl.call(() => { mark(i + 1, i); setScore(i + 1, "none"); }, [], i);
            tl.to(sections[i], { clipPath: "inset(0 0 0% 0)", duration: 0.7, ease: "power2.out" }, i);
            if (i === 2 && domicile) {
                // The signature moment: the domicile goes, the check drops and names article 15-3; then it is restored.
                tl.call(() => { setScore(i, "ko"); }, [], i + 0.55);
                tl.to(domicile, { opacity: 0, duration: 0.25 }, i + 0.55);
                tl.call(() => { setScore(i + 1, "none"); }, [], i + 0.9);
                tl.to(domicile, { opacity: 1, duration: 0.25 }, i + 0.9);
            }
        });
        tl.call(() => { mark(steps.length, -1); setScore(9, "ok"); }, [], steps.length + 0.4);

        return () => {
            tl.scrollTrigger?.kill();
            tl.kill();
            gsap.set(sections, { clearProps: "clipPath" });
            if (domicile) gsap.set(domicile, { clearProps: "opacity" });
            mark(steps.length, -1);
            setScore(9, "ok");
        };
    });
}
