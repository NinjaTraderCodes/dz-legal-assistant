// A stand-in for the hosted model, for the guide test: a local server speaking the chat-completions protocol
// (POST /v1/chat/completions, streamed as server-sent events) that answers each of the demo's prompts in the shape
// its parsers expect. Nothing leaves the machine and no credit is spent. Point the demo at it with
//   GROQ_API_KEY=x Engines__GroqEndpoint=http://127.0.0.1:5090/v1/ Engines__GroqModel=mock
// (the other keys unset), as docs/WEB-DEMO.md describes. Usage: node tests/mock-model.mjs [--port 5090] [--log]
import { createServer } from "node:http";

const arg = (name, fallback) => { const i = process.argv.indexOf("--" + name); return i > 0 ? process.argv[i + 1] : fallback; };
const port = Number(arg("port", "5090"));
const log = process.argv.includes("--log");

/** The prompt's list of known claim types ("id — name: when"), as ids; the loan claim first when it is offered. */
function claimId(prompt) {
    const ids = [...prompt.matchAll(/^([a-z][a-z0-9-]+) — /gmu)].map((m) => m[1]);
    return ids.includes("loan-repayment") ? "loan-repayment" : ids[0] ?? "other";
}

/** The candidate keys of the selection prompt, in its own order. */
function candidateKeys(prompt) {
    const block = prompt.split("المواد المرشحة")[1] ?? "";
    return [...block.matchAll(/^([^\s—]+) — /gmu)].map((m) => m[1]);
}

/** How many numbered lines the argument prompt asks for. */
function paragraphCount(prompt) {
    const m = prompt.match(/اكتب\s+(\d+)\s+أسطر/u);
    return m ? Number(m[1]) : 1;
}

// One sentence per ground, as the argued-grounds prompt asks: it starts with «وحيث إن», names the parties by their
// roles, applies the rule to the facts, cites no article, carries no number and no quotation, and leaves the request
// to the program. The drafting checks let it through.
const sentence = "وحيث إن المدعى عليه اقترض من المدعي المبلغ المذكور في الوقائع بموجب اعتراف بدين موثق، وحل الأجل ولم يسدد رغم الإعذار الرسمي.";
const argument = sentence + " فإن طلب المدعي إلزام المدعى عليه بدفع المبلغ المطلوب يكون مؤسسا قانونا.";

function answer(prompt) {
    if (prompt.includes("كلمات البحث:") && prompt.includes("نوع الدعوى:")) {
        return [
            "صاحب الطلب: المدعي يطلب من المحكمة إلزام المدعى عليه برد القرض المذكور في الوقائع.",
            "العلاقة القانونية: عقد قرض بين المدعي والمدعى عليه ثابت باعتراف بدين موثق.",
            "الالتزام المخل به: لم يرد المدعى عليه مبلغ القرض عند حلول الأجل رغم الإعذار.",
            "المطلوب: إلزام المدعى عليه برد مبلغ القرض مع التعويض عن التأخير.",
            `نوع الدعوى: ${claimId(prompt)}`,
            "كلمات البحث: قرض، رد القرض، اعتراف بدين، حلول الأجل، إعذار، تأخر في الوفاء، تعويض عن التأخير",
        ].join("\n");
    }
    if (prompt.includes("المواد المرشحة")) {
        const keys = candidateKeys(prompt);
        const roles = ["أساس", "أساس", "إثبات", "تعويض"];
        return keys.slice(0, 3).map((k, i) => `${k} | ${roles[i]} | تطبق على القرض الذي لم يرد عند حلول الأجل رغم الإعذار`).join("\n") || "لا شيء";
    }
    if (prompt.includes("سطر واحد فقط، دون رقم")) {
        return sentence; // the per-paragraph retry: one line, no number
    }
    if (prompt.includes("لكل رقم، اكتب ما يأتي")) {
        const n = paragraphCount(prompt);
        return Array.from({ length: n }, (_, i) => `${i + 1}- ${sentence}`).join("\n");
    }
    if (prompt.includes("الحجة القانونية لكل سند")) {
        const n = paragraphCount(prompt);
        return Array.from({ length: n }, (_, i) => `${i + 1}- ${argument}`).join("\n");
    }
    if (prompt.includes("أعد صياغة طلبات المحامي")) {
        const m = prompt.match(/(\d+)\s+سطرا/u);
        const n = m ? Number(m[1]) : 1;
        return Array.from({ length: n }, () => "إلزام المدعى عليه بأن يدفع للمدعي المبلغ المطلوب كما ورد في الطلبات.").join("\n");
    }
    // Any other request (the assistant, a redraft): the argument again, as plain prose.
    return argument;
}

function sse(res, text) {
    res.writeHead(200, { "Content-Type": "text/event-stream; charset=utf-8", "Cache-Control": "no-cache" });
    const pieces = text.match(/[^\n]{1,40}\n?/gu) ?? [text];
    for (const piece of pieces) res.write(`data: ${JSON.stringify({ choices: [{ delta: { content: piece }, finish_reason: null }] })}\n\n`);
    res.write(`data: ${JSON.stringify({ choices: [{ delta: {}, finish_reason: "stop" }], usage: { completion_tokens: pieces.length } })}\n\n`);
    res.write("data: [DONE]\n\n");
    res.end();
}

createServer((req, res) => {
    if (req.method !== "POST" || !req.url.endsWith("/chat/completions")) { res.writeHead(404); res.end(); return; }
    let body = "";
    req.on("data", (c) => (body += c));
    req.on("end", () => {
        let prompt = "";
        try { prompt = JSON.parse(body).messages.map((m) => m.content).join("\n"); } catch { /* an empty prompt gets the default answer */ }
        if (log) console.log("mock-model: request", prompt.replace(/\s+/g, " ").slice(0, 160));
        sse(res, answer(prompt));
    });
}).listen(port, "127.0.0.1", () => console.log(`mock-model: listening on http://127.0.0.1:${port}/v1/chat/completions`));
