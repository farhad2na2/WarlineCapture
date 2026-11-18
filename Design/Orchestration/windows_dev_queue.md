# Windows development queue

The Mac program lead edits this file. The Windows dev lead reads it (from `origin/missions/integration`), works
tasks in order, and never edits it. Rules: `WINDOWS_DEV_PROMPT.md`. Codex model `gpt-6.2-sol`, effort per task.
Status: `READY` (take it), `TAKEN by windows <date>` is written only in your results file, `DONE` / `HOLD` set by the Mac lead.

Background for every draft task: drafts live in `Tools/Missions/drafts/` and are checked with
`python -B Tools/Missions/check_draft.py <draft>` (a PASS is a draft preflight, not Unity validation) and
`python -B Tools/Missions/quickcheck.py --missions <ids>`. Read `Tools/Missions/README.md`,
`Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`, `Tools/CI/skirmish_catalog_diagnostics.md` and the template drafts named
below first. Row data (map source, objective, army A/G/C, start F/E, layout id, budget template) is in
`Design/Orchestration/missions.csv`. Siblings of one map + objective share the measured layout of their template and
differ by army and start; copy the template's measured geometry, never invent coordinates. Anything the template marks
as a candidate or unmeasured stays marked so.

## W1 — Desert Base Frontline Control siblings S008–S012 (2026-11-18)
status: READY
codex effort: medium
template: `Tools/Missions/drafts/s007-desert-base-fc-ground-field-candidate.json`
deliverable: one draft per row S008, S009, S010, S011, S012 (army and start from missions.csv), each passing
check_draft.py, plus one quickcheck run over S007–S012 at Standard. Branch `missions/win-w1-db-fc`.

## W2 — Desert Base Breakthrough siblings S014–S018 (2026-11-18)
status: READY
codex effort: medium
template: `Tools/Missions/drafts/s013-desert-base-bt-ground-field-candidate.json`
deliverable: as W1 for S014–S018. Branch `missions/win-w2-db-bt`.

## W3 — Desert Base Convoy Escort siblings S020–S024 (2026-11-18)
status: READY
codex effort: medium
template: `Tools/Missions/drafts/s019-desert-base-ce-ground-field-candidate.json`
deliverable: as W1 for S020–S024. Branch `missions/win-w3-db-ce`.

## W4 — City Crossroads Frontline Control S031–S036 (2026-11-18)
status: READY
codex effort: high
templates: the S007 FC draft (structure) and `s026-city-crossroads-ba-ground-established.json` (City Crossroads source,
bases and measured anchors).
deliverable: S031 first as the family template: three control points, routes and stagings placed only on anchors and
positions the S026 draft or a Unity layout dump already measured on City Crossroads (`Tools/Missions/layout_dump_to_draft.py`).
Where a needed position is not measured, mark it unmeasured and list it. Then S032–S036 as siblings. check_draft.py on all,
quickcheck S031–S036 Standard. In Unity on Windows: the compile check, then any SkirmishMissionAuthoringEntry diagnostics
method the catalog docs name for a draft (read-only; do not write assets). Branch `missions/win-w4-cc-fc`.

## W5 — Mountain Pass Frontline Control S055–S060 (2026-11-18)
status: READY
codex effort: high
templates: the S007 FC draft and `s049-mountain-pass-ba-ground-field.json`. Same method as W4. Branch `missions/win-w5-mp-fc`.

## Not yet (HOLD until the Mac lead decides)
- Industrial Basin (S079–S096) and Airport (S097–S120) rows: open source-fit decisions in `decisions_pending.md`.
- Counted ARIA runs on Windows (fps).
