// Small behaviours of the site, loaded at the end of the page: the theme toggle, remembered in a first-party
// cookie and localStorage, and the loading shell of the demo link. No framework, no network calls.
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
  document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
    label(button, current());
    button.addEventListener("click", function () {
      var next = current() === "dark" ? "light" : "dark";
      if (next === "dark") document.documentElement.setAttribute("data-theme", "dark");
      else document.documentElement.removeAttribute("data-theme");
      remember("dzlegal.theme", next);
      document.querySelectorAll("[data-theme-toggle]").forEach(function (b) { label(b, next); });
    });
  });
  // The language links set the cookie on the server (/lang/{code}); nothing to do here.
})();
