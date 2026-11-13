# The Official Journal scan: summary

French edition, from issue 1 of 2003. Specification: `../../specs/official-journal-scan.md`. One line per issue in `issues-<year>.jsonl`.

An issue with **decoded lines** printed part of its text in a font whose text layer is shifted; those lines were decoded before the search (see `TEXT_VERSION` in the script).

**Unread** issues are those whose text could not be read: blind (no text layer) or failing the readability check (a page of enough words that does not read as French). They are listed as unread, never as "no match", and their tables of contents are read from page images by the triage.

| Year | Issues fetched | Unread | of which blind | In error | Date unread | With decoded lines | Title hits | Operative hits | Other hits | Patterns |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 2005 | 85 | 2 | 0 | 0 | 0 | 11 | 8 | 107 | 29 | 1 |
| 2007 | 82 | 0 | 0 | 0 | 0 | 0 | 2 | 20 | 15 | 1 |
| 2022 | 90 | 0 | 0 | 0 | 0 | 0 | 64 | 131 | 118 | 1 |
