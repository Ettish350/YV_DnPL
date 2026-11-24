# D-007: Move 23 activity-type display fields from A1 to C3 (online and kiosk; C4 in the old numbering)

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting Lior |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
23 kiosk and website display fields of `new_tour_type` (kiosk names, price explanations, sort weights, display tab, text alignment, etc.) are not built in A1. They belong to C3 (online and kiosk; C4 in the old numbering) (online self-service ordering).

## DD source / evidence
Nobody uses them in the pilot. The list is in scripts/build_mapping_md.py (DISPLAY) and marked 'move to C3 (online and kiosk; C4 in the old numbering)' in mapping/A1-field-mapping.md.

## Alternatives considered
Keep in A1: A1 grows by about a quarter with fields nobody tests in the pilot.

## Affects
A1-02.

## Note, 22.09.2026
This decision assumes `new_tour_type` maps to Bundle. Doc 2 V9 says the Bundle is not the activity type (knowledge/A1-binding-rules.md, F1). Settle F1 first.

## History
- 16.09.2026: proposed.
