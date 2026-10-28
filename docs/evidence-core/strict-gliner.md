# Strict fact audit

facts.strict/1

| reader | partition | cases | critical cases | dates correct / all expected | polarity correct / all expected |
|---|---|---:|---:|---:|---:|
| gliner2.5 | development | 44 | 44 | 0/8 | 2/54 |
| gliner2.5 | robustness | 2 | 2 | 0/0 | 1/5 |
| gliner2.5 | validation | 15 | 15 | 0/15 | 7/30 |

Legacy results are preserved. Missing decisive values and unreadable output are critical here. Matching is one-to-one in prediction order, never selected by gold-value agreement. Unannotated additions need further annotation before they can be called false.