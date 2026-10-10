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
};
