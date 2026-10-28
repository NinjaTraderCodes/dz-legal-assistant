# Civil Code: current-edition preparation and open validation

28 October 2026, `codex/law-corpus`. **No completed Civil delivery is claimed.** The typed package remains 45 identities / 55 versions (seven checked, 32 partly-checked, 16 unsettled). The preparation below is separate research data and has not replaced those versions or the application's law library.

The pinned [Ministry Arabic compilation](https://droit.mjustice.gov.dz/sites/default/files/ACivil.pdf), SHA-256 `17a555bf81d5b99bf67417f3d42c157fd5df69499d6190cffbf182bc3566b573`, retrieved 26 October 2026, has been decoded across **164 code pages, PDF8–171**, with the proven per-object glyph table. No glyph assignment is undecided. Later annexes are excluded. The previously identified WIPO copy has the same extracted text and supplies no independent legal witness.

`prepare_civil.py` now recovers **1,059 qualified source candidates**: 1,049 main-number records and ten locally numbered management-contract records. These include editorial repeal markers; they are not 1,059 verified live provisions. Main article41 is absent from this compilation and is independently accounted for in the existing explicit-repeal discovery. Paragraphs, inline editorial markers, headings, word spacing, amendments and current applicability still require validation.

The locally numbered chapter matters: **Law89-01 inserts Civil BookII/TitleIX's management-contract chapter with its own articles1–10.** The original JO page3 shows the insertion instruction and local1–3; page4 right contains local4–10, the amending law's publication article2 and signature. Page4 left starts Law89-02, which is excluded. The Ministry places the chapter between Civil570 and571 on PDF101–102. Its bare-alif first label `المادة الاولى` had escaped the old parser. Research `numberScope` separates these ten from general provisions1–10. No printed bis number is invented, and no existing identity is overwritten. The typed reader still needs explicit support for this qualified numbering before a complete import.

Actual source-image inspection covered Ministry PDF8,86,89,101,102,171 and all four cached JO89-01 column views. Font color/weight and geometry separate headings from bodies: the black bold subsection before505 is now excluded from504, and the signature after1003 remains paratext. A known open structure issue is recorded rather than hidden: native preparation merges the two visible body paragraphs of489. All inferred paragraph boundaries remain provisional.

The first comparison of **828 unannotated base-number candidates** with the cached original1975 JO gives **15 strict word agreements**, 5,209 word/digit/extra groups and 1,276 punctuation groups. These are recognition/alignment/edition review groups, **not a count of proven legal errors**. Absence of an editorial amendment label does not establish that an article has never changed. The approximate diagnostic and separate exact-word overlay are retained; neither grants a corpus verification state.

For the current-edition transcription check, **164 body crops** exclude historical footnotes at the PDF's actual separator geometry. A containment guard verifies every prepared body line against its crop and refuses clipping. Tesseract and PP-OCR run from the installed, pinned local tools; no new model or source download is needed. Supplementary OCR starts in ready batches while Tesseract continues. The existing recognizer's hash binding and historical evidence are unchanged. Raw PDFs, crops and OCR outputs stay in the ignored cache; progress is recoverable per crop.

Research records are in `data/law/corpus/research/civil-2026-10-28/`. Five new regressions cover the printed first-label alias, local/global number collisions, duplicate rejection, signature/page continuation and body-crop clipping. All **161 Python tests** pass. The last full Release checkpoint (`c9c3480`) passed all692 .NET tests with zero warnings/errors; the new checkpoint receives the same gate.

Remaining work:

1. Finish both current-edition OCR streams and exact body comparisons; inspect word/digit/negation flags and retain punctuation uncertainty explicitly.
2. Remove editorial annotations with guarded, reversible projections; validate paragraph and heading boundaries.
3. Reconcile every amending publication, insertion, partial rewrite and repeal against the JO, including qualified local numbering. Keep current outcomes separate from the deferred complete historical archive.
4. Import the complete current package with provenance, references and honest temporal limits; preserve the earlier typed history and checked pilot records.
5. Finish independent inventory/structure review, the reproducible article sample, legacy reconciliation and the full validation gate before declaring delivery.

Changes outside the corpus/tool directories in this checkpoint: this report and the coverage note in `docs/LAW-CORPUS.md`. No application behavior, legacy legal texts, claim mappings, prompts or safety rule was changed.
