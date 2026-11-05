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
  // The guide's light: one element ringed, never clicked. A re-render can re-class or replace it, so the ring is put
  // back whenever the page changes. On a phone, a target in a panel that is not shown lights that panel's tab instead.
  // A new target is scrolled into view when it is out of sight (or under the guide's card).
  point(selector) {
    const g = this._guide ??= { selector: null, observer: null, lit: null, queued: false };
    g.selector = selector;
    if (!selector) {
      g.observer?.disconnect();
      g.observer = null;
      g.lit?.classList.remove("guide-ring");
      g.lit = null;
      return;
    }
    if (!g.observer) {
      g.observer = new MutationObserver(() => {
        if (g.queued) return;
        g.queued = true;
        requestAnimationFrame(() => { g.queued = false; this._ring(false); });
      });
      g.observer.observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ["class", "aria-selected"] });
    }
    requestAnimationFrame(() => this._ring(true));
  },
  _ring(scroll) {
    const g = this._guide;
    if (!g?.selector) return;
    let target = null;
    try { target = document.querySelector(g.selector); } catch { }
    if (target && target.getClientRects().length === 0) {
      const panels = [".form-panel", ".draft-panel", ".check-panel"];
      const index = panels.findIndex((p) => target.closest(p));
      const tab = index < 0 ? null : document.querySelectorAll(".mobile-tabs [role=tab]")[index];
      target = tab && tab.getClientRects().length > 0 ? tab : null;
    }
    // Once the visitor opens the tab that was lit, the target itself is brought into view.
    const fromTab = g.lit !== target && !!g.lit?.closest(".mobile-tabs");
    if (g.lit && g.lit !== target) g.lit.classList.remove("guide-ring");
    g.lit = target;
    if (!target) return;
    if (!target.classList.contains("guide-ring")) target.classList.add("guide-ring");
    if (!scroll && !fromTab) return;
    const r = target.getBoundingClientRect();
    const card = document.querySelector(".guide-card")?.getBoundingClientRect();
    // The card covers the bottom of the window only where it stands; elsewhere the window's own bottom counts.
    const under = card && r.right > card.left && r.left < card.right ? card.top : innerHeight;
    let hidden = r.top < 0 || r.bottom > under;
    const box = target.parentElement?.closest(".panel, .paper-scroll");
    if (box) { const b = box.getBoundingClientRect(); hidden ||= r.top < b.top || r.bottom > b.bottom; }
    if (hidden) {
      const smooth = !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      target.scrollIntoView({ block: "center", inline: "nearest", behavior: smooth ? "smooth" : "auto" });
    }
  },
  // The shell shown before the circuit connects; the page has rendered once this is called.
  hideLoading() {
    const shell = document.getElementById("app-loading");
    if (shell) shell.remove();
  },
};
