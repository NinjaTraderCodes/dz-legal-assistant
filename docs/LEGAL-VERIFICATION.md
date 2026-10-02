# Legal verification checklist (for the practicing lawyer)

The rules in `data/document-types/requete-introductive/definition.json` were drafted by the engineering team from the **published text of Loi n° 08-09 (CPCA)** as we understand it. **None has been confirmed by a lawyer yet.** Each carries `"verified": false` and shows "لم يتحقق منه محامٍ بعد / Not yet verified by a lawyer" in the app.

Please check each point against the **current official text (Journal Officiel), including Loi 22-13**, and against your practice. Corrections are data edits (see [UPDATING-LEGAL-RULES.md](UPDATING-LEGAL-RULES.md)).

## 1. Our reading of the relevant articles (to confirm)

**Art. 15:** «يجب أن تتضمن عريضة افتتاح الدعوى، تحت طائلة عدم قبولها شكلا، البيانات الآتية:
1. الجهة القضائية التي ترفع أمامها الدعوى،
2. اسم ولقب المدعي وموطنه،
3. اسم ولقب وموطن المدعى عليه، فإن لم يكن له موطن معلوم، فآخر موطن له،
4. الإشارة إلى تسمية وطبيعة الشخص المعنوي، ومقره الاجتماعي وصفة ممثله القانوني أو الاتفاقي،
5. عرضا موجزا للوقائع والطلبات والوسائل التي تؤسس عليها الدعوى،
6. الإشارة، عند الاقتضاء، إلى المستندات والوثائق المؤيدة للدعوى.»

**Art. 14:** the claim is brought by a **written, signed and dated** requête, filed at the greffe by the plaintiff, their agent or their lawyer, in as many copies as there are parties.

**Art. 17 (para. 2):** a requête concerning **real property and/or a registered real right** must be **published at the conservation foncière** and produced at the first hearing, on pain of formal inadmissibility, unless deposit for publication is proven.

**Art. 8:** proceedings and pleadings must be in **Arabic**, on pain of inadmissibility.

## 2. Element-by-element

| Element id | Article | What the app checks | Question for you |
|---|---|---|---|
| `art15.1.court` | 15-1 | Court name entered and heading present | Is naming the tribunal enough, or must the section (القسم) always appear? |
| `art15.2.plaintiff` | 15-2 | Surname, first name, domicile | Is profession or parentage (ابن/بنت) expected in practice? |
| `art15.3.defendant` | 15-3 | Surname, first name, and domicile **or** last known domicile | Confirmed? |
| `art15.4.plaintiff` / `art15.4.defendant` | 15-4 | Legal person: name, nature, registered office, representative's capacity | Must the representative's **name** also appear? |
| `art15.5.facts` / `.grounds` / `.requests` | 15-5 | Each of the three sections is non-empty | Confirmed as three distinct requirements? |
| `art15.6.documents` | 15-6 | List of supporting documents (**warning** only, "where applicable") | Right severity? |
| `art14.date` | 14 | A date dd/mm/yyyy in the closing line | Is a missing date sanctioned by inadmissibility, or only a formality? |
| `art14.signature` | 14 | Signature block with the lawyer's name | Same question: what is the sanction? |
| `art17.publication` | 17 | For real-property claims: publication reference entered and mentioned | What exactly must appear **in the requête** itself, as opposed to proof produced at the first hearing? |
| `art8.arabic` | 8 | At least 60% of letters Arabic in subject/facts/grounds/requests | Is inline French terminology acceptable? Is 60% a sensible threshold? |
| `form.subject` | 15-5 | "الموضوع:" line present (**warning**) | The project brief lists *objet de la demande* as mandatory; we read it as part of item 5. Which is right? |
| `form.order` | practice | Usual order of parts (**warning**) | Is the order header → parties → subject → facts → grounds → requests → documents → date → signature right? |

## 3. Anything missing?

- Did **Loi 22-13** add or change any item of arts. 14, 15 or 17?
- Are there other requirements (other articles, or court practice) whose omission causes rejection of a requête introductive, for example the lawyer's bar registration or the parties' nationality?
- Should court fees (art. 17 para. 1) or the number of copies (art. 14) be a reminder in the app, even though they are not part of the document text?

## 4. Template wording

Since rules v0.2.0, the scaffold in `templates/` follows the formulas that recur in public Algerian model requêtes (tribunaldz.com, kanundz, elmouhami, university course material):

| Part | Formula |
|---|---|
| Parties | «لفائدة: السيد … القاطن بـ: …» / «القائم في حقه الأستاذ …، محام لدى …» / «ضد: …» |
| Addressee | «إلى السيد رئيس القسم المدني بمحكمة …» |
| Opening | «يتشرف المدعي بواسطة دفاعه أن يعرض على هيئتكم الموقرة ما يلي:» |
| Closing | «لهذه الأسباب ومن أجلها» + «يلتمس المدعي من هيئة المحكمة الموقرة القضاء بما يلي:» |
| End | «حرر ب… في: …» / «عن المدعي» / «الأستاذ …» / «التوقيع:» |

Grammatical gender comes from the form. For a legal-person plaintiff, the feminine is used («تتشرف المدعية»، «القائم في حقها»).

Please confirm:
- the addressee: should it be the section president or «السادة رئيس وأعضاء المحكمة»?
- whether «القاطن» or «الساكن» is preferred;
- the feminine agreement for legal persons;
- anything outdated or regional.

## 5. Recording your confirmation

For each element you confirm, we will set `"verified": true` and note "confirmed by <name>, <date>, against <text/version>" in `sourceNote`. For each correction, we will add a corpus case reproducing it before changing the rule, so it can never silently regress.

## Law library and claim catalog (grounds suggestions)

See [LAW-LIBRARY.md](LAW-LIBRARY.md). Two things need a lawyer:

1. **Each imported code.** Read its import report, compare a sample of articles with the Journal Officiel, then set `verified: true`.
2. **Each claim type** in `data/law/claims/*.json`:
   - Are the cited articles the right ones, and are any missing (for example interest on late payment, or proof rules)?
   - Is each article's role right: basis, proof, damages or procedure?
   - Are the suggested requests and the required documents those used in practice?
   - Are the prerequisites complete? For example, labour conciliation under Law 90-04; for commercial leases, the 2005 reform (Commercial Code art. 187 bis).
   - Which frequent claim types are missing?

   Then set `verified: true` on that claim.

Please also write **10–20 fact patterns from your practice** (anonymised) with the claim you would start from. They become the hold-out test set that measures the suggestions honestly.
