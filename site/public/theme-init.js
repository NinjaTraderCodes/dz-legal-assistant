// Applies the remembered theme before the first paint (external file: the server's CSP allows no inline scripts).
// Dark is the default and is already on <html>; only a remembered "light" changes it. The system preference is never
// consulted. The same keys are read by the web demo at /app.
(function () {
  // The petition's faces wait until the page has loaded (world.css, data-faces; ui.js lifts it).
  document.documentElement.setAttribute("data-faces", "later");
  try {
    var m = document.cookie.match(/(?:^|; )dzlegal\.theme=(light|dark)/);
    var theme = m ? m[1] : null;
    if (!theme) { try { theme = localStorage.getItem("dzlegal.theme"); } catch (e) { theme = null; } }
    if (theme === "light") {
      document.documentElement.removeAttribute("data-theme");
      // The browser frame's colour follows; both values sit on the tag, so no style has to be computed here.
      var meta = document.querySelector('meta[name="theme-color"]');
      if (meta && meta.getAttribute("data-light")) meta.setAttribute("content", meta.getAttribute("data-light"));
    }
  } catch (e) { /* no preference, dark stays */ }
})();
