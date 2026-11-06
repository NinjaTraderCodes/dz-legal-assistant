// Small behaviours of the site, loaded at the end of the page: the theme toggle, remembered in a first-party
// cookie and localStorage; the hand-over to the demo; the gentle reveal of steps as they scroll into view; the
// picture lightbox; and the folded documents that open on a link to one of their headings. No framework, no network calls.
(function () {
  // The petition's and the app's faces were held back for the first screen (theme-init.js, world.css): let them load.
  function faces() { setTimeout(function () { document.documentElement.removeAttribute("data-faces"); }, 0); }
  if (document.readyState === "complete") faces(); else window.addEventListener("load", faces);
  // The sticky bar's height, at this width, for scroll-padding-top (global.css).
  var bar = document.querySelector(".site-header");
  function barHeight() { if (bar) document.documentElement.style.setProperty("--header-h", bar.offsetHeight + "px"); }
  barHeight();
  window.addEventListener("resize", barHeight);
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

  // Steps marked data-reveal fade and slide in as they scroll into view, and the line joining them fills up to the
  // last one shown (--progress on the list). Only when motion is welcome: the attribute on <html> switches the
  // styles on, so without it, or without this script, everything is simply visible.
  // Stamps marked data-strike strike once as they come into view (.struck); the scroll story strikes its own.
  var motion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: no-preference)").matches && "IntersectionObserver" in window;
  var targets = document.querySelectorAll("[data-reveal], [data-strike]");
  if (motion && targets.length) {
    document.documentElement.setAttribute("data-motion", "");
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        entry.target.classList.add(entry.target.hasAttribute("data-strike") ? "struck" : "in");
        observer.unobserve(entry.target);
        var list = entry.target.parentElement;
        if (list && list.hasAttribute("data-progress")) {
          var items = list.querySelectorAll("[data-reveal]"), shown = list.querySelectorAll("[data-reveal].in").length;
          list.style.setProperty("--progress", Math.round((shown / items.length) * 100) + "%");
        }
      });
    }, { threshold: 0.2 });
    targets.forEach(function (target) { observer.observe(target); });
  }

  // A picture opens larger in the page's dialog: the version of the theme shown, at the size the button carries.
  var dialog = document.querySelector("[data-lightbox-dialog]");
  if (dialog && typeof dialog.showModal === "function") {
    var image = dialog.querySelector("[data-lightbox-image]"), caption = dialog.querySelector("[data-lightbox-caption]");
    document.querySelectorAll("[data-lightbox]").forEach(function (button) {
      button.addEventListener("click", function () {
        var shown = Array.prototype.find.call(button.querySelectorAll("img"), function (img) { return img.getClientRects().length > 0; }) || button.querySelector("img");
        if (!shown) return;
        image.src = shown.getAttribute("data-full") || shown.currentSrc || shown.src;
        image.alt = shown.alt;
        image.hidden = false;
        var figure = button.closest("figure"), text = figure && figure.querySelector("figcaption");
        caption.textContent = text ? text.textContent : "";
        dialog.showModal();
      });
    });
    // A click on the backdrop (the dialog itself, outside its content) closes it; Esc and the button are native.
    dialog.addEventListener("click", function (event) { if (event.target === dialog) dialog.close(); });
    dialog.addEventListener("close", function () { image.removeAttribute("src"); image.hidden = true; });
  }

  // A documentation page keeps the full document folded under its summary. A link to one of its headings, from the
  // table of contents or another page, opens the fold first, so the browser can scroll to the heading.
  function unfold(hash) {
    if (!hash || hash.length < 2) return;
    var target;
    try { target = document.getElementById(decodeURIComponent(hash.slice(1))); } catch (e) { return; }
    var fold = target && (target.matches("details[data-fold]") ? target : target.closest("details[data-fold]"));
    if (!fold) return;
    if (!fold.open) {
      fold.open = true;
      if (target !== fold) target.scrollIntoView();
    }
  }
  if (document.querySelector("details[data-fold]")) {
    unfold(location.hash);
    window.addEventListener("hashchange", function () { unfold(location.hash); });
    document.addEventListener("click", function (event) {
      var link = event.target.closest && event.target.closest('a[href^="#"]');
      if (link) unfold(link.getAttribute("href"));
    });
  }
})();
