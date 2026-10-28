# CPCA delivery 2 — working evidence

- `law-22-13-layer.json`: the 90 bodies quoted by Law 22-13, cleaned from the JO 48/2022 text layer (`extract_22_13.py`).
- `check-22-13-reread.json`, `check-base-reread.json`: comparison of the cleaned native texts with two OCR streams and the
  targeted re-read (`check_native.py`, `reread.py`).
- `image-decisions-22-13.json`: Claude's (AI) readings of the JO 48/2022 page images for every remaining 22-13 difference.
- `base-review-log.txt`: Claude's (AI) readings of the 93 base review sheets (item number, decision, note); item numbers
  index `tools/law-corpus/.cache/evidence/reviews/cpca-base/cpca-base-items.json`. Decisions: OK (print agrees), HDR (page
  header, not text), INS (printed mark missing from the HTML), FIX (word spacing/abbreviation from the print), RECHECK
  (strip showed the wrong line), STRUCT (art. 1065 carries heading and signature text).
Not lawyer verification.
