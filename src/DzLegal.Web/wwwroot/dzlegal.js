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
  isNarrow() {
    try { return window.matchMedia("(max-width: 899px)").matches; } catch { return false; }
  },
  prefersDark() {
    try { return window.matchMedia("(prefers-color-scheme: dark)").matches; } catch { return false; }
  },
  forgetCase() {
    try { localStorage.removeItem("dzlegal.case"); } catch { }
  },
};
