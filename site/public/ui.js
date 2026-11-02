// Small behaviours of the site, loaded at the end of the page: the theme toggle, remembered in a first-party
// cookie and localStorage, and the hand-over to the demo. No framework, no network calls.
(function () {
  function remember(key, value) {
    try { document.cookie = key + "=" + value + "; Path=/; Max-Age=31536000; SameSite=Lax" + (location.protocol === "https:" ? "; Secure" : ""); } catch (e) { /* ignore */ }
    try { localStorage.setItem(key, value); } catch (e) { /* ignore */ }
  }
  function current() { return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light"; }
  function label(button, theme) {
    var dark = theme === "dark";
    button.setAttribute("aria-pressed", dark ? "true" : "false");
    var sun = button.querySelector("[data-icon=sun]"), moon = button.querySelector("[data-icon=moon]");
    if (sun) sun.hidden = !dark;
    if (moon) moon.hidden = dark;
    var text = button.getAttribute(dark ? "data-label-light" : "data-label-dark");
    if (text) button.setAttribute("aria-label", text), button.setAttribute("title", text);
  }
  // The browser's own frame follows the bar colour of the theme shown (both values sit on the tag, from the tokens).
  function tint(theme) {
    var meta = document.querySelector('meta[name="theme-color"]');
    var bar = meta && meta.getAttribute(theme === "dark" ? "data-dark" : "data-light");
    if (bar) meta.setAttribute("content", bar);
  }
  document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
    label(button, current());
    button.addEventListener("click", function () {
      var next = current() === "dark" ? "light" : "dark";
      if (next === "dark") document.documentElement.setAttribute("data-theme", "dark");
      else document.documentElement.removeAttribute("data-theme");
      remember("dzlegal.theme", next);
      document.querySelectorAll("[data-theme-toggle]").forEach(function (b) { label(b, next); });
      tint(next);
    });
  });
  // The site's pages cross-fade into each other (@view-transition in the stylesheet). The demo at /app is another
  // program that does not take part, and a transition started towards it would only be aborted with a console error:
  // it is skipped before the swap. Nothing to transition into on another origin either.
  window.addEventListener("pageswap", function (event) {
    if (!event.viewTransition) return;
    var entry = event.activation && event.activation.entry;
    var url = entry && entry.url;
    if (!url || /^\/app(?:[/?#]|$)/.test(new URL(url, location.href).pathname)) event.viewTransition.skipTransition();
  });
  // The language links set the cookie on the server (/lang/{code}); nothing to do here.
})();
