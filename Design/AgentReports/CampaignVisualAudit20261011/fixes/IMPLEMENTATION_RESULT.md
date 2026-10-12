# Campaign visual fixes — implementation and verification

Status: visual implementation and final native opening review complete for all 25 missions in the isolated T7 checkout. Full normal-input mission and Android acceptance remain pending. Original audit findings and failed attempts remain available in the parent report.

## Changes

- Finished road/sidewalk joins and terminals on the 11 existing Campaign sources. Graded 1,222 legacy DirtRoad mesh instances where raised render geometry intruded into asphalt and curbs. Protected placement, collision and ownership checks run around source mutations.
- Connected civic hall and bazaar paving, added organized market stock and mission-specific destination dressing, and kept required gameplay lots clear.
- Finished assembled runway ends and shoulders; removed false internal end bands and cleared decoration from the runway envelope.
- Added ground variation with matching rotated normal sampling, grouped loose landscape props, and finished visible canal ends.
- Assigned lighting ownership to the loaded authored source when it has an enabled daylight key.
- Framed opening actors or the actual first construction/recruitment location above the command controls. First Contact needed a camera-only margin adjustment. Power Relay intentionally focuses the family convoy selected by its first guide step.
- Repaired required functional placements in Supply Line, Steel Push and the affected Chapter 5 missions. Reconciled transferred presentation ownership with strict readiness contracts.
- Added a narrow Campaign AI build preflight to skip entries the mission catalog cannot admit. Required starting grants continue through their existing placement boundary.

## Verified evidence

The latest packed rebuild passed inventory, source parity and exact repeated-build determinism. It contains 124,305 source rows, 59,501 eligible rows, 64,804 resident rows, 38,909 placements, 7,403 slots and 9,514 prototypes. The final source hash is `bc250a60f2fb3050c691265273df13a7bea3fa2420fe4bef1a58debad75d2ace`; packed content hash is `746887b5f55cd8b642c61cecb5aa573aa40a0817caa254a7f2120823263b72de`.

Road mesh sharing reuses 245 exact serialized payloads across 1,222 instances, changing 977 references. All original generated assets are retained. This reduces duplicate referenced meshes; no device memory or frame-rate improvement is claimed.

Supply Line regression checks passed (57 tests and 14 rules groups covering objectives, settlement, hauling and resources). Focused camera tests passed (3), and Campaign AI eligibility plus generic planner regression checks passed (7 planner tests). Source previews cover all 11 graded road sources. See [final 25-mission evidence verification](final-verification.json), [progress and full logs](PROGRESS.md), [road reconciliation](road-entry-reconciliation.json), and [native gallery](gallery.html).

## Evidence limits and pending gates

All 25 missions now have completed native captures with zero failed required placement requests. All 25 final receipts reflect the completed road-entry correction. The gallery and native-review.json record each completed capture and its manual review separately. Captures deploy through the native menu with an isolated profile and narrative Skip, using PC/Metal at 1600 × 900. Capture pass markers establish image generation and inspected startup state, not mission completion. Complete normal-input mission/ARIA/result/return playthroughs and real Android acceptance remain pending.

The first road-entry wrapper timed out; its late underlying pass is retained as failed wrapper evidence. The subsequent wrapper invocation passed. Earlier failed framing attempts and the invalid cross-load object-identity hash assertion remain recorded, with their corrections. Persistent source hashes and the packed parity ledger provide cross-run source evidence.

The changes are prepared on `codex/campaign-visual-audit-20261011`; Git history records integration. Main-checkout synchronization is coordinated with the active mission agent to preserve its work.

## Source-control checks

Authored C# and shader patches pass the staged diff check. The full staged check reports 7,232 trailing spaces on Unity-serialized empty fields in metadata/materials; the raw warning log and classification are retained. No authored-code whitespace errors were found. Local Pipeline port and dynamic fallback-font cache changes are excluded. Isolated test profiles and scratch source copies are retained locally and ignored by Git.
