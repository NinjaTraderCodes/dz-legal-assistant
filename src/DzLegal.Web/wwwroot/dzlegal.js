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

// R3: interface preferences only, kept separately from dzlegal.case.
window.dzlegal.openDraftPicker = () => document.querySelector('input[type=file]')?.click();
window.dzlegal.initWorkbench = async function (bridge) {
  this._workbench?.abort.abort();
  const root = document.getElementById('workbench');
  if (!root) return;
  const abort = new AbortController(), signal = abort.signal;
  const clamp = (v, min, max) => Math.max(min, Math.min(max, v));
  let stored = {};
  try { stored = JSON.parse(localStorage.getItem('dzlegal.workbench.layout') || '{}'); } catch {}
  if (!stored || typeof stored !== 'object' || Array.isArray(stored)) stored = {};
  const bounded = (value, fallback, min, max) => typeof value === 'number' && Number.isFinite(value) && value >= min && value <= max ? value : fallback;
  const prefs = {
    outline: bounded(stored.outline, 224, 160, 360), editor: bounded(stored.editor, 440, 320, 900), tools: bounded(stored.tools, 240, 120, 500),
    compact: typeof stored.compact === 'boolean' ? stored.compact : innerWidth < 1500,
    outlineVisible: typeof stored.outlineVisible === 'boolean' ? stored.outlineVisible : true,
    toolsOpen: typeof stored.toolsOpen === 'boolean' ? stored.toolsOpen : false,
    tool: ['review','source','assistant','activity'].includes(stored.tool) ? stored.tool : 'review'
  };
  const wb = this._workbench = { root, bridge, abort, prefs, active: 0, wasPalette: false };
  const save = () => { try { localStorage.setItem('dzlegal.workbench.layout', JSON.stringify(prefs)); } catch {} };
  const apply = () => {
    const outline = root.classList.contains('outline-hidden') ? 0 : root.classList.contains('outline-compact') ? 64 : prefs.outline;
    root.style.setProperty('--outline-width', prefs.outline + 'px');
    root.style.setProperty('--editor-width', clamp(prefs.editor, 320, Math.max(320, root.clientWidth - outline - 12 - 440)) + 'px');
    const stack = root.querySelector('.editor-stack'), engine = root.querySelector('.editor-engine'), tabs = root.querySelector('.tool-tabs');
    if (!stack || stack.clientHeight === 0) return;
    const tools = root.classList.contains('tools-open') ? clamp(prefs.tools, 120, Math.max(120, stack.clientHeight - engine.offsetHeight - tabs.offsetHeight - 6 - 220)) : 0;
    root.style.setProperty('--effective-tools-height', tools + 'px');
    root.style.setProperty('--editor-work-height', Math.max(82, stack.clientHeight - engine.offsetHeight - tabs.offsetHeight - tools - (tools ? 6 : 0)) + 'px');
    const narrativeMax = Math.max(82, Math.min(260, parseFloat(root.style.getPropertyValue('--editor-work-height')) - 140));
    root.style.setProperty('--narrative-max-height', (Math.floor((narrativeMax - 18) / 32) * 32 + 18) + 'px');
    for (const h of root.querySelectorAll('[data-resize]')) {
      const kind = h.dataset.resize, compact = root.classList.contains('outline-compact');
      const min = kind === 'outline' ? (compact ? 64 : 160) : kind === 'editor' ? 320 : 120;
      const max = kind === 'outline' ? (compact ? 64 : 360) : kind === 'editor' ? Math.max(320, Math.min(900, root.clientWidth - outline - 12 - 440)) : Math.max(120, stack.clientHeight - engine.offsetHeight - tabs.offsetHeight - 6 - 220);
      const now = kind === 'outline' ? outline : kind === 'editor' ? parseFloat(root.style.getPropertyValue('--editor-width')) : tools;
      h.setAttribute('aria-valuemin', String(min)); h.setAttribute('aria-valuemax', String(max)); h.setAttribute('aria-valuenow', String(now));
    }
  };
  wb.apply = apply; wb.save = save;
  const observer = new ResizeObserver(apply);
  observer.observe(root); observer.observe(root.querySelector('.editor-stack'));
  signal.addEventListener('abort', () => observer.disconnect());
  window.addEventListener('resize', apply, {signal});
  await bridge.invokeMethodAsync('RestoreWorkbenchLayout', {compact:prefs.compact, outlineVisible:prefs.outlineVisible, toolsOpen:prefs.toolsOpen, tool:prefs.tool});
  apply();
  const value = kind => kind === 'outline' ? prefs.outline : kind === 'editor' ? prefs.editor : prefs.tools;
  const set = (kind, number) => {
    if (kind === 'outline') prefs.outline = clamp(number, 160, 360);
    else if (kind === 'editor') {
      const outline = root.classList.contains('outline-hidden') ? 0 : root.classList.contains('outline-compact') ? 64 : prefs.outline;
      prefs.editor = clamp(number, 320, Math.max(320, Math.min(900, root.clientWidth - outline - 12 - 440)));
    } else prefs.tools = clamp(number, 120, 500);
    apply(); save();
  };
  for (const handle of root.querySelectorAll('[data-resize]')) {
    handle.addEventListener('keydown', e => {
      const kind = handle.dataset.resize, horizontal = kind === 'tools';
      if (!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Home'].includes(e.key)) return;
      e.preventDefault();
      if (kind === 'outline' && root.classList.contains('outline-compact')) return;
      if (e.key === 'Home') set(kind, kind === 'outline' ? 224 : kind === 'editor' ? 440 : 240);
      else if (horizontal && ['ArrowUp','ArrowDown'].includes(e.key)) set(kind, value(kind) + (e.key === 'ArrowUp' ? 16 : -16));
      else if (!horizontal && ['ArrowLeft','ArrowRight'].includes(e.key)) set(kind, value(kind) + (e.key === 'ArrowRight' ? 16 : -16));
    }, {signal});
    handle.addEventListener('dblclick', () => set(handle.dataset.resize, handle.dataset.resize === 'outline' ? 224 : handle.dataset.resize === 'editor' ? 440 : 240), {signal});
    handle.addEventListener('pointerdown', e => {
      if (e.button !== 0 || (handle.dataset.resize === 'outline' && root.classList.contains('outline-compact'))) return;
      e.preventDefault(); handle.focus(); handle.setPointerCapture(e.pointerId);
      const kind = handle.dataset.resize, horizontal = kind === 'tools', start = horizontal ? e.clientY : e.clientX, original = value(kind);
      const bounds = (horizontal ? root.querySelector('.editor-stack') : root.querySelector('.columns')).getBoundingClientRect();
      const ghost = document.createElement('div'); ghost.className = 'resize-ghost'; root.append(ghost);
      let end = start;
      const move = event => {
        end = horizontal ? event.clientY : event.clientX;
        Object.assign(ghost.style, horizontal
          ? {left:bounds.left+'px', top:end+'px', width:bounds.width+'px', height:'6px'}
          : {left:end+'px', top:bounds.top+'px', width:'6px', height:bounds.height+'px'});
      };
      const finish = event => {
        handle.removeEventListener('pointermove', move); handle.removeEventListener('pointerup', finish); handle.removeEventListener('pointercancel', cancel);
        ghost.remove();
        if (event.type === 'pointerup') set(kind, original + (horizontal ? start - end : end - start));
      };
      const cancel = event => finish(event);
      handle.addEventListener('pointermove', move);
      handle.addEventListener('pointerup', finish);
      handle.addEventListener('pointercancel', cancel);
      move(e);
    }, {signal});
  }
  root.addEventListener('pointerover', e => {
    const summary = e.target.closest('.field-help>summary'); if (summary) summary.parentElement.open = true;
  }, {signal});
  root.addEventListener('focusin', e => {
    if (!e.target.closest('.command-palette')) wb.lastNonPaletteFocus = e.target;
    const summary = e.target.closest('.field-help>summary'); if (summary) summary.parentElement.open = true;
  }, {signal});
  root.addEventListener('click', e => {
    const summary = e.target.closest('.field-help>summary');
    if (summary) { e.preventDefault(); const details = summary.parentElement; details.dataset.pinned = details.dataset.pinned === '1' ? '0' : '1'; details.open = details.dataset.pinned === '1'; }
  }, {signal});
  const closeHelp = e => {
    const details = e.target.closest('.field-help');
    if (details && !details.contains(e.relatedTarget) && details.dataset.pinned !== '1' && !details.contains(document.activeElement)) details.open = false;
  };
  root.addEventListener('pointerout', closeHelp, {signal}); root.addEventListener('focusout', closeHelp, {signal});
  document.addEventListener('keydown', e => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); wb.returnFocus = document.activeElement; bridge.invokeMethodAsync('ShowCommands'); return; }
    const palette = root.querySelector('.command-palette');
    if (palette) {
      const entries = [...palette.querySelectorAll('.command-results button:not(:disabled)')];
      if (e.key === 'Escape') { e.preventDefault(); bridge.invokeMethodAsync('CloseCommands'); }
      else if (['ArrowUp','ArrowDown'].includes(e.key) && entries.length) {
        e.preventDefault(); wb.active = clamp(wb.active + (e.key === 'ArrowDown' ? 1 : -1), 0, entries.length - 1);
        entries.forEach((b,i) => b.classList.toggle('active-command', i === wb.active)); entries[wb.active].scrollIntoView({block:'nearest'});
      } else if (e.key === 'Enter' && document.activeElement?.id === 'command-query' && entries.length) {
        e.preventDefault(); entries[Math.min(wb.active, entries.length - 1)].click();
      } else if (e.key === 'Tab') {
        const stops = [...palette.querySelectorAll('input,button:not(:disabled)')];
        if (e.shiftKey && document.activeElement === stops[0]) { e.preventDefault(); stops.at(-1)?.focus(); }
        else if (!e.shiftKey && document.activeElement === stops.at(-1)) { e.preventDefault(); stops[0]?.focus(); }
      }
    } else if (e.key === 'Escape') {
      for (const details of root.querySelectorAll('.field-help[open]')) { details.open = false; details.dataset.pinned = '0'; e.preventDefault(); }
    }
  }, {signal});
};
window.dzlegal.syncWorkbench = function () {
  const wb = this._workbench; if (!wb || !wb.root.isConnected) return;
  const root = wb.root, prefs = wb.prefs;
  prefs.compact = root.dataset.compact === 'true'; prefs.outlineVisible = root.dataset.outlineVisible === 'true';
  prefs.toolsOpen = root.dataset.toolsOpen === 'true'; prefs.tool = root.dataset.tool;
  wb.apply(); wb.save();
  const palette = root.querySelector('.command-palette'), query = root.querySelector('#command-query');
  if (palette && !wb.wasPalette) {
    if (!wb.returnFocus?.isConnected || palette.contains(wb.returnFocus)) wb.returnFocus = wb.lastNonPaletteFocus;
    wb.active = 0; query?.focus();
  }
  if (!palette && wb.wasPalette) {
    if (wb.returnFocus?.isConnected) wb.returnFocus.focus();
    wb.returnFocus = null;
  }
  if (palette) {
    const count = palette.querySelectorAll('.command-results button').length;
    if (wb.lastCount !== count || wb.lastQuery !== query?.value) { wb.active = 0; wb.lastCount = count; wb.lastQuery = query?.value; }
    palette.querySelectorAll('.command-results button').forEach((b,i) => b.classList.toggle('active-command', i === wb.active));
  }
  wb.wasPalette = !!palette;
};
