# M03 Radar Warning — Implementation and Acceptance Tracker

Date: 2026-09-08

Status: Planning only; implementation not started

Progress: **0/48 implementation items accepted**

Product: [Detailed M03 production plan](../M03_Radar_Warning_Production_Plan.md)

Architecture: [M03 technical architecture](m03_radar_warning_technical_architecture.md)

Catalog scope: [Every unit identity and class](../M03_Radar_Warning_Class_Coverage.md)

This tracker turns the requested comic, cinematics, English/Persian, classes, tutorials, guide, ARIA, fun, and architecture into reviewable deliverables. The planning documents themselves do not count as implemented features. Every item below remains unchecked until its stated evidence exists.

## Execution rules

1. Re-read current source and the latest accepted M01/M02 findings before editing. The M02 tracker supersedes older M02 prose where owner corrections changed the product.
2. Follow repository `AGENTS.md` for every Unity operation. Keep Hub open/signed in and use the macOS wrapper for validation/capture. Do not invoke the Editor directly, pass batchmode, reset IPC, or stop existing Unity processes.
3. Resolve exact file ownership and source-growth ceilings before each mutation. Preserve unrelated working-tree changes, including the two font assets present when this plan was written.
4. Extend existing mission/build/production/combat/camera/UI/persistence owners. Add only the bounded missing seams identified in the architecture.
5. Run focused tests at the changed boundary plus affected regressions. Re-run broader suites when shared changes justify them, not after every copy-only adjustment.
6. Create a playable, reviewable provisional experience before final comic/voice production. Media acceptance is a dependency on stable gameplay and copy, not a reason to postpone writing storyboards now.
7. Record the exact commit/content hashes, environment, invocation, pass marker, outcome, captures and remaining limitations for acceptance. Never count a deferred device run as passed.
8. Planning does not authorize implementation, publication, a device build, or a source-growth exception. These are future work items for when M03 execution is requested.

## G0 — Contract and feasibility

| ID | Depends on | Deliverable | Acceptance / evidence required |
|---|---|---|---|
| [ ] **M03RW-001** | — | Snapshot current M01/M02/M03 data, code owners, dirty files, package versions, accepted camera/result behavior and test entry points. | Compact inventory with exact paths; distinguish existing, partial, proposed and obsolete sources. Confirm M02's non-combat flow and comic-before-Victory ordering. |
| [ ] **M03RW-002** | 001 | Reconcile M03 rules, class exposure, loaned unlocks, finite convoy, guide scope and full versus fallback radar. | One decision table; no contradiction between chapter, new plan, player copy and runtime goals. Preserve later permanent gates and M04 scope. |
| [ ] **M03RW-003** | 001 | Exact ownership/rollback/test matrix and source-growth plan. | Every task family has explicit touched paths, read-only assets, existing line/byte ceilings and applicable regressions; no wildcard permission or budget increase. |
| [ ] **M03RW-004** | 002,003 | Map feasibility probe for the post, clinic, ground sensor, two defense positions, fork and real contact distances. | Actual grid/renderer bounds, sightlines, vehicle footprints, weapon ranges and camera sweep fit. Accepted physical-source hashes unchanged. No guessed coordinates promoted to assets. |
| [ ] **M03RW-005** | 002,003 | Canonical roster and capability manifest for all participating classes. | Resolve actual prefab/config IDs, faction compatibility, squad quantity, armed/unarmed vehicles, Ground Radar Tank and Air-only dish. Reference-only/naval classes never appear deployable. |
| [ ] **M03RW-006** | 004,005 | Real transaction cost matrix and initial balance proposal. | Demonstrate tower/barrier and reinforcement/reserve plans are affordable with actual four-member production cost; documented sensor-loss recovery and no hidden Fuel failure. Fix budget before tutorial copy freezes. |

G0 exits when the remaining work is implementable from exact current-head contracts. Recommended sensor resolution is a loaned existing Ground Radar Tank. A satellite dish must not be relabeled as a Ground sensor while its actual enum value is Air.

## G1 — Playable defense with authoritative truth

| ID | Depends on | Deliverable | Acceptance / evidence required |
|---|---|---|---|
| [ ] **M03RW-007** | 002,003 | Default-safe defense contracts, rule enums and no-post-damage star semantics. | Existing serialized values preserved; zero/invalid enums rejected; primary-loss and simultaneous-event rules tested; M01/M02 definition regressions pass. |
| [ ] **M03RW-008** | 005–007 | Deterministic mission/scenario assets with finite convoy elements, loaned access, post and civilian roles. | Exact counts, initial resources, schedule/contact time, guidance keys and reward IDs resolve; legacy singular-wave defaults preserved. |
| [ ] **M03RW-009** | 004,008 | Logical convoy-approach map and camera/route/build anchors via Editor builder. | Stable physical binding; grounded spawns, valid build areas and route/inner-core geometry. Two builder passes stable; physical map unchanged. |
| [ ] **M03RW-010** | 007–009 | Catalog projection and consolidated data validation. | M01/M02/M03 resolve by exact identity; duplicate/stale/missing entries fail closed; data projected once with correct lifetime. |
| [ ] **M03RW-011** | 010 | Campaign and direct launch, readiness and attempt reconstruction. | Locked profile rejection, M02-cleared profile entry, retry/replay, rapid double deploy and missing map all behave correctly. No M01 fallback. |
| [ ] **M03RW-012** | 011 | Existing wave owner supports vanguard/main-body warning and activation once. | Expected required members exist; preactivation combat/movement/detection/minimap suppression coherent; no early victory; M02 remains non-combat. |
| [ ] **M03RW-013** | 009,012 | Convoy path orders and barrier interaction through existing movement/breach owners. | Armed car fires normally, APC never shoots, both elements reach intended contact lines; blocked road has real alternate/breach behavior; no deadlocks or scripted teleport damage. |
| [ ] **M03RW-014** | 012,013 | Actual convoy/post/civilian/inner-core facts. | Correct living boundary entrants, no fake missing-entity kills, dead wreck on boundary safe, post damage monotonic, group and attempt isolation. |
| [ ] **M03RW-015** | 007,014 | Existing runtime/objective owners resolve defense victory and defeat. | All required enemies defeated and post/core safe for victory; post loss/breach causes defeat; simultaneous-event priority stable; tutorial clicks cannot complete objectives. |
| [ ] **M03RW-016** | 015 | Independent three-star projection and diagnostic result truth. | Victory, clean post, civilian loss and tower damage combinations tested; best stars only on victory; exact failure reason and relevant retry topic. |
| [ ] **M03RW-017** | 006,013–016 | Graybox balance/recovery proof. | Two strategies win; initial-warning/contact window feasible; sensor loss/Ping skip/ordinary positioning error recoverable; idle does not reliably three-star. Capture representative manual play, not only scripted tests. |

G1 can use a clearly labeled scout warning while the full threat pipeline is developed. It cannot close Radar Ping or functional radar acceptance through this fallback.

## G2 — Warnings, Radar Ping, guidance, guide and presentation

| ID | Depends on | Deliverable | Acceptance / evidence required |
|---|---|---|---|
| [ ] **M03RW-018** | 007,012 | Source/confidence/route/contact-time/attempt warning contracts and legacy adapters. | One definition of source, unknown strength, actual contact ETA, freshness, focus and resolved state; legacy producers remain compatible. |
| [ ] **M03RW-019** | 018 | One canonical warning resolver with bounded ledger and separate presentation acknowledgements. | Script/sensor merge, simultaneous elements, deterministic priority, expiry and critical retention; popup dismiss cannot destroy active threat truth. |
| [ ] **M03RW-020** | 005,019 | Detector integration, pause retention and information filtering. | Ground source detects actual eligible threats; Air-only sensor fails Ground request; pause freezes warning rather than clearing it; no suppressed enemy/minimap leak. |
| [ ] **M03RW-021** | 019,020 | Real POP-01/HUD/minimap warning binding. | Source, severity, ETA, confidence, route and known strength agree; Jump uses the exact warning target, preserves selection, returns safely, and has a truthful unavailable state. |
| [ ] **M03RW-022** | 020,021 | Full two-charge/60-second proposed Radar Ping transaction and UI. | Owned/loaned gating, sensor/faction/coverage, cooldown, duplicate/stale requests, empty scan, sensor death and retry reset tested. Valid empty scan consumes once; invalid request consumes nothing. |
| [ ] **M03RW-023** | 013,021 | Manual/assistant command capability parity for every M03 active class. | Rifle and Radar Tank Move/Hold/Stop; unarmed sensor cannot attack; enemy/civilian requests reject; no UI-only success. Confirm actual Stop/auto-engage semantics before narration. |
| [ ] **M03RW-024** | 018,023 | Twelve typed guidance steps and authoritative completion predicates. | All IDs/keys/targets resolve; explanation acknowledgements separated from accepted commands and finished actions; completed/alternative actions skip unnecessary steps. |
| [ ] **M03RW-025** | 024 | Full Guidance journey using real warning/build/production/command surfaces. | First-time player can finish without hidden instructions; contact preempts optional steps; no mandatory optional purchase/Ping or locked-step softlock. |
| [ ] **M03RW-026** | 024,025 | Contextual, Minimal, replay and muted-voice behavior. | Critical warning information always available; 30-second escalation bounded; no repeated unchanged narration; replay tutorial preference respected. |
| [ ] **M03RW-027** | 022–026 | ARIA SHOW ME / bounded DO IT / override behavior. | Only explicit current action executes; cancellation on input, stale target, pause/result/exit; resource/charge rejection truthful; no strategic autopilot. |
| [ ] **M03RW-028** | 021,024 | Twelve-topic field guide and complete class reference projection. | Correct category/availability, real numeric data, short explanations, relevant help entry, explicit pause and exact return context; no future-class deploy claims. |
| [ ] **M03RW-029** | 024,028 | Provisional complete `en`/`fa-IR` key coverage and live locale switching. | Central catalog only; placeholder parity; all warning/guide/result/reward/error keys present; no runtime asset-wide rebuild side effects. |
| [ ] **M03RW-030** | 009,011 | M1/M2-style opening: RTS overview → simultaneous smooth zoom/pan to key areas → original RTS pose. | Match reference behavior through existing request path; capture each transition and return, safe camera bounds, no immediate focus snap, comic handoff, skip and reduced motion. Combat/ETA clock starts after return. |
| [ ] **M03RW-031** | 021,030 | Player-requested warning/ARIA focus and camera arbitration. | No forced combat cutaway; drag/command cancels; selected units/orders preserved; missing anchor safe; camera returns to the valid preceding tactical context. |
| [ ] **M03RW-032** | 016,030 | Outcome-true optional finale and robust camera cleanup. | Damaged/clean post framing, no celebration on defeat, result independent of media; skip/abort/pause/World teardown restore camera/input exactly once. |
| [ ] **M03RW-033** | 010,016 | Idempotent first clear/replay rewards and M04 availability. | Real XP/Credits/Tower/Ping grants; duplicate-preowned conversion only where valid; no repeat grants; save failure visible; M04 unready deploy stays disabled. |
| [ ] **M03RW-034** | 029,033 | Campaign → briefing → match → debrief comic → Victory → menu, with correct retry/replay branches. | Preserve latest M02 result ordering; freeze/settle outcome before presentation; archive replay cannot write progression. |
| [ ] **M03RW-035** | 024,029,030,032,034 | Provisional three-sequence narrative, seven-panel storyboard integration and outcome line conditions. | M02 continuity, no premature later-story revelation, truthful source/strength, no clean-post/civilian-safe claim after losses, M04 extraction hook. |
| [ ] **M03RW-036** | 017,022,025–035 | Complete playable slice acceptance before final media. | Manual first-time and expert runs in both languages; capture choice→consequence and recovery; fix action/timing/route/UI defects; record remaining balance hypotheses. |

## G3/G4 — Final comic, cinematic timing, two languages and audio

| ID | Depends on | Deliverable | Acceptance / evidence required |
|---|---|---|---|
| [ ] **M03RW-037** | 036 | Seven final panels, fourteen aspect crops, focal/safe-area metadata and outcome-safe treatment. | Accepted character/world identity, no baked text, no clipping/false result art; in-game 16:9/20:9 captures and residency metrics. |
| [ ] **M03RW-038** | 036 | Editorially locked English and Persian story/tutorial/guide/glossary copy. | Fluent Persian review, actual command/radar claims, stable keys/placeholders, names from speaker catalog, evidence and outcome branch correctness. |
| [ ] **M03RW-039** | 037,038 | Both-language narrative voice, captions and manifest. | Correct established speakers, approved text hashes, actual durations, no stale spoken ETA, skip/replay/locale-switch cleanup and listening QA. |
| [ ] **M03RW-040** | 038 | Both-language tutorial/ARIA voice and warning priority. | Required step/critical lines match current actions; narration modes and interruption work; no M01/M02 substituted clip or repeated popup speech. |
| [ ] **M03RW-041** | 030–032,039,040 | Final cinematic and audio timing pass. | RTS start/zoom-pan/key-area holds/return remain smooth and readable; pan and zoom move together; speech unobscured; optional focus never steals combat control. |
| [ ] **M03RW-042** | 037–041 | Localization/accessibility visual and audio acceptance. | Both locales × both aspects × normal/large text; shaping, mixed-script numbers/names, reduced motion, silent mode, live switch, warning/guide/result safe areas and full key coverage. |

## G5/G6 — Regression, fun-factor proof and release evidence

| ID | Depends on | Deliverable | Acceptance / evidence required |
|---|---|---|---|
| [ ] **M03RW-043** | 032–035,042 | Consolidated M03 correctness plus affected M01/M02/Skirmish regressions. | All architecture/rules/warning/Ping/guide/commands/media/settlement tests pass at the same source state; required pass markers and zero compiler errors retained. |
| [ ] **M03RW-044** | 043 | Lifecycle, retry/replay, interrupted navigation and persistence stress. | Initial ten-cycle leak probe plus M01→M02→M03→Skirmish isolation; no stale warnings/requests/voice, leaked handles, repeated grants or lost camera/input. |
| [ ] **M03RW-045** | 043,044 | Editor frame, GC, detector/pathfinding, media memory and source-growth evidence. | Current tracked thresholds pass without relaxation; 0 B/frame target for added hot work; actual steady-state metrics separate from capture overhead; all applicable ratchets satisfied. |
| [ ] **M03RW-046** | 036,042,045 | Final fun/fairness and bilingual player-learning rounds. | Report sample/cohorts, first-time outcomes, two strategies, warning comprehension, recovery and replay interest; adjust and revalidate implicated content when findings require it. |
| [ ] **M03RW-047** | 043–046 | Editor-complete handoff, documentation reconciliation and reproducible captures. | All promised Editor scope implemented; evidence index and known limits explicit; gameplay, media, guide, rewards and source docs agree; device gate reported separately. |
| [ ] **M03RW-048** | 047 and release/device scheduling | Android/Samsung release evidence if this lane is authorized and scheduled. | Real device touch, fonts, audio, thermal/memory/frame acceptance through approved workflows. Otherwise mark explicitly deferred; never check it as passed from Editor evidence. |

## Dependency graph and work sizing

Critical path:

```text
001 → 002/003 → 004/005 → 006/007 → 008/009 → 010/011
    → 012/013/014 → 015/016/017
    → 018/019/020/021/022 → 024/025/026/027/028/029
    + 030/031/032 + 033/034/035
    → 036 → 037/038 → 039/040/041/042
    → 043/044/045/046 → 047 → 048 if scheduled
```

The tables, not abbreviated graph arrows, define exact dependencies. Once a shared contract is stable, map authoring, storyboard preparation, localization drafts, and test-case authoring are independent work packages. This describes scheduling opportunities; it does not start agents or create tasks automatically.

Planning effort ranges below are estimates of focused person-days, not calendar promises or commitments. They assume reuse seams are viable, current assets are available, and a reviewer can exercise the build. They exclude approval/availability waits and device lab scheduling.

| Package | Estimate | Main uncertainty |
|---|---:|---|
| Contract, current-head audit, map/roster/cost feasibility (001–006) | 2–4 | M02 cost/quantity and M03 map geometry. |
| Authoring, playable convoy, rules and recovery (007–017) | 4–7 | Vehicle/barrier path behavior and real combat pacing. |
| Warnings, sensor/Ping, ARIA, guide, locale, camera and result integration (018–036) | 8–13 | Warning ownership migration, pause semantics, shared helper ceilings. |
| Final panels, two-language edit/voice and presentation (037–042) | 4–7 | Art revisions, Persian review and established voice production availability. |
| Regression, lifecycle/performance, playtests and Editor handoff (043–047) | 3–5 | Shared regressions and observed fun/performance findings. |
| Device release work (048) | 2–4 after Editor acceptance | Actual device/build lane and observed issues. |

Estimated Editor scope is **21–36 focused person-days**, plus a planning contingency of about 25% for shared-system gaps (roughly 27–45 person-days total). These are deliberately broad until G0 and G1 close. Re-estimate at G0/G1/G3 with measured work; do not treat media generation throughput as the same thing as engineering throughput.

## Evidence format and completion semantics

Each accepted item records:

```text
Task ID / status:
Source commit or exact uncommitted diff identity:
Exact changed and generated paths:
Contracts/decisions applied:
Validation entry point, wrapper invocation, log, pass marker and exit:
Behavior/capture evidence (locale, aspect, guidance, scenario/seed, phase):
Performance/memory evidence when relevant:
Unresolved findings or explicitly deferred gates:
```

Local logs may be in `/private/tmp`, but durable acceptance summaries and compact captures belong under the M03 evidence directory. Store enough information to reproduce a result without committing enormous transient logs. An automatic test verifies behavior; a human-visible capture/listening/playtest verifies presentation and fun. Neither substitutes for the other.

“Editor complete” means 001–047 are accepted at a consistent reviewed state. “Release complete” also requires the device evidence appropriate to the release. If 048 is deferred, report 47/48 with the deferral or separate 47/47 Editor scope and 0/1 device scope; never report an unqualified 100% complete.

## Decision log

| Date | ID | Decision | Basis | Status |
|---|---|---|---|---|
| 2026-09-08 | M03-PLAN-01 | Plan Chapter 1 M03 Radar Warning in English and Persian. | User M3 request; current mission and locale identities. | Planning assumption, clearly stated. |
| 2026-09-08 | M03-PLAN-02 | Use latest accepted non-combat M02 baseline and comic-before-Victory result order. | Latest M02 tracker corrections, 2026-08-30. | Existing behavior to preserve. |
| 2026-09-08 | M03-PLAN-03 | Start at RTS camera, smoothly zoom/pan to important areas, then return to RTS through existing M1/M2 owners. | User's explicit camera clarification in this task. | User direction; required. |
| 2026-09-08 | M03-PLAN-04 | Loan the existing Ground Radar Tank; preserve the satellite dish's Air role. | Current enum and both Unity config assets. | Recommended implementation decision; prove live coverage. |
| 2026-09-08 | M03-PLAN-05 | Keep Tower/Barrier/Ping access mission-scoped until canonical rewards/gates apply. | Existing Chapter 1 rewards and Chapter 2 barrier gate conflict. | Proposed scope resolution. |
| 2026-09-08 | M03-PLAN-06 | Cover every unit class in the guide/reference while teaching the small M03 roster. | User asks all classes; Chapter 1 teaching/feature-exposure contracts. | Proposed scope interpretation. |
| 2026-09-08 | M03-PLAN-07 | No source-growth exceptions or performance-budget increases in this plan. | Current repository architecture contracts. | Required default. |

No runtime implementation, gameplay acceptance, final art/voice production or Unity validation has been performed by writing this tracker.
