# AM-004 Core Architecture Scorecard

AM-004 freezes how the Core Architecture Lane is scored and measured. It does not recompute the architecture rating, certify a release, or change production behavior.

## Baseline

| Field | Value |
|---|---|
| Commit | `76f80c7a23b06ba6719593cb5f2815e476db7987` |
| Tree | `af237ee62e29e8d5191d4e3051451b6b47ab1712` |
| Branch at capture | `codex/am-004-scorecard` |
| JSON schema | `1` |
| Core score state | `not-recomputed` |
| Historical context | approximately `8.8 / 10`; not a current score |
| Release lane | deferred, inactive, and `measurement-required` until `AM-053` |

## Rating Model

| Core category | Closeout context | Weight | Target | Current state |
|---|---:|---:|---:|---|
| ECS and runtime ownership | `9.2` | `20%` | `9.6+` | current evidence required |
| Modularity and dependency boundaries | `8.5` | `15%` | `9.4+` | current evidence required |
| UI and presentation architecture | provisional `8.2` | `10%` | `9.4+` | current evidence required |
| Lifecycle and resource safety | `8.6` | `15%` | `9.6+` | current evidence required |
| Maintainability and testability | `8.7` | `15%` | `9.5+` | current evidence required |
| Performance and GC discipline | `8.2` | `15%` | `9.7+` | current evidence required |
| Diagnostics and continuous governance | measurement required | `10%` | `9.5+` | measurement required |

The current Core score remains `null` until all seven categories have current evidence. It is then the weighted mean rounded to one decimal. A `9.5+` claim additionally requires every category to be at least `9.0`.

Score anchors retain the tracker meanings: below `5` is unsafe or undefined; `5-6` is substantially manual; `7` is correct in major flows but incompletely proven; `8` is broadly automated and measured with bounded gaps; `9` is fail-closed and current with no material uncovered path; `9.5` adds independent review and broad failure/lifecycle/performance evidence; `10` sustains that standard across multiple production releases.

## Core Budgets

| Budget ID | Limit | Window | State |
|---|---:|---|---|
| `CORE-EDITOR-FRAME-P95` | p95 `<= 20 ms` | at least `180` frames, `700` units, `600` runtime buildings, visible-model estimate `40` | active; unmeasured at this baseline |
| `CORE-MATCH-GC-GLOBAL` | `<= 1,024 bytes` | `180` warmup + `300` measured frames | active; unmeasured at this baseline |
| `CORE-OWNER-FOCUSED-RECURRING-GC` | exactly `0 bytes` | focused unchanged state, `180` + `300` frames | active; unmeasured at this baseline |
| `CORE-UI-UNCHANGED-STATE-GC` | exactly `0 bytes` | surface open, fully bound, unchanged, `180` + `300` frames | active; unmeasured at this baseline |
| `CORE-UI-TRANSITION-GC` | exactly `0 bytes` after one registered warmup open/close | each repeated open and close reported separately | active; unmeasured at this baseline |

The AM-003 major-UI scenario's shorter `60`-frame warmup is not enough for scorecard acceptance. The scorecard minimum is `180` warmup frames and `300` measured frames; AM-005 must reject evidence using the shorter window.

p99 and maximum frame time are always reported, but AM-004 found no accepted numeric p99 authority. They remain evidence fields rather than invented pass thresholds.

## Scenario Bindings

Every AM-003 placeholder has one disposition. `Active` means the budget is frozen but still needs exact-baseline evidence; it does not mean passed.

| Budget ID | Frozen disposition |
|---|---|
| `AM004.AM003-SCN-001-IDLE-MATCH.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-001-IDLE-MATCH.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-001-IDLE-MATCH.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-003-CONSTRUCTION.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-003-CONSTRUCTION.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-003-CONSTRUCTION.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-004-TRANSPORT.command-time` | No accepted numeric authority; measurement required and fail closed |
| `AM004.AM003-SCN-004-TRANSPORT.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-004-TRANSPORT.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-005-AIRCRAFT.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-005-AIRCRAFT.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-005-AIRCRAFT.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-006-PROJECTILES.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-006-PROJECTILES.gc-allocation` | Active: global `<=1,024 bytes`; focused owner `0 bytes` |
| `AM004.AM003-SCN-006-PROJECTILES.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.frame-time` | Active: Editor p95 `<=20 ms` |
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.gc-allocation` | Active: unchanged-state `0 bytes`, `180` + `300` frames |
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.memory-growth` | Policy frozen: no positive post-warmup slope; measurement required |
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.gc-allocation` | Baseline-relative transition ratchet; measurement required and fail closed |
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.memory-growth` | No positive post-warmup slope and return to accepted tolerance; measurement required |
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.transition-time` | No accepted numeric authority; measurement required and fail closed |
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.gc-allocation` | Baseline-relative transition ratchet; measurement required and fail closed |
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.memory-growth` | No positive post-warmup slope and return to accepted tolerance; measurement required |
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.transition-time` | No accepted numeric authority; measurement required and fail closed |
| `AM004.AM003-SCN-010-LONG-SOAK.frame-time` | Inactive release-lane measurement required |
| `AM004.AM003-SCN-010-LONG-SOAK.gc-allocation` | Inactive release-lane measurement required |
| `AM004.AM003-SCN-010-LONG-SOAK.memory-growth` | Inactive release-lane measurement required |

## GC Classification

| Class | Acceptance |
|---|---|
| `player-relevant` | At most `1,024 bytes` over the canonical post-warmup window |
| `owner-focused` | Exactly zero recurring bytes in focused unchanged state |
| `one-time-warmup-cache` | Allowed only before measurement; registered owner, trigger, capacity, lifetime, and post-warmup stability are mandatory |
| `transition-only` | Reported separately and compared with an accepted non-loosening transition baseline |
| `unity-editor-instrumentation` | Excluded only with attribution and a paired instrumentation-off or Player control |

Samples are retained. Classification changes attribution and gate ownership; it never deletes unfavorable evidence. Unknown classification is invalid.

## Lifecycle And Memory

Lifecycle evidence uses `10` warmup cycles followed by `100` measured Menu -> Match -> Menu cycles. Entity, native-container, pool active/capacity, scene-root, and subscription counts are sampled every cycle. The final `20` measured cycles are the plateau window: count tolerance is zero and no monotonic growth is allowed. No one-time cache is pre-approved; an unregistered cache invalidates the result.

Managed, native, graphics, and pool memory are sampled every `5` cycles plus before and after each transition. A single before/after pair is invalid. The last `20` measured cycles form the plateau window, and the architectural target is no positive retained-growth slope (`<=0 bytes/cycle`). Managed memory must return within the larger of `1 MiB` or `1%` of its warmed baseline; native memory within the larger of `4 MiB` or `1%`; pool memory has zero tolerance. These are frozen policy limits and remain unmeasured at this baseline. An unknown tolerance is invalid. Graphics residency remains release-only and `measurement-required` until `AM-053`.

## Freshness

Only `current` evidence can pass. `invalid`, `missing`, `stale`, and `unknown` fail closed.

- Evidence identifies the exact tested commit and AM-002 environment identity.
- Changes to governed source, configuration, scenario, or tool hashes stale the row.
- Dirty, malformed, unresolved, or commit-unknown evidence is rejected.
- Unrelated documentation-only changes do not force recapture when every governed hash and environment identity remain unchanged.
- A recorder with material overhead records the overhead or provides a paired instrumentation-off control.

The maturity tracker hash treats `Rating Model`, `Status Rules`, `Global Architecture Guardrails`, and `Core Evidence Contract` as an unordered heading set, scans the normalized UTF-8/LF source top-to-bottom, includes each selected heading through but excluding the next level-two heading, and concatenates included lines in source order without inserted separators. Mutable progress snapshots, checklist state, decisions, and implementation logs are excluded so recording AM-004 completion cannot invalidate its own evidence.

AM-005 must implement these content and environment checks; the older dashboard's revision-only freshness is insufficient.

## Release Lane

Android p95 authorities (`33 ms` recommended, `25 ms` high-end), APK (`463,359,198 bytes`), AAB (`426,399,778 bytes`), and the same-device peak-memory reduction target (`10%`) are preserved. Absolute peak memory, installed size, startup p95, resource-memory category ceilings, and sustained-release governance have no accepted numeric authority.

All device-tier fields are `measurement-required-inactive` until `AM-053`. They are reported separately and are never averaged into the Core score.

## Exception Registry

`exception_registry.json` has zero qualified active temporary exceptions and zero performance/GC waivers. Admission requires `owner`, `rationale`, `measuredEffect`, independent `approval`, `expiry`, and `removalTask`; missing or expired records fail closed.

The audit distinguishes pre-existing policy records from temporary waivers:

- `139` exact-ceiling source-growth ratchet authorizations remain under their existing validator and authorize no growth beyond their line/byte ceilings. They lack the complete AM-004 temporary-exception metadata and are not treated as passes.
- `25` managed `SystemBase` inventory entries require AM-006 re-inventory and are not promoted into the active registry.
- The separately owned operation-map R&D authorization remains under its own tracker and is neither imported nor modified.
- No performance, GC, memory, package, or freshness waiver was found or created.

## Determinism And Scope

Both JSON artifacts use UTF-8, LF endings, two-space indentation, lexicographically sorted object keys, deterministic array ordering, and one trailing line feed. Unchanged baseline identities, authority hashes, and policy values must regenerate byte-identically.

The exact write allowlist is this Markdown file, `entry_scorecard.json`, and `exception_registry.json`. Production, operation-map, FirstLaunch, audio, and UI visual-lock files are excluded.
