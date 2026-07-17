# AM-WP-018 - Tooltip Ownership And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves every tooltip caller and whether the surface is required, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package. If no production tooltip behavior is proven, remove the unresolved surface contract instead of inventing one.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-019`.

## 1. Current Ownership And Risk

- The shell prefab contains a `TooltipLayer`, but no dedicated runtime tooltip request, projection, source/version, presentation owner, or production C# caller was found in the audited UI paths.
- No accepted contract defines whether tooltip activation uses hover, long press, focus, controller navigation, accessibility narration, or programmatic warnings.
- Target identity, anchor ownership, priority, dismissal, safe-area placement, localization, and input-modality transitions are undefined.
- Treating the empty layer as implemented would hide a feature gap; attaching ad hoc listeners later would create duplicated input ownership and retained target/view references.

Risks are inventing unsupported UX, stale tooltips over replaced screens, pointer/focus capture leaks, per-frame placement polling, accessibility conflict, unbounded text/layout work, and measuring an absent implementation as a zero-allocation pass.

## 2. Accepted Future Ownership

- `AM-026` first proves whether Tooltip is a required production surface and inventories all code, serialized, accessibility, and input callers. With no proven caller/design contract, the accepted change is contract removal only.
- If retained, one shell-scoped tooltip presentation owner consumes immutable requests from explicit view/input adapters. Feature views own tooltip content/source identity; they do not instantiate competing tooltip roots.
- Input and accessibility owners define activation, delay, modality, focus, narration, and dismissal behavior before implementation. This package does not invent those policies.
- At most one visible tooltip exists. Deterministic priority resolves competing requests without mutating source features.
- Placement is event/invalidation driven by target geometry, safe area, orientation, scale, and shell layout changes; no always-on per-frame polling is allowed.
- Hidden state performs zero input subscription beyond the proven shared input adapter, formatting, layout, ECS, or recurring managed work.
- Route/popup/root/World replacement, target destruction/disable, modality change, focus loss, and application pause clear ownership idempotently.

No `SystemBase`, default-World lookup, view-local polling, global mutable tooltip authority, broad manager/controller/provider/service type, scene/prefab edit before explicit serialized-owner handoff, or visual/accessibility policy invention is allowed.

## 3. Identity And Invalidation Contract

Minimum identity if Tooltip is retained:

- shell root, route/popup, and binding generations;
- request sequence, source feature, target instance, and target-lifetime generation;
- content/localization/accessibility versions;
- anchor geometry, canvas scale, safe-area, orientation, and layout generations;
- input device/modality, pointer/focus/contact identity, and activation generation;
- priority, dismissal reason, and explicit invalidation generation.

Rules:

- A request is valid only while its exact target and shell identity remain active.
- Equal request/content/geometry identity produces zero formatting, TMP write, layout rebuild, or placement apply.
- Competing requests resolve by an approved stable priority and sequence; late dismissal from an old target cannot close a newer tooltip.
- Target disable/destruction, route change, popup replacement, root replacement, focus loss, pause, modality change, and World replacement invalidate once.
- Long-press/hover timers, if approved, are bounded and cancelled with their exact pointer/focus identity.
- Hidden state retains no target object beyond the accepted request lifetime and performs no recurring work.
- Tooltip text and narration share the approved localization/accessibility source; UI does not synthesize gameplay meaning.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- the exact existing shell view/root file proven to own `TooltipLayer`; path must be appended by `AM-026` before dispatch
- one narrowly named tooltip request contract under `Assets/Game/Scripts/UI/Contracts/`
- one narrowly named shell tooltip presentation view under `Assets/Game/Scripts/UI/Shell/`
- exact proven feature-view/input-adapter callers appended after owner handoff
- existing shell lifecycle files only when needed for root/route/popup invalidation and explicitly appended before dispatch

Test files allowed:

- existing shell/input/accessibility tests whose contract genuinely changes
- `Assets/Tests/Editor/TooltipOwnershipLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/TooltipAllocationValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_018_tooltip_ownership_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_018_tooltip_ownership_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: `UIShellAppCanvas.prefab`, visual-lock assets, input/accessibility contracts, localization, safe-area/layout ownership, and feature content.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, gameplay, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and any tooltip visual, timing, input, narration, or content-policy change without explicit owner approval.

This package is serialized with other packages claiming shared shell lifecycle files.

## 5. Characterization Matrix

Required before edits or contract removal:

1. every code/serialized caller and every route/popup containing a tooltip-capable target;
2. touch, mouse, keyboard, controller, accessibility focus, and programmatic activation only where current behavior/design proves them;
3. target active/disabled/destroyed, route/popup/root replacement, World replacement, pause/resume, and focus loss;
4. short/long/empty/localized/right-to-left text and missing content;
5. center/edge/corner anchors, safe-area changes, orientation changes, canvas-scale changes, and overlapping popup/scrim layers;
6. competing requests, stale dismissals, repeated enter/exit or press/release, multi-touch, and modality switching;
7. hidden idle, visible unchanged, content changed, anchor changed, and `100` bind/show/hide/replacement cycles.

Record listeners/timers, requests, accepted/rejected identities, retained target references, model rebuilds, managed bytes, TMP writes, layout/placement applies, narration events, and CPU time. If no caller exists, record the proof and removal impact instead.

## 6. Baseline And Acceptance Gates

If retained, measure `180` warmup plus `300` hidden-idle frames and `300` visible-unchanged frames for every approved input modality; activation, movement/layout invalidation, content change, dismissal, and lifecycle replacement are measured separately.

Acceptance:

- exactly one shell tooltip owner and one visible tooltip exist, or the unused layer/contract is explicitly removed with caller proof;
- activation, timing, priority, modality, narration, and dismissal match approved input/accessibility/design behavior;
- hidden idle performs zero recurring production-owned managed allocation, polling, formatting, TMP, layout, placement, timer, or ECS work;
- visible unchanged performs zero recurring managed allocation, formatting, equal-value write, or placement apply;
- stale requests/dismissals cannot affect newer targets, and no tooltip survives target/route/popup/root/World invalidation;
- `100` lifecycle cycles leave zero duplicate listeners/timers, retained targets, stale views, or pointer/focus capture;
- edge placement remains within the approved safe area without per-frame correction;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, proven callers/policies, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits after caller and UX-policy characterization:

1. caller decision: remove the unused surface or establish the narrow request/identity contract;
2. one event-driven shell presentation owner with deterministic priority and placement invalidation;
3. input/accessibility lifecycle and allocation acceptance.

Rollback if tooltip behavior is invented, targets leak, stale requests win, input/focus is captured incorrectly, accessibility behavior regresses, hidden/unchanged work remains, or the slice introduces `SystemBase`, polling, global mutable authority, protected-file edits, or non-allowlisted overlap.
