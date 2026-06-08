# AAA Readiness Recommendation (Approval Required)

Date: 2026-05-10  
Author lane: pm  
Status: recommendation only (not dispatched to lane task files)

## Approval Scope

This document is a detailed design audit + execution recommendation package.

This document does **not**:

- modify `Design/AgentTasks/*_current.md`
- alter lane ownership
- dispatch new implementation tasks

PM/user approval is required before any routing to lane task files.

## Evidence Base Used

- `Design/Gameplay_North_Star_And_Content_Grammar.md`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/FTUE_And_Command_Assistant_Design.md`
- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/Tactical_UI_Missing_Parts_Work_Order.md`
- `Design/README.md`
- `Design/Agent_Coordination_Workflow.md`

## Audit Boundaries (So Claims Are Verifiable)

This recommendation is based on design contracts, visual-lock audits, and existing agent reports in this repo.

It is **not** claiming a fresh runtime playtest for every route in this single pass.  
Where runtime proof is required, the acceptance signal explicitly calls for capture/test evidence before approval.

## Rubric Method

Scale per area:

- `9-10`: AAA-ready now for production shipment quality
- `7-8`: strong direction, moderate execution risk
- `5-6`: partially ready, missing meaningful proof/content depth
- `<5`: concept stage or unresolved fundamentals

Overall score is weighted average:

- Readability/UX/FTUE/combat loop: **40%**
- Progression/economy/content cadence: **35%**
- Pipeline/QA/live-ops readiness: **25%**

## AAA Readiness Rubric (Detailed)

Overall snapshot: **72 / 100**  
Interpretation: architecture and contracts are above average; player-visible execution maturity is not yet AAA-complete.

| Area | Score | What is strong | What is missing to hit 8.5+ |
|---|---:|---|---|
| 1) Core combat readability | 7.5 | Clear command model and reason codes; tactical bounds and marker ownership defined. | Repeated public-quality proof under target camera/HUD composition, not one-off validation. |
| 2) FTUE + onboarding | 8.0 | ARIA concept, typed-id targeting, and interruption rules are strong. | Tight pacing validation and low-friction fail recovery across full M01 player path. |
| 3) Progression loop | 7.5 | Saga/Operation/Quick hierarchy is coherent and teachable. | More implemented chapter cadence and clearer medium-term motivation proof. |
| 4) Economy fairness | 7.0 | Canonical resources and anti-pay-to-win guardrails are documented. | Telemetry-backed balancing and sink/source stress tests. |
| 5) Mission content depth | 6.0 | Archetype grammar and threat families are well authored. | More shipped encounters with reliable quality consistency. |
| 6) UX surface completeness | 7.0 | Excellent SCN/POP coverage map and contracts. | Final closure on tactical HUD missing parts and consistent disabled states. |
| 7) Visual cohesion | 6.5 | Strategic/tactical split is explicit; 2D isometric direction is coherent. | Runtime world/HUD scale readability acceptance in repeated captures. |
| 8) Production pipeline | 8.0 | Traceable handoffs, report template, and validation gates are disciplined. | Keep rigor while reducing process overhead risk. |
| 9) QA/HCI discipline | 8.0 | Player-visible evidence requirement is strong and correct. | Sustain it across all lanes without regression to state-only proofs. |
| 10) Live-ops readiness | 6.5 | Operation and events framework exists in design. | Demonstrate repeatable content + tuning loop over multiple cycles. |

## Priority Risk Register (What blocks AAA feel first)

1. **Readability mismatch risk**: strong contracts but inconsistent public-facing tactical clarity.
2. **Content density risk**: design breadth exceeds currently proven mission volume.
3. **Feedback coherence risk**: missing/weak invalid-state messaging can make controls feel non-AAA.
4. **Process drag risk**: excellent workflow rigor can reduce throughput if not pruned per milestone.

## 6-8 Week Recommendation Plan (Approval-Gated)

The plan is lane-ready but intentionally **not dispatched**.

| Week | Priority | Intended owner lanes | Deliverable | Acceptance signal |
|---|---|---|---|---|
| 1 | M01 tactical readability lock | gameplay + ui + qa-hci | `SCN-08` clarity package: selected panel, mode banner, world markers, invalid toast, minimap bridge. | 16:9 + 20:9 public captures pass HCI gate; no black-edge or unreadable state. |
| 2 | FTUE M01 reliability | support-ftue + ui + gameplay | Typed-id path stability (`deploy -> select -> move -> attack -> result`) with ARIA interruption safety. | End-to-end pass with no coordinate-based fallback; fail-recovery messaging visible. |
| 3 | Result clarity | ui + gameplay | `POP-05` integrity and consequence readability pass. | Mission/scenario/map ids correct; objective/star/reward/city consequence comprehensible in one glance. |
| 4 | Disabled-state quality | ui + gameplay | `SCN-09` + `POP-03` metadata-validity and reason-label closure for mission-banned build states. | No inert controls; all disabled actions explainable and testable. |
| 5 | Identity flow baseline | ui + support-ftue | `POP-11` v1 (name + portrait + default frame) with profile integration. | Stable first-launch + profile edit path; save/load persistence proven. |
| 6 | Operation clarity slice | ui + gameplay + qa-hci | `SCN-11/SCN-12` one-day loop readability package. | Warning severity, action outcomes, and district deltas understandable without debug context. |
| 7 | Content cadence rehearsal | gameplay + ui + qa-hci | Two additional mission archetype slices using same M01 quality gates. | Pass rate and integration effort are predictable, not ad hoc. |
| 8 | Tuning checkpoint | gameplay + pm + qa-hci | First telemetry-informed balancing pass for Chapter 1 fail/clear/star curves. | Documented adjustments + before/after metrics with rationale. |

## Mock Change Matrix (Detailed, SCN/POP)

Decision key:

- `Keep As-Is`: no major redesign required now
- `Revise`: targeted updates required
- `New Screen`: net-new implementation required

| Surface | Decision | Why | Minimum change intent before approval-to-dispatch |
|---|---|---|---|
| `SCN-01` Splash | Keep As-Is | No active critical-path disagreement in current design audits. | Maintain existing loading status readability and nonblocking route transition checks. |
| `SCN-02` Main Menu | Revise | `UIUX_Mockup_Target_Alignment_Audit.md` flags a P1 mismatch: top strip third resource appears as gem-style visual while canonical resource is `Command Authority`; same audit flags Persistent Operation subtitle wording drift from city-operation framing. `UIUX_Gameplay_Element_Alignment.md` requires explicit DesignedUnavailable states for non-live routes. | Definition of "clear enough": in a static screenshot, a reviewer must identify all three top-strip resources as `Credits`, `Materials`, `Command Authority` with no alternate gem term/icon semantics; each non-live route (`Inbox/Store/Events/Ranking/Command Feed`) must show explicit DesignedUnavailable state copy (no inert click). Acceptance: one 16:9 + one 20:9 capture + route-state checklist pass. |
| `SCN-03` Commander Profile | Revise | FTUE design requires commander identity visibility/edit path (`POP-11`) and profile surfaces as entry points. Current recommendation depends on that linkage for consistency. | Add explicit profile entry into `POP-11`; ensure commander portrait/name updates reflect in Main Menu/Profile; define locked vs available identity cosmetics states. Acceptance: focused route test from Main Menu profile shortcut and persisted reload check. |
| `SCN-04` Settings | Keep As-Is | Not current critical-path blocker. | Maintain accessibility visibility and explicit unsupported states. |
| `SCN-05` Saga Map | Revise | `UIUX_Mockup_Target_Alignment_Audit.md` identifies stale chapter-content examples in target inventory and requires mission/scenario/map binding accuracy. | Node states must show locked/current/completed/replayable distinctly; selected node panel must expose mission + scenario/map identity fields. Acceptance: screenshot with Chapter 1 five-node state + data-binding checklist. |
| `SCN-06` Mission Briefing | Revise | Same audit flags stale mission examples and calls out required Level/Map binding plus objective/star/reward/intel clarity. | "Clear enough" definition: reviewer can answer mission goal, star conditions, enemy threat, and reward set in <10 seconds without opening subpanels. Acceptance: comprehension test checklist + canonical field presence (`Mission`, `ScenarioSetup`, `Level/Map`, objectives, stars, enemy intel, rewards). |
| `SCN-07` Loadout | Revise | Alignment docs require explicit mission restrictions and non-inert lock states; route-ready exists but mission-specific clarity is marked as follow-up. | For any disabled slot/unit, show exact reason (`Locked`, `MissionBanned`, `RequiresUnlock`, etc.). Acceptance: lock-state matrix screenshots + one focused test for each reason type. |
| `SCN-08` Battle HUD | Revise (High) | `Tactical_UI_Missing_Parts_Work_Order.md` explicitly lists missing/high-priority elements (`SelectedEntityPanel`, `CommandModeBanner`, `WorldCommandMarkerLayer`, `InvalidCommandToast`, `MinimapCameraBridge`). | Must prove direct+explicit command flows (select/move/attack), marker correctness, invalid reason feedback, and bounded minimap camera jumps. Acceptance: M01 capture set + focused tests per listed `ElementId`. |
| `SCN-09` Build Drawer | Revise (High) | Work order requires `BuildDrawer.ItemAvailabilityReason`; alignment rules prohibit silent inert controls. | Every unavailable item row must render exact reason label; mission-banned build path must map to reason code (for M01: `MissionDoesNotAllowBuild`). Acceptance: unavailable-state screenshot matrix + interaction test. |
| `SCN-10` Command Wheel | Revise (High) | Work order requires explicit segments and hints (`AttackModeSegment`, `MoveModeSegment`, `TargetHint`, `DisabledReason`). | Mode entry/exit state must be visible; expected target type must be readable before click; disabled segments must expose reasons. Acceptance: command-wheel state capture sheet + segment behavior tests. |
| `SCN-11` Operation Dashboard | Revise | `UIUX_Mockup_Target_Alignment_Audit` marks as designed-unavailable shell pending live binding; readiness score for live-ops depends on this becoming actionable. | Add severity hierarchy and next-action clarity for at least one full day loop. Acceptance: one day-loop walkthrough showing warning -> action -> delta explanation. |
| `SCN-12` District Detail | Revise | Same shell status as SCN-11; intel confidence and action-risk readability required for operation legitimacy. | Show risk/consequence text tied to district metrics and confidence. Acceptance: district action card states with before/after deltas visible. |
| `SCN-13` Quick Custom | Keep As-Is | Useful testing route and lower immediate AAA risk. | Keep stable; defer major polish until M01/Operation gains close. |
| `POP-01` Threat Alert | Revise | Work order explicitly requires `ThreatAlert.JumpToThreat` and `ThreatAlert.RoutePreview` with tactical anchor correctness. | Jump must center on valid threat anchor within camera bounds; route preview must match active threat route id/ETA. Acceptance: threat popup capture + jump validation clip/log. |
| `POP-02` Confirm Raid | Keep As-Is | Structurally good for current phase. | Minor copy/polish only. |
| `POP-03` Build Placement | Revise (High) | Work order requires footprint validity overlays, socket/zone labels, and confirm gating tied to metadata validity. | Confirm disabled unless footprint+cost valid; invalid areas must show reason (blocked/socket/zone/resource). Acceptance: placement-state grid capture + confirm-state tests. |
| `POP-04` Reward Unlock | Keep As-Is | Lower risk if deterministic reward data is correct. | Keep data integrity and readability. |
| `POP-05` Mission Result | Revise (High) | `UIUX_Mockup_Target_Alignment_Audit.md` marks stale/noncanonical result terms and explicitly calls for visible civilian/district consequence row; alignment requires canonical reward types only. | Result must show mission/scenario/map identity, objective/star outcomes, canonical rewards, and consequence row in one screen. Acceptance: M01 result capture + terminology checklist pass. |
| `POP-06` End Of Day | Revise | Operation loop clarity depends on readable metric deltas and implications; currently shell-level maturity. | Show district deltas with cause/effect text and clear next-action cue. Acceptance: day-end report review with comprehension checklist. |
| `POP-07` Pause | Keep As-Is | Functionally adequate for current stage. | Preserve clear action hierarchy and safe exit messaging. |
| `POP-08` Intel Reveal | Keep As-Is | Supports operation loop adequately for now. | Keep readable confidence deltas and evidence clarity. |
| `POP-10` Assistant Takeover | Revise (High) | FTUE/ARIA design requires explicit control ownership and instant yield on player input; this is a trust-critical UX contract. | Must always expose ownership banner + cancel/resume; any player input interrupts takeover immediately. Acceptance: assisted action demo + interruption test evidence. |
| `POP-11` Commander Identity | New Screen | Explicitly required by FTUE/identity design. | Ship v1 scope first (name, portrait, default frame), then phased expansion. |

## Dispatch-Ready Package (Still Recommendation-Only)

If approved, dispatch should be in this order (not yet written to lane files):

1. `SCN-08` + `POP-05` quality lock
2. `SCN-09` + `SCN-10` + `POP-03` disabled/targeting clarity
3. `POP-10` takeover trust pass
4. `POP-11` v1 identity implementation
5. `SCN-11/SCN-12` operation readability slice

## Approval Template

```text
Status: accepted / needs changes / deferred
Approved sections:
Sections requiring rewrite:
Can dispatch to lane task files now: yes/no
If no, exact changes required before dispatch:
```

## Note On Earlier "Companion Visual Artifact"

The earlier interactive canvas was created in Cursor's managed canvas path (outside the repo root).  
This report is now fully self-contained inside the project and should be used as the approval source.

