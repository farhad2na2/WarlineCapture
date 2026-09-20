# ARIA implementation work packages

Updated: 2026-09-20. M1–M5 guided and both Skirmish Base Assault scenarios have EN/FA Editor baseline victories. See [current evidence](../../AgentReports/AriaWatchPlay/implementation-status.md). The checklists below describe the full release contract; baseline wins do not close their broader Operations, recovery, multi-seed, device and player-study requirements.

## Dependency order

`A0 → A1 → A2 → A3 → A4 → A5 → A6 → A7`

A0 inventories all modes immediately. A1 first proves input and cancellation. A2/A3 establish reusable observation/skills/presentation. A4 uses M1 to prove the complete loop and then completes M2–M5. A5 closes free-play strategy and Operations. A6/A7 are required before calling the player-facing feature ready across current modes. Do not stop the project at a successful M1 demo.

## A0 — baseline, routes and dependency audit

- [ ] Pin source/config hashes; read current architecture, input, UI and Unity execution contracts; preserve user saves and create isolated QA profiles.
- [ ] Complete the catalog/route inventory in [COVERAGE.md](COVERAGE.md), including replay and all supported Skirmish options. Trace each Operations action into its actual runtime/result path or record it as non-match/content-blocked.
- [ ] Record normal human touch paths for group selection, hold-drag, camera/pinch/minimap, command wheel, Build, placement, recruitment, exchange, transport and Continue. Identify all independent device/pointer readers.
- [ ] Inspect UI/EventSystem/input-module settings and installed Input System behavior. Produce a physical/synthetic pointer-identity and update-order design with an actual parity experiment.
- [ ] Inventory existing ARIA ownership/direct-command entry points, player-auto-AI settings, pending intents, narration priorities and scene lifecycle hooks; assign a single new ownership authority and migration path. Distinguish normal unit autonomy from an independent faction autoplayer.
- [ ] Capture current manual-play baseline for the affected flows and identify voice/localization reuse versus missing lines.

Exit: a complete inventory with named dependency owners, exact integration file/assembly list, shared input plan and normal-play regression baseline. Operations uncertainty must be resolved into records; it cannot simply disappear from the scope.

## A1 — touch execution and instant handback

- [ ] Add session generation/source identity and one touch executor behind narrow contracts. Restrict planner dependencies so it cannot acquire gameplay mutation interfaces.
- [ ] Implement tap, hold, drag, pinch and cancel through the normal touch path. Reconcile all primary-pointer readers and UI/world click capture; preserve manual control and multi-touch.
- [ ] Process genuine Stop/input before new synthetic samples. Cancel stale generation proposals and make in-flight planner results discardable.
- [ ] Implement genuine cancellation semantics in every affected UI/world handler; cancel must never become a click, placement confirmation, selection completion or camera jump.
- [ ] Disable legacy direct Do It/Give Control execution and drain/invalidate its pending work during Watch sessions. Keep manual Show Me functional without creating dual ownership.
- [ ] Handle Stop, physical takeover, pause, background, scene unload and result during every gesture phase; ensure UI cannot hide Stop behind a modal.
- [ ] Add gesture/input trace and focused parity/cancellation validation using real input events, not direct callbacks.

Exit: visible diagnostic contact marker and trace prove real touches reach the same handlers as physical input; all cancellation races pass. No planner or final hand artwork is needed to manufacture this proof. Do not proceed around an input issue with direct commands.

## A2 — public observations and mode goals

- [ ] Implement versioned UI observations after layout, including visibility, hit eligibility, modal state and clipped bounds.
- [ ] Implement visible world/minimap observations with ownership and occlusion/fog filtering; bounded remembered sightings carry age/uncertainty. Keep QA ground truth inaccessible to planning.
- [ ] Publish public goal semantics alongside the actual localized instructions and rules. Resolve missing player-visible prerequisites/feedback in the owning UI, benefiting human players too.
- [ ] Add read-only Campaign, Skirmish and Operations tactical adapters; establish capability/version manifests for every discovered playable row.
- [ ] Add invalidation on layout/camera/modal/goal/locale changes and reject stale gestures. Verify hidden or blocked controls never become actionable.
- [ ] Validate minimal visible values and precision; exclude hand/coaching overlay and internal mission targets from observations.

Exit: observation snapshots can explain their player-visible provenance; negative tests prove no hidden-target or mission-fact advantage. Unknown objectives become explicit unsupported states, never guessed direct actions.

## A3 — player shell, hologram and skill loop

- [ ] Build idle/confirmation/active/wait/recovery/handback states in the existing ARIA panel with full-width controls and content-sized readable text.
- [ ] Create the cyan holographic hand, exact fingertip contact/ring, hold/drag/pinch cues, edge behavior and reduced-motion option using [UX.md](UX.md).
- [ ] Keep one persistent Stop reachable across all gameplay panels; synthetic input cannot press consent/account/session controls.
- [ ] Implement bounded `Observe → Explain/Aim → Gesture → Verify → Recover` skills K01–K17 as needed; use current bounds and visible results rather than fixed screen/world positions.
- [ ] Add meaningful waits, two corrective attempts per stalled skill/target, cross-skill no-progress detection and safe handback reasons.
- [ ] Add local goal/tactical priorities and human pacing. Handle immediate threats without per-frame order spam or paused simulation.
- [ ] Add English/conversational Farsi copy, RTL, subtitle fallback and narration arbitration. Inventory/prepare any missing paid voice payload separately; use the existing authorization workflow for external generation.
- [ ] Keep manual yellow guidance coherent when entering/leaving Watch. Verify no duplicate old prompt, missing text, animated layout jump or result audio leakage.

Exit: every gesture and shell state can be demonstrated with actual input and inspected in EN/FA. M1's complete loop works in development but is not yet the all-mode release.

## A4 — Campaign M1–M5

- [ ] Complete C01 end to end at normal speed through public controls, including world outcomes, first play/replay and current story lifecycle. Prove Stop/restart-from-present-state and a changed camera/layout.
- [ ] Complete C02 construction, production and delivery; verify final recruitment advances to result without repeated Build prompting.
- [ ] Complete C03 legally placed gate, selection/deployment/Hold, relevant Scan and both convoy phases. Explain all waiting without inventing a countdown or asking for removed Stop.
- [ ] Complete C04 four-person selection/boarding/unloading, helicopter interaction, secure-zone wait and departure. Test overlapping units, mixed selection and full carriers.
- [ ] Complete C05 visible attackable gate, breach, radar destruction/marker removal, archive hold and result. No target shortcut or invulnerability.
- [ ] Run EN and FA with tutorial variants, normal recoverable mid-match states, interrupted gestures, loss and result transitions. Close shared regressions at their owning systems.

Exit: all five coverage rows meet Campaign checks in [ACCEPTANCE.md](ACCEPTANCE.md), with per-run evidence. The remaining Skirmish/Operations work stays visibly open.

## A5 — Skirmish and Operations tactical play

- [ ] Build a local strategy from public win/lose conditions: preserve mandatory assets, select useful composition, reserve resources, recruit/reinforce, defend, advance, inspect and attack.
- [ ] Complete S01 through real recruitment/delivery and construction/exchange where used. Test mountains/roads, blocked production, poor placement, population/resource limits, losses and rebuilding without free units or resources.
- [ ] Use visible enemy composition and map information; no opponent AI plans or hidden spawn timings. Validate strategy through several seeds rather than one scripted opening.
- [ ] Integrate every playable Operations record from A0 with public objectives, faction relations, carried-in force constraints and result handling. Respect the current tactical scope; do not press End Day, buy permanent upgrades or start another operation.
- [ ] For content-blocked Operations records, document the exact gameplay dependency and integration contract. Re-run this package when that content becomes playable; do not publish an Operations-ready claim meanwhile.
- [ ] Test every supported setup option individually at least once; select a documented combination matrix for interactions and seeds. Exclude options only when the game itself marks them unsupported.
- [ ] Verify win/loss/draw/deadline paths and partial-match starts, with truthful progress and bounded recovery when victory is no longer feasible.

Exit: current free-match capabilities and all playable Operations rows pass; no unknown launch route remains. Record strategy win rates independently from UI/control correctness.

## A6 — certification and regression

- [ ] Run the full [acceptance matrix](ACCEPTANCE.md), including stop races, modal/layout changes, occlusion, background/resume, disabled controls, stale targets and localization.
- [ ] Capture complete normal-speed matches and gesture traces on pinned builds. Record setup shortcuts separately; injected test fixtures are not normal-play victories.
- [ ] Measure observation/planning/input/hand/audio costs against manual-play baseline and current product budgets on supported Android devices. Include warmup, dense combat, production, camera movement and terminal transitions.
- [ ] Have unfamiliar players watch and then repeat selection, placement/production and transport actions. Record confusion and fix it; an automated win alone is not teaching acceptance.
- [ ] Recheck manual M1–M5 and current Skirmish controls with Watch off, after handback and after Replay. Confirm normal saves, result/reward settlement and audio remain correct.

Exit: package status distinguishes Editor, Android development/release and player evidence. Missing device evidence remains open even if an internal Editor preview is useful.

## A7 — availability and maintenance

- [ ] Enable player UI only for certified exact content/capability versions; keep one global reversible feature switch and per-match support data.
- [ ] Validate every publicly playable catalog/route against the support manifest in the release workflow. New content cannot silently ship without a disposition and tests.
- [ ] Persist coaching preferences and non-punitive assisted-result metadata; never persist active ownership or queued gestures.
- [ ] Document developer run selection, isolated saves, normal-speed evidence collection, trace interpretation and honest pass/failure reporting. Player and QA executor must remain identical.
- [ ] Publish a completion report with coverage, open dependencies, exact evidence builds, observed success rates and known limits. Avoid broad claims based on hidden helpers.

Exit: the feature is usable in every currently playable certified match and maintainers can extend it through the same contract. Cloud planning, monetization and Operations metagame automation remain separately scoped future work.

## Issue and evidence record

Use `ID | package | coverage/seed/start state | reproduction | expected | actual | severity | owning system | narrow fix | affected modes | evidence | status`. Source inspection, unit validation, simulated touches, rendered/audio review, device performance and player comprehension are distinct evidence types. Keep the exact reviewed mockup and test recordings linked, and update status only after the corresponding evidence exists.
