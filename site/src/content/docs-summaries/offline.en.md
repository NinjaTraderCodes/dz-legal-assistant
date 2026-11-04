---
lawyer: "You can draft with the network unplugged: your client's facts stay on your computer, as professional secrecy requires."
---

- The desktop app is built so that no case data leaves the lawyer's computer, because client case data is covered by professional secrecy.
- The form, the templates, the storage, the interface, the template mode, the grounds suggestions, the law library and the article search use no network at all.
- The local model is reached only at 127.0.0.1:11434, a process on the same computer: its packets never leave the machine.
- Any other address is refused without even a DNS lookup; the model's connection uses no proxy and follows no redirect, and Ollama "cloud" models are refused.
- The app has no telemetry, analytics, crash reporting or update check.
- An "official source" link opens in the browser only when you click it; it is fixed data, never built from the case, and only joradp.dz and mjustice.dz links are accepted.
- Drafts are saved where you choose, by default in your local application-data folder, and nothing is synchronised anywhere.
- The public web demo is a separate program that sends its prompts to a hosted model, so it is for fictitious cases only.
