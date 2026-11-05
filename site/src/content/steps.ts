// The nine steps of the case form and the petition they fill, with the labels of the form definition
// (data/document-types/requete-introductive/definition.json). The petition is the app's own, section by section, for
// the fictitious sample case the demo and the video use (src/DzLegal.Web/Samples/debt.json): src/content/paper-ar.json,
// read from the demo's paper by tests/paper-dump.mjs. No real person, place or case.
import type { Lang } from "../i18n";
import run from "./grounds-run-ar.json";
import appPaper from "./paper-ar.json";
import civil from "../../../data/law/claims/civil.json";

export interface Step { id: string; label: Record<Lang, string>; section: string }

/** A line of the grounds: the rule of an article, written by code from the official text, or the AI's application of it. */
export interface PaperLine { text: string; by: "code" | "ai" }

/** One section of the app's paper: its layout from the form definition and its text, line breaks kept. */
export interface PaperBlock { align: "start" | "center" | "justify" | "end"; bold: boolean; spaceBefore: number; heading: string | null; text: string }

/** A paragraph of the legal grounds as the app writes it: one paragraph, the rule quoted by code and then the model's application. */
export interface GroundsParagraph { rule: string; rest: string }

/** The part of the petition one step of the form fills: one or more of the app's sections. */
export interface PaperSection { id: string; blocks: PaperBlock[]; ai?: boolean; review?: boolean; grounds?: GroundsParagraph[]; domicile?: { before: string; domicile: string; after: string } }

export const steps: Step[] = [
    { id: "court", label: { ar: "الجهة القضائية", en: "Court" }, section: "header" },
    { id: "plaintiff", label: { ar: "المدعي", en: "Plaintiff" }, section: "plaintiff" },
    { id: "defendant", label: { ar: "المدعى عليه", en: "Defendant" }, section: "defendant" },
    { id: "claim", label: { ar: "موضوع الدعوى", en: "Claim" }, section: "subject" },
    { id: "facts", label: { ar: "الوقائع", en: "Facts" }, section: "facts" },
    { id: "grounds", label: { ar: "الأسانيد القانونية", en: "Legal grounds" }, section: "grounds" },
    { id: "requests", label: { ar: "الطلبات", en: "Requests" }, section: "requests" },
    { id: "documents", label: { ar: "المستندات", en: "Supporting documents" }, section: "documents" },
    { id: "signature", label: { ar: "التاريخ والتوقيع", en: "Date and signature" }, section: "signature" },
];

/** The grounds section's own title, as the app's review row and export notice name it (definition.json, sections). */
export const groundsTitle: Record<Lang, string> = { ar: "المناقشة القانونية", en: "Legal grounds" };

/**
 * The order the story writes the sections: the lawyer's own entries first, in document order, and the legal grounds
 * last, as in the app, where the AI drafts them once the facts are in and the formal check waits for them.
 */
export const writingOrder = ["court", "plaintiff", "defendant", "claim", "facts", "requests", "documents", "signature", "grounds"];

/**
 * The legal grounds are the real output of a model run on this sample, verbatim (src/content/grounds-run-ar.json,
 * written by tests/guide-shots.mjs --dump from the page of the demo). The app writes each paragraph as the rule of
 * the ticked articles, taken from the claim catalogue, followed by the model's application; the split below finds
 * where the catalogue's wording ends, so the sheet can mark what the code wrote and what the model wrote.
 */
export const groundsRun = { model: run.answeredBy, engine: run.engine, date: run.date, lang: run.lang, sample: run.sample };
// A catalogue line may open with a label («تعريف قرض الاستهلاك: …»); the app quotes the part after the colon.
const rules: string[] = ((civil as { claims: { id: string; articles: { why: { ar: string } }[] }[] }).claims
    .find((claim) => claim.id === "loan-repayment")?.articles.map((article) => article.why.ar) ?? [])
    .flatMap((why) => [why, why.split(":").pop()?.trim() ?? why]);
/** A paragraph of the grounds split where the catalogue's rule ends: the code's quotation, then the model's application. */
export function splitGrounds(paragraph: string): PaperLine[] {
    let end = 0;
    for (const rule of rules) {
        const at = paragraph.indexOf(rule);
        if (at >= 0) end = Math.max(end, at + rule.length);
    }
    if (end === 0) return [{ by: "ai", text: paragraph }];
    const rule = paragraph.slice(0, end).replace(/[،,\s]+$/, "");
    const application = paragraph.slice(end).replace(/^[،,.\s]+/, "");
    return application ? [{ by: "code", text: rule + "." }, { by: "ai", text: application }] : [{ by: "code", text: rule + "." }];
}
/** The same split, keeping the paragraph's own text exactly: the app prints it whole, without quotation marks. */
export function splitRule(paragraph: string): GroundsParagraph {
    let end = 0;
    for (const rule of rules) {
        const at = paragraph.indexOf(rule);
        if (at >= 0) end = Math.max(end, at + rule.length);
    }
    // The comma that joins the rule to the application stays with the rule, so the site's mark sits after it.
    const comma = paragraph.slice(end).match(/^[،,]/)?.[0].length ?? 0;
    return { rule: paragraph.slice(0, end + comma), rest: paragraph.slice(end + comma) };
}

// Which step fills each of the app's thirteen sections, in the paper's order: the heading lines come with the court;
// the subject, the address to the court and the paragraph on form with the claim; the requests' introduction and
// list together; the place and date with the signature.
const stepOfSection = ["court", "plaintiff", "defendant", "claim", "claim", "claim", "facts", "grounds", "requests", "requests", "documents", "signature", "signature"];
const blocks = appPaper.sections as PaperBlock[];
if (blocks.length !== stepOfSection.length || blocks[7].heading !== "المناقشة القانونية") throw new Error("src/content/paper-ar.json no longer matches the story's sections (node tests/paper-dump.mjs, then src/content/steps.ts)");
/** The petition on paper, grouped by the step that fills it, always in Arabic (the document's language). */
export const paper: PaperSection[] = steps.map((step) => {
    const own = blocks.filter((_, i) => stepOfSection[i] === step.id);
    const section: PaperSection = { id: step.id, blocks: own };
    if (step.id === "grounds") Object.assign(section, { ai: true, review: true, grounds: run.paragraphs.map(splitRule) });
    if (step.id === "defendant") {
        // The domicile is the part of the defendant's line the story takes away: from «القاطن بـ:» to the line's end.
        const text = own[0].text;
        const at = text.indexOf("القاطن بـ:");
        const eol = text.indexOf("\n", at);
        if (at > 0) section.domicile = { before: text.slice(0, at), domicile: text.slice(at, eol < 0 ? undefined : eol), after: eol < 0 ? "" : text.slice(eol) };
    }
    return section;
});
/** Whether the app's paper carries the draft watermark for this sample (the sample case forces it). */
export const paperWatermark = appPaper.watermark;
