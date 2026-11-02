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
  // Remembered for this visit only (the tab): the dismissed invitation to the guide.
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
  // The shell shown before the circuit connects; the page has rendered once this is called.
  hideLoading() {
    const shell = document.getElementById("app-loading");
    if (shell) shell.remove();
  },
};
