// The scroll story of the home page: as the reader scrolls, each step ticks and its section is written on the paper,
// which slides inside its frame so the section being written stays in view. After the defendant, the domicile line is
// taken away and the formal check drops to 2 of 9 with the article that requires it, then it comes back. The form is
// written first (court to signature), the check reaches 8 of 9 and names the missing grounds, and the legal grounds
// come last: the code's quoted rules appear, the AI's lines write themselves, the review box is ticked, 9 of 9.
// Waiting sections are clipped, so the sheet fills from the top.
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
        const canvas = story.querySelector<HTMLElement>(".paper-canvas");
        const paper = story.querySelector<HTMLElement>(".paper");
        const domicile = story.querySelector<HTMLElement>("[data-domicile]");
        const score = story.querySelector<HTMLElement>("[data-score]");
        const bar = story.querySelector<HTMLElement>("[data-bar]");
        const present = story.querySelector<HTMLElement>("[data-present]");
        const missing = story.querySelector<HTMLElement>("[data-missing]");
        const findingDomicile = story.querySelector<HTMLElement>('[data-finding="domicile"]');
        const findingGrounds = story.querySelector<HTMLElement>('[data-finding="grounds"]');
        const check = present?.closest<HTMLElement>(".check");
        // The writing order, as the markup states it: indexes into the steps (and the sections, which match them).
        const order = (story.dataset.order ?? "").split(" ").map((id) => steps.findIndex((step) => step.dataset.step === id));
        if (steps.length !== sections.length || order.length !== steps.length || order.includes(-1) || !canvas || !paper
            || !score || !bar || !present || !missing || !findingDomicile || !findingGrounds || !check) return;
        const groundsIndex = order[order.length - 1];
        const grounds = sections[groundsIndex];
        const lines = Array.from(grounds.querySelectorAll<HTMLElement>("[data-rule], [data-ai]"));
        const chips = Array.from(grounds.querySelectorAll<HTMLElement>("[data-chip]"));
        const review = grounds.querySelector<HTMLElement>("[data-review]");

        // "ko" shows the red verdict and the named finding; "ok" the green verdict; neither while the form is being filled.
        const setScore = (n: number, verdict: "ok" | "ko" | "none", finding?: "domicile" | "grounds") => {
            score.textContent = String(n);
            bar.style.width = `${Math.round((n / 9) * 100)}%`;
            check.classList.toggle("ko", verdict === "ko");
            present.hidden = verdict !== "ok";
            missing.hidden = verdict !== "ko";
            findingDomicile.hidden = !(verdict === "ko" && finding === "domicile");
            findingGrounds.hidden = !(verdict === "ko" && finding === "grounds");
        };
        // The first `count` steps of the writing order are done; `current` is the step being written, or none.
        const mark = (count: number, current: number) => {
            const done = new Set(order.slice(0, count));
            steps.forEach((step, i) => {
                step.classList.toggle("done", done.has(i));
                step.classList.toggle("current", i === current);
            });
        };
        // Where the sheet must sit for a section to be centred in the frame, clamped to the sheet's own bounds; a section
        // taller than the frame shows its top. Read at tween time, so a resize or the font swap re-measures it.
        const yFor = (sec: HTMLElement) => () => {
            const frame = canvas.clientHeight;
            const pad = parseFloat(getComputedStyle(canvas).paddingTop) || 0;
            const lowest = Math.min(0, frame - paper.offsetHeight - 2 * pad);
            const wanted = sec.offsetHeight > frame - 2 * pad ? 4 - sec.offsetTop : frame / 2 - (pad + sec.offsetTop + sec.offsetHeight / 2);
            return gsap.utils.clamp(lowest, 0, wanted);
        };

        // Waiting sections are clipped away (not faded): hidden text is not read as low-contrast text, and the sheet
        // writes itself from the top as the reader scrolls. The grounds' lines are clipped from their end, so each one
        // sweeps in in reading order (right to left).
        story.classList.add("framed");
        gsap.set(sections.slice(1), { clipPath: "inset(0 0 100% 0)" });
        gsap.set(lines, { clipPath: "inset(0 0 0 100%)" });
        gsap.set(chips, { opacity: 0 });
        if (review) gsap.set(review, { opacity: 0, attr: { "data-done": 0 } });
        mark(1, order[0]);
        setScore(1, "none");

        const groundsUnits = 4; // the rules, the AI's lines one by one, the review box, the check
        const units = order.length - 1 + groundsUnits;
        // The stage pins just under the site's sticky header, whatever its height at this width; the frame's height
        // follows (StepStory.astro reads --story-top).
        const header = document.querySelector<HTMLElement>(".site-header");
        const topOffset = () => (header?.offsetHeight ?? 80) + 16;
        story.style.setProperty("--story-top", `${topOffset()}px`);
        const tl = gsap.timeline({
            scrollTrigger: {
                trigger: story.querySelector(".stage"), start: () => `top top+=${topOffset()}`, end: () => "+=" + (units + 1) * 190, pin: true, scrub: 0.4, anticipatePin: 1, invalidateOnRefresh: true,
                onRefreshInit: () => story.style.setProperty("--story-top", `${topOffset()}px`),
            },
        });
        order.forEach((index, k) => {
            if (k === 0) return;
            const sec = sections[index];
            const t = k;
            if (index === groundsIndex) {
                // The signature moment. The sheet slides back to the grounds; the section opens with its gold line and tag;
                // each rule sweeps in, quoted, with its "code" chip; the AI's lines write themselves one by one under it;
                // the review box appears and is ticked; the check turns green at 9 of 9.
                tl.call(() => { mark(k, index); setScore(k, "ko", "grounds"); }, [], t);
                tl.to(paper, { y: yFor(sec), duration: 0.8, ease: "power2.inOut" }, t);
                tl.to(sec, { clipPath: "inset(0 0 0% 0)", duration: 0.5, ease: "power2.out" }, t + 0.3);
                let at = t + 0.8;
                for (const line of lines) {
                    if (line.hasAttribute("data-rule")) {
                        tl.to(line, { clipPath: "inset(0 0 0 0%)", duration: 0.4, ease: "none" }, at);
                        const chip = line.querySelector<HTMLElement>("[data-chip]");
                        if (chip) tl.to(chip, { opacity: 1, duration: 0.15 }, at + 0.4);
                        at += 0.55;
                    } else {
                        tl.set(line, { attr: { "data-writing": 1 } }, at)
                            .to(line, { clipPath: "inset(0 0 0 0%)", duration: 0.28, ease: "none" }, at)
                            .set(line, { attr: { "data-writing": 0 } }, at + 0.28);
                        at += 0.3;
                    }
                }
                if (review) {
                    tl.to(review, { opacity: 1, duration: 0.2 }, at + 0.1);
                    tl.set(review, { attr: { "data-done": 1 } }, at + 0.45);
                }
                tl.call(() => { mark(order.length, -1); setScore(9, "ok"); }, [], at + 0.55);
                return;
            }
            tl.call(() => { mark(k + 1, index); setScore(k + 1, "none"); }, [], t);
            tl.to(paper, { y: yFor(sec), duration: 0.6, ease: "power2.inOut" }, t);
            tl.to(sec, { clipPath: "inset(0 0 0% 0)", duration: 0.7, ease: "power2.out" }, t);
            if (steps[index].dataset.step === "defendant" && domicile) {
                // The domicile goes, the check drops and names article 15-3; then it is restored.
                tl.call(() => { setScore(k, "ko", "domicile"); }, [], t + 0.55);
                tl.to(domicile, { opacity: 0, duration: 0.25 }, t + 0.55);
                tl.call(() => { setScore(k + 1, "none"); }, [], t + 0.9);
                tl.to(domicile, { opacity: 1, duration: 0.25 }, t + 0.9);
            }
            if (k === order.length - 2) {
                // The form is complete and the grounds are still to be written: the check says so, as the app does.
                tl.call(() => { setScore(k + 1, "ko", "grounds"); }, [], t + 0.75);
            }
        });
        // The timeline is exactly `units` long, so one unit is the same scroll distance everywhere and the final state
        // holds over the last stretch of the pin.
        tl.to({}, { duration: Math.max(0.01, units - tl.duration()) });
        // The sheet's height changes when the document font arrives: the pin and the positions are measured again.
        document.fonts?.ready.then(() => ScrollTrigger.refresh());

        return () => {
            tl.scrollTrigger?.kill();
            tl.kill();
            gsap.set([paper, ...sections, ...lines, ...chips], { clearProps: "all" });
            if (domicile) gsap.set(domicile, { clearProps: "opacity" });
            if (review) { gsap.set(review, { clearProps: "opacity" }); review.setAttribute("data-done", "1"); }
            lines.forEach((line) => line.removeAttribute("data-writing"));
            story.classList.remove("framed");
            mark(order.length, -1);
            setScore(9, "ok");
        };
    });
}
