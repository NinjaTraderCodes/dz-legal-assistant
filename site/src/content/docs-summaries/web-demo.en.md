---
lawyer: "Try the demo with invented cases only: your real files belong in the desktop app, where nothing leaves your computer."
---

- The product is an offline desktop app; the web demo exists so that anyone can use the real product from a link, without installing anything.
- It runs the same engine, the same law library and the same screen logic; the one difference is where the AI model runs.
- On the desktop the model is Gemma 3 4B, local; in the demo it is Nemotron 3 Ultra hosted on OpenRouter first, then Gemma 4 26B on Google AI Studio, then Groq.
- The demo sends the case to the hosted model, so it is for fictitious cases only.
- The demo saves nothing on the server: Open and Save use a draft file on the visitor's own disk, and the case is otherwise kept only as a recovery copy in the browser.
- Model calls are limited per visitor and per network address, and go through one shared queue, two calls at a time.
- Failures never end a visitor's session: a busy or failing service hands the request to the next engine, and the visitor sees a plain message.
- Even the demo's larger model makes mistakes, which is why the checks matter: the articles a claim type requires are always proposed, and the program writes each ticked article's rule.
