// Browser helpers for the demo: downloads and the clipboard. Nothing here talks to any server.
window.dzlegal = {
  async download(name, streamRef) {
    const blob = new Blob([await streamRef.arrayBuffer()]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { a.remove(); URL.revokeObjectURL(url); }, 10000);
  },
  async copyText(text) {
    await navigator.clipboard.writeText(text);
  },
  // The visitor's case, kept in this browser only, so a server restart or a closed tab does not lose it.
  // Storage can be unavailable (private window, blocked site data): then nothing is kept and nothing breaks.
  saveCase(json) {
    try { localStorage.setItem("dzlegal.case", JSON.stringify({ savedAt: new Date().toISOString(), json })); } catch { }
  },
  // A copy older than a day is deleted unread: a shared or public computer should not keep a case.
  loadCase() {
    try {
      const stored = localStorage.getItem("dzlegal.case");
      if (stored && Date.now() - Date.parse(JSON.parse(stored).savedAt) > 24 * 3600 * 1000) {
        localStorage.removeItem("dzlegal.case");
        return null;
      }
      return stored;
    } catch { return null; }
  },
  // Remembered for this visit only (the tab): the dismissed invitation to the guide, and the guide's step.
  sessionGet(key) {
    try { return sessionStorage.getItem(key); } catch { return null; }
  },
  sessionSet(key, value) {
    try { sessionStorage.setItem(key, value); } catch { }
  },
  prefersDark() {
    try { return window.matchMedia("(prefers-color-scheme: dark)").matches; } catch { return false; }
  },
  forgetCase() {
    try { localStorage.removeItem("dzlegal.case"); } catch { }
  },
  // The visitor's language and theme, shared with the website: a first-party cookie the server reads, and localStorage.
  remember(key, value) {
    try { document.cookie = key + "=" + value + "; Path=/; Max-Age=31536000; SameSite=Lax" + (location.protocol === "https:" ? "; Secure" : ""); } catch { }
    try { localStorage.setItem(key, value); } catch { }
  },
  applyChoice(lang, dir, theme) {
    const html = document.documentElement;
    html.lang = lang;
    html.dir = dir;
    if (theme === "dark") html.setAttribute("data-theme", "dark"); else html.removeAttribute("data-theme");
    this.remember("dzlegal.lang", lang);
    this.remember("dzlegal.theme", theme);
  },
  // The guide's light. While the guide is open the window is dimmed, except a hole around what it points at (never
  // clicked for the visitor, and the dim lets every click through), and its card stands beside that, or in the middle
  // when there is nothing to point at. A frame loop follows the target as panels scroll and re-render. On a phone, a
  // target in a panel that is not shown lights that panel's tab, and the card stays at the bottom of the screen.
  point(selector, open) {
    const g = this._guide ??= { lit: [], frame: 0, spot: null, card: "" };
    if (!open) { this._unpoint(); return; }
    g.selector = selector || null;
    g.scroll = true;
    if (!g.spot) {
      g.spot = document.createElement("div");
      g.spot.className = "guide-spot";
      g.spot.setAttribute("aria-hidden", "true");
      document.body.appendChild(g.spot);
    }
    if (!g.frame) {
      const tick = () => { g.frame = requestAnimationFrame(tick); this._follow(); };
      g.frame = requestAnimationFrame(tick);
    }
  },
  _unpoint() {
    const g = this._guide;
    if (!g) return;
    cancelAnimationFrame(g.frame);
    g.frame = 0;
    g.lit.forEach((el) => el.classList.remove("guide-ring"));
    g.lit = [];
    g.spot?.remove();
    g.spot = null;
    g.card = "";
  },
  _targets() {
    const g = this._guide;
    let found = [];
    try { found = g.selector ? [...document.querySelectorAll(g.selector)] : []; } catch { }
    const panels = [".form-panel", ".draft-panel", ".check-panel"];
    return [...new Set(found.map((el) => {
      if (el.getClientRects().length > 0) return el;
      const index = panels.findIndex((p) => el.closest(p));
      const tab = index < 0 ? null : document.querySelectorAll(".mobile-tabs [role=tab]")[index];
      return tab && tab.getClientRects().length > 0 ? tab : null;
    }).filter(Boolean))];
  },
  // The part of an element the visitor can see: its box cut by every scrolling ancestor and by the window.
  _visible(el) {
    const r = el.getBoundingClientRect();
    let top = r.top, left = r.left, bottom = r.bottom, right = r.right;
    for (let a = el.parentElement; a && a !== document.body; a = a.parentElement) {
      if (getComputedStyle(a).overflow === "visible") continue;
      const b = a.getBoundingClientRect();
      top = Math.max(top, b.top); left = Math.max(left, b.left); bottom = Math.min(bottom, b.bottom); right = Math.min(right, b.right);
    }
    top = Math.max(top, 0); left = Math.max(left, 0); bottom = Math.min(bottom, innerHeight); right = Math.min(right, innerWidth);
    return bottom - top > 2 && right - left > 2 ? { top, left, bottom, right, full: (bottom - top) * (right - left) >= 0.9 * r.width * r.height } : null;
  },
  _follow() {
    const g = this._guide;
    const targets = this._targets();
    const fromTab = targets.length > 0 && g.lit.some((el) => el.closest(".mobile-tabs")) && !targets.some((el) => el.closest(".mobile-tabs"));
    g.lit.forEach((el) => { if (!targets.includes(el)) el.classList.remove("guide-ring"); });
    targets.forEach((el) => { if (!el.classList.contains("guide-ring")) el.classList.add("guide-ring"); });
    g.lit = targets;
    // A new target (or the target behind a tab just opened) is brought into view once, if it is out of sight.
    if ((g.scroll || fromTab) && targets.length > 0) {
      g.scroll = false;
      const seen = this._visible(targets[0]);
      if (!seen || !seen.full) {
        const smooth = !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        targets[0].scrollIntoView({ block: "center", inline: "nearest", behavior: smooth ? "smooth" : "auto" });
      }
    }
    const boxes = targets.map((el) => this._visible(el)).filter(Boolean);
    const pad = 6;
    const hole = boxes.length === 0 ? null : {
      top: Math.min(...boxes.map((b) => b.top)) - pad, left: Math.min(...boxes.map((b) => b.left)) - pad,
      bottom: Math.max(...boxes.map((b) => b.bottom)) + pad, right: Math.max(...boxes.map((b) => b.right)) + pad,
    };
    const spot = g.spot;
    spot.classList.toggle("empty", !hole);
    // The section assistant is a drawer of its own, over the work: the guide does not dim it.
    spot.hidden = !!document.querySelector(".assistant");
    const h = hole ?? { top: innerHeight / 2, left: innerWidth / 2, bottom: innerHeight / 2, right: innerWidth / 2 };
    const css = `top:${h.top}px;left:${h.left}px;width:${h.right - h.left}px;height:${h.bottom - h.top}px`;
    if (spot.style.cssText !== css) spot.style.cssText = css;
    this._place(hole);
  },
  _place(hole) {
    const g = this._guide;
    const card = document.querySelector(".guide-card");
    if (!card) return;
    if (innerWidth < 900) {
      if (g.card) { card.classList.remove("placed"); card.style.left = card.style.top = ""; g.card = ""; }
      return;
    }
    const w = card.offsetWidth, h = card.offsetHeight, m = 12, gap = 12, vw = innerWidth, vh = innerHeight;
    const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), Math.max(lo, hi));
    let spot;
    if (!hole) {
      spot = { left: (vw - w) / 2, top: (vh - h) / 2 };
    } else {
      const rtl = (card.closest("[dir]")?.getAttribute("dir") ?? document.documentElement.dir) === "rtl";
      const along = clamp(rtl ? hole.right - w : hole.left, m, vw - w - m);
      const middle = clamp((hole.top + hole.bottom - h) / 2, m, vh - h - m);
      const startSide = rtl ? hole.right + gap : hole.left - gap - w;
      const endSide = rtl ? hole.left - gap - w : hole.right + gap;
      // Under the target first (the eye reads on), then over it, then beside it; the card never covers it when it fits.
      const options = [{ left: along, top: hole.bottom + gap }, { left: along, top: hole.top - gap - h }, { left: endSide, top: middle }, { left: startSide, top: middle }];
      spot = options.find((o) => o.top >= m && o.top + h <= vh - m && o.left >= m && o.left + w <= vw - m)
        ?? { left: along, top: vh - hole.bottom > hole.top ? clamp(hole.bottom + gap, m, vh - h - m) : clamp(hole.top - gap - h, m, vh - h - m) };
    }
    const key = `${Math.round(spot.left)},${Math.round(spot.top)}`;
    if (key === g.card && card.classList.contains("placed")) return;
    g.card = key;
    card.classList.add("placed");
    card.style.left = `${Math.round(spot.left)}px`;
    card.style.top = `${Math.round(spot.top)}px`;
  },
  // The shell shown before the circuit connects; the page has rendered once this is called.
  hideLoading() {
    const shell = document.getElementById("app-loading");
    if (shell) shell.remove();
  },
};
