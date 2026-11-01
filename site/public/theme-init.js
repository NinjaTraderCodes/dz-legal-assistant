// Applies the remembered theme before the first paint (external file: the server's CSP allows no inline scripts).
// Light is the default; the system preference is never consulted. The same keys are read by the web demo at /app.
(function () {
  try {
    var m = document.cookie.match(/(?:^|; )dzlegal\.theme=(light|dark)/);
    var theme = m ? m[1] : null;
    if (!theme) { try { theme = localStorage.getItem("dzlegal.theme"); } catch (e) { theme = null; } }
    if (theme === "dark") document.documentElement.setAttribute("data-theme", "dark");
  } catch (e) { /* no preference, light stays */ }
})();
