// The nine steps of the case form and the sections of the petition they fill, with the labels of the form
// definition (data/document-types/requete-introductive/definition.json). The paper text is the fictitious sample
// case the demo and the video use (src/DzLegal.Web/Samples/debt.json): no real person, place or case.
import type { Lang } from "../i18n";

export interface Step { id: string; label: Record<Lang, string>; section: string }

/** A line of the grounds: the rule of an article, written by code from the official text, or the AI's application of it. */
export interface PaperLine { text: string; by: "code" | "ai" }

export interface PaperSection { id: string; heading?: string; lines: (string | PaperLine)[]; domicile?: string; ai?: boolean; review?: boolean }

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

/**
 * The order the story writes the sections: the lawyer's own entries first, in document order, and the legal grounds
 * last, as in the app, where the AI drafts them once the facts are in and the formal check waits for them.
 */
export const writingOrder = ["court", "plaintiff", "defendant", "claim", "facts", "requests", "documents", "signature", "grounds"];

/** The petition on paper, section by section, always in Arabic (the document's language). */
export const paper: PaperSection[] = [
    { id: "header", lines: ["الجمهورية الجزائرية الديمقراطية الشعبية", "مجلس قضاء الجزائر", "محكمة سيدي امحمد — القسم المدني", "عريضة افتتاح دعوى"] },
    { id: "plaintiff", lines: ["لفائدة: السيد بن علي كريم، تاجر، القاطن بـ: 12 شارع ديدوش مراد، الجزائر.", "القائم في حقه الأستاذ حداد كريم، محام لدى مجلس قضاء الجزائر، الكائن مكتبه بـ: 04 شارع كريم بلقاسم، الجزائر.", "(مدعٍ)"] },
    { id: "defendant", lines: ["ضد: السيد حمداني سمير،"], domicile: "القاطن بـ: 5 نهج العربي بن مهيدي، الجزائر." },
    { id: "subject", lines: ["الموضوع: المطالبة بوفاء دين مع التعويض", "إلى السيد رئيس القسم المدني بمحكمة سيدي امحمد", "يتشرف المدعي بواسطة دفاعه أن يعرض على هيئتكم الموقرة ما يلي:"] },
    { id: "facts", heading: "الوقائع:", lines: ["حيث إنه بتاريخ 15 جانفي 2025، اقترض المدعى عليه من المدعي مبلغا ماليا قدره 1,500,000 دج بموجب اعتراف بدين موثق.", "وحيث إنه اتفق الطرفان على إرجاع المبلغ في أجل أقصاه 30 جوان 2025.", "وحيث إنه رغم حلول الأجل وإعذار المدعى عليه رسميا عن طريق المحضر القضائي بتاريخ 10 جويلية 2025، امتنع عن السداد دون أي مبرر قانوني."] },
    // The grounds as the app writes them: each rule opens with its articles, quoted by code from the official text,
    // and the AI writes only how the facts meet it. The AI lines are short so that each is one visual line on the sheet.
    { id: "grounds", heading: "المناقشة القانونية:", ai: true, review: true, lines: [
        { by: "code", text: "حيث إن المواد 106 و107 و450 من القانون المدني تقضي بأن العقد شريعة المتعاقدين، وبأنه يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية، وبأنه يلتزم المقترض برد نظير ما اقترضه عند نهاية القرض." },
        { by: "ai", text: "وحيث إن المدعى عليه اقترض من المدعي 1,500,000 دج بتاريخ 15 جانفي 2025،" },
        { by: "ai", text: "وحيث إن الأجل حلّ في 30 جوان 2025 دون أي سداد،" },
        { by: "ai", text: "رغم الإعذار الرسمي المبلّغ له بتاريخ 10 جويلية 2025." },
        { by: "code", text: "وحيث إن المادة 323 من القانون المدني تقضي بأن على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه." },
        { by: "ai", text: "وحيث إن الاعتراف بالدين الموثق يثبت الالتزام، ولم يثبت المدعى عليه الوفاء،" },
        { by: "ai", text: "فيكون طلب إلزامه بدفع أصل الدين مؤسسا قانونا." },
    ] },
    { id: "requests", heading: "لهذه الأسباب ومن أجلها:", lines: ["يلتمس المدعي من هيئة المحكمة الموقرة القضاء بما يلي:", "في الشكل: قبول الدعوى شكلا.", "في الموضوع: 1. إلزام المدعى عليه بأن يدفع للمدعي أصل الدين المقدر بـ 1,500,000 دج."] },
    { id: "documents", heading: "قائمة المرفقات:", lines: ["1. نسخة من الاعتراف بالدين الموثق.", "2. محضر الإعذار المؤرخ في 10 جويلية 2025."] },
    { id: "signature", lines: ["حرر بالجزائر في 2025/09/01", "عن المدعي، الأستاذ حداد كريم"] },
];
