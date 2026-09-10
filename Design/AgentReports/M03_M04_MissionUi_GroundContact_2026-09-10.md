# M3/M4 mission UI and road contact

Status: implementation complete; affected Editor regressions passed.

## Scope

Bring the field guide and mission action/status additions into the existing target-lock HUD design language. Correct the visible ground gap under M4 infantry and vehicles. Validate in the Editor; Android validation is outside the requested scope. Screenshots stay under `/private/tmp`, not in Design or Git.

## Findings

- The guide and action rows were authored with plain Image fills and minimal hierarchy. The current HUD already provides Oxanium typography, localized Noto Arabic, framed dark surfaces, shaded buttons, cyan accents, and reusable command icons.
- M4's `SM_Env_Road_01` surface is at 0.450 m at the starting APC and both transfer locations. Its whole-mesh bounds reach 0.813152 m. The old surface overlay applied that maximum everywhere, raising ground vehicles by 0.363152 m. The independent mesh-triangle audit also detects overlapping road pieces at different heights.
- Infantry also had a 0.1 m authored clearance above roots whose idle boots already sit roughly 0.022 m above the origin. The resulting constant gap was about 0.122 m, in addition to any road-height error.

## Implementation

- Shared mission styling is reapplied by the canonical M3 and M4 UI builders. Guide examples and mistakes have separate framed cards; active tabs have cyan indicators. Longer class dossiers retain scrolling.
- M4 exposes separate aboard, APC transfer, secure-area, and remaining-time counters. The countdown uses minutes and seconds. All captions and status changes use the existing English/Farsi catalog. Unchanged values avoid repeated string formatting.
- Ground support uses shared baked triangle geometry: 31 small assets (about 1.2 MB) serve 3,430 mesh placements in each canonical runtime scene. Bounds locate candidate meshes; transformed triangle intersection determines contact height and normal. Curbs no longer lift the entire road, and empty corners of mesh bounds do not become invisible platforms.
- Geometry assets contain contact data without retaining source render-mesh dependencies. The runtime surface entry retains its existing maximum size of 80 bytes by storing the legacy footprint's yaw as one angle. Persistent native caches supply Burst jobs; the steady-state sampler reads baked arrays without managed mesh API calls or physics casts. Missing exact geometry fails publication instead of silently reverting to the incorrect height.
- All 33 character prefabs have per-animation contact offsets measured from the GPU animation's deformed foot vertices. Grounding resolves the active animation's planted-foot baseline. Natural airborne run frames remain animated; the character is not pinned to the ground every animation frame.
- Guide cards resize around localized copy, mirror lesson ordering for Farsi, and expose a slim native scrollbar when content exceeds the reading area.
- M3 places its action panel below the existing warning strip, with a regression assertion that neither panel overlaps at 16:9 or 20:9.
- Both mission panels participate in the existing header's center-responsive targets, keeping their left edge aligned with the resource strip on widescreen layouts. Rebuilds remove stale target references and preserve the original header targets and base positions.
- Surface-cache ownership and spatial indexing are separated from the unit sampling job. The original system is reduced to 455 lines / 21,162 bytes; existing source-growth ceilings are unchanged. Prefab saves also refresh cached authoring mirrors from the existing authoritative unit configs; no unit balance configs changed.

## Completed evidence

- `/private/tmp/warline-road-contact-baseline.txt`: independent source-mesh audit of actual M4 road height versus the previous bounds maximum.
- `/private/tmp/warline-mission-ground-build-06.log`: rebuilt all three canonical map bindings, checked persisted geometry references, and passed six focused sampling/movement/spawn regressions; wrapper exit 0.
- `/private/tmp/warline-unit-locomotion-contact.txt`: independently sampled 127 idle/walk/run/run-aim clips across all 33 character prefabs. Planted feet remain within the regression tolerance (5 mm below / 25 mm above), with natural running lift recorded separately.
- `/private/tmp/warline-m04-ground-style-review-01.log` and `/private/tmp/warline-m04-ground-contact/state.txt`: full M4 Editor run passed. Includes English/Farsi HUD at 16:9 and 20:9, all 12 lessons and 57 classes in both languages, modal pause/resume, real APC/helicopter rescue, debrief, saved unlocks, bilingual results, campaign return, carrier-loss replay, clean retry, idle defeat and no duplicate rewards.
- M4 motion samples report zero root-to-corrected-surface discrepancy to six decimal places. The APC is at 0.450000 m on the audited road, rather than 0.813152 m. Moving vehicle captures and final guide/HUD captures were visually reviewed.
- `/private/tmp/warline-m03-ui-style-normal-01.log` and `/private/tmp/warline-m03-ui-style-large-01.log`: both M3 Editor UI runs passed with wrapper exit 0. Each covers real warning/guide/camera-return interactions, preserved selection, paused clock/health/positions, all 12 lessons and 57 classes in both languages, search/filter behavior, one resident class, both aspect ratios, and scrolling to the bottom. Normal and large Farsi guide captures were visually reviewed.
- `/private/tmp/warline-mission-ui-ground-regressions-01.xml`: 224/226 passed. The only failures were the existing line/byte growth ceilings for `UnitSurfaceTrackingSystem`; cache responsibility extraction resolved them without changing thresholds.
- `/private/tmp/warline-mission-ui-ground-regressions-02.xml`: 176/176 passed, wrapper exit 0, including all 139 tests in the nine core architecture fixtures and the affected surface/movement/ECS tests. Combined latest results are 226/226 unique affected tests passing; the other 50 tests already passed and their implementations did not change after the first run.
- Regression coverage includes both map-binding scene parity checks with persisted geometry/transform verification, 127 character locomotion clips, M3 presentation, M4 integration/rules, and resource text repair.
- `/private/tmp/warline-mission-ui-ground-regressions-03.xml`: final responsive alignment refinement passed all 18 presentation/integration/resource-text checks, including both mission panels' resource-header alignment and warning clearance at both aspect ratios; wrapper exit 0.
- `/private/tmp/warline-m03-ui-ground-final-smoke-02.log`: final M3 live launch passed after the cache/layout refinements, with all 20 members, the physical post, completed Barracks, returned camera and active simulation. The final widescreen Farsi HUD capture was visually reviewed; wrapper exit 0.

## Delivery hygiene

- Screenshot and motion evidence is local under `/private/tmp`; no evidence images are stored in Design or committed.
- Validation is Editor-only, as requested. These are scoped UI/ground-contact results; they do not supersede the unrelated voice/performance findings in the earlier M3/M4 completion report.
