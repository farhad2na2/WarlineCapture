# Designer Validation: AAA Readiness Recommendation Audit

Date: 2026-05-16
Lane: Designer / Game Design
Status: complete; PM/user review required before dispatch
Owner of next action: PM/user

## Sources Reviewed

- `Design/AgentReports/2026-05-10_pm_aaa-readiness-recommendation-approval.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`
- `Design/WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/VisualLockLayered/README.md`
- Existing `Design/VisualLockLayered/` package READMEs for SCN-03, SCN-05, SCN-06, SCN-07, SCN-08, SCN-09, SCN-10, SCN-11, SCN-12, POP-01, POP-03, POP-05, POP-10, and POP-11.

## Executive Verdict

The May 10 AAA readiness recommendation is directionally useful, but it cannot be dispatched as-written. Several items are stale because layered packs or route shells now exist. The valid Art/Atlas target-lock work should be narrower:

1. `SCN-02_MainMenu` - valid new layered target needed for canonical resources and designed-unavailable route states.
2. `POP-05_MissionResult` - valid revised layered target needed for M01/current Chapter 1 result, canonical rewards, mission/scenario/map identity, and consequence row.
3. `POP-10_AssistantTakeover` - valid revised layered target needed if ARIA takeover is selected for implementation; current package is flat reference only.
4. `POP-11_CommanderIdentity` - valid revised layered target needed before implementation; current package is flat reference only.
5. `SCN-11_OperationDashboard`, `SCN-12_DistrictDetailActions`, and `POP-06_EndOfDayReport` - valid but should be dispatched only as an Operation readability slice after PM/user confirms Operation is the next priority.

Do not dispatch M01 Gameplay, QA/HCI, or Art/Atlas directly from this report. PM/user should approve the selected spec blocks first.

## Recommendation Validation Table

| Surface | May 10 recommendation | Designer classification | Current source of truth | Designer decision |
| --- | --- | --- | --- | --- |
| `SCN-01` Splash | Keep As-Is | Valid | UI element alignment requires loading status readability; no current critical-path mismatch. | No revised layered mockup. Maintain existing target. |
| `SCN-02` Main Menu | Revise | Valid | UI element alignment requires resource strip to use Credits, Materials, Command Authority and designed-unavailable states; mockup audit flags Main Menu gem/resource and Operation wording drift. | Create revised `Design/VisualLockLayered/SCN-02_MainMenu` target-lock pack. |
| `SCN-03` Commander Profile | Revise | Partially valid | `SCN-03_CommanderProfile` route shell exists; FTUE identity requires commander identity entry through `POP-11`. | Do not redo SCN-03 now. Route identity work through `POP-11`; later service-backed profile pass can revise SCN-03. |
| `SCN-04` Settings | Keep As-Is | Valid | UI alignment requires accessibility visibility and unsupported-state explanation. No active mismatch. | No revised layered mockup. |
| `SCN-05` Saga Map | Revise | Invalid / stale | `Design/VisualLockLayered/SCN-05_SagaMap` is already Chapter 1 / First Response route-ready with five nodes. | No new Art target; remaining work is data binding/implementation validation if needed. |
| `SCN-06` Mission Briefing | Revise | Invalid / stale | `Design/VisualLockLayered/SCN-06_MissionBriefing` is route-ready with Mission / ScenarioSetup / Level / Map content. | No new Art target now. |
| `SCN-07` Loadout | Revise | Defer | `SCN-07_LoadoutSquadPrep` is route-ready; mockup audit says later mission-specific regeneration may be useful. | Defer until M04/M05 loadout depth is active. |
| `SCN-08` Battle HUD | Revise High | Partially valid | Tactical UI work order and M01 contract remain valid; `SCN-08_RTSBattleHUD_M01_TacticalFeedback` and M01 imagegen sample now cover target review. | No broad Art redo now; implementation/capture validation remains lane work after PM/user resumes M01. |
| `SCN-09` Build Drawer | Revise High | Partially valid / stale for M01 | `SCN-09_BuildDrawer_M01DisabledState` exists and satisfies target-review gate; M01 build is disabled with `MissionDoesNotAllowBuild`. | No M01 Art redo. Full build drawer can be revised when M02 build flow is active. |
| `SCN-10` Command Wheel | Revise High | Partially valid / stale | `SCN-10_UnitCommandWheel` is route overlay implemented; `SCN-10_UnitCommandWheel_TargetingState` exists for target review. | No new Art target now; behavior validation remains implementation work. |
| `SCN-11` Operation Dashboard | Revise | Valid | Route shell exists but is designed-unavailable; north star requires Operation day loop and district consequence readability. | Create revised Operation readability layered target only if PM/user starts Operation slice. |
| `SCN-12` District Detail | Revise | Valid | Route shell exists but is designed-unavailable; north star requires district risk, confidence, and consequence clarity. | Create revised Operation readability layered target only with SCN-11 slice. |
| `SCN-13` Quick Custom | Keep As-Is | Defer | Mockup audit flagged wording/rule mismatch, but this is lower immediate risk and not current PM priority. | Defer; no Art dispatch now. |
| `POP-01` Threat Alert | Revise | Partially valid / stale | `POP-01_ThreatAlert` and `POP-01_ThreatAlert_RoutePreviewState` already exist. | No Art redo now; route-preview behavior belongs to implementation/validation. |
| `POP-02` Confirm Raid | Keep As-Is | Valid | UI alignment covers confirm raid risk/cost/intel fields; no current blocker. | No revised layered mockup. |
| `POP-03` Build Placement | Revise High | Partially valid / stale | `POP-03_BuildPlacement` and `POP-03_BuildPlacement_MetadataValidityState` exist. | No M01 Art redo. Revisit for M02 build placement implementation. |
| `POP-04` Reward Unlock | Keep As-Is | Defer | Mockup audit notes reward visual terms still need canonicalization, but it is not a current critical path. | Defer until reward-unlock feature is routed. |
| `POP-05` Mission Result | Revise High | Valid | North star requires objective/star/reward/consequence clarity; M01 contract requires active mission ids; POP-05 README still references stale Downtown Breakthrough/Supply Crate/Unlock Fragments target content. | Create revised `POP-05_MissionResult` layered target for M01/current Chapter 1 result. |
| `POP-06` End Of Day | Revise | Valid but defer behind Operation slice | Operation loop requires district deltas and next action clarity; route is not current M01 priority. | Include as Operation slice spec, not immediate standalone dispatch. |
| `POP-07` Pause | Keep As-Is | Valid | No current blocker; route/exit safety is adequate for current scope. | No revised layered mockup. |
| `POP-08` Intel Reveal | Keep As-Is | Valid | Confidence/evidence readability remains adequate for current phase. | No revised layered mockup. |
| `POP-10` Assistant Takeover | Revise High | Valid | FTUE/ARIA design requires visible ownership, cancel/resume, and instant yield. Current package is flat target only. | Create implementation-ready layered target before ARIA takeover implementation. |
| `POP-11` Commander Identity | New Screen | Valid | FTUE design explicitly requires identity flow; current package is flat target only. | Create implementation-ready layered target before identity implementation. |

## Revised-Screen Priority Order

1. `POP-05_MissionResult` - highest product value for current M01/Chapter 1 closure.
2. `SCN-02_MainMenu` - fixes first-hub resource and non-live route trust issues.
3. `POP-11_CommanderIdentity` - supports player role and FTUE identity.
4. `POP-10_AssistantTakeover` - supports ARIA trust only if takeover is entering implementation.
5. `SCN-11_OperationDashboard`, `SCN-12_DistrictDetailActions`, `POP-06_EndOfDayReport` - dispatch together as an Operation readability slice, not during paused M01 runtime work.

## Art/Atlas Spec Blocks

### Spec 1 - SCN-02 Main Menu

Screen/popup id: `SCN-02`

Player-facing problem: The Main Menu is the player's first hub. Current design evidence flags resource ambiguity and silent/non-live routes. If the top strip reads as a generic gem economy or buttons look inert, the game feels less coherent and less AAA.

Source evidence:

- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`: resource strip must bind to canonical resources; Main Menu route buttons must expose `DesignedUnavailable` states for Inbox, Store, Events, Ranking, and Command Feed when non-live.
- `WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`: Main Menu top resource should read Credits, Materials, Command Authority; Persistent Operation copy should frame district/city operation pressure, not broad global war.
- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`: main mode fantasy is Saga Campaign, Persistent Operation, and Quick Custom, with city pressure and district consequence.

Exact target-lock layered mockup change needed:

- Create `Design/VisualLockLayered/SCN-02_MainMenu/`.
- Regenerate the flattened Main Menu reference and layered pack in the existing WarlineCapture dark glass/metal, cyan trim, amber accent style.
- Top strip must show Credits, Materials, and Command Authority as distinct canonical wallet resources.
- Mode cards must clearly show Saga Campaign, Persistent Operation, and Quick Custom Game.
- Persistent Operation subtitle should communicate district/city operation pressure.
- Non-live side routes must show designed-unavailable/empty-state affordances rather than silent clickable icons.

Required runtime/gameplay data exposed:

- `PlayerProfileState`, commander name/portrait/default fallback.
- Wallet values for Credits, Materials, Command Authority.
- Mode unlock/designed-unavailable state for Saga, Operation, Quick Custom.
- Designed-unavailable route state for Inbox, Store, Events, Ranking, and Command Feed.

Art/Atlas deliverable path:

- `Design/VisualLockLayered/SCN-02_MainMenu/`

Acceptance checks:

- One 16:9 and one 20:9 target/contact review shows all three top-strip resources identifiable without alternate gem semantics.
- Every non-live route has visible designed-unavailable copy or badge state.
- Main Menu style matches nearby accepted WarlineCapture UI targets.
- TMP text is live-text planned; frames, icons, cards, badges, and fills are separated in `layer_manifest.json`.

### Spec 2 - POP-05 Mission Result

Screen/popup id: `POP-05`

Player-facing problem: The result screen is where tactical victory becomes progression and city consequence. The current layered package exists, but its README still references stale `Downtown Breakthrough`, `Supply Crate`, and `Unlock Fragments` content. That is not acceptable for M01/current Chapter 1 approval.

Source evidence:

- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`: result must expose objectives, stars, rewards, and district/civilian consequences.
- `WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`: POP-05 needs canonical rewards and a visible civilian/district consequence row.
- `WarlineCapture_M01_FirstContact_Production_Contract.md`: result must use `saga.ch01.m01.first_contact`, `scenario.ch01.m01.first_contact`, `level.ch01.district_edge_01`, `iso.ch01.district_edge_01`, objective completion, stars/rewards, and squad survival.

Exact target-lock layered mockup change needed:

- Revise `Design/VisualLockLayered/POP-05_MissionResult/` around the current M01 or Chapter 1 canonical result.
- Show mission/scenario/level/map identity in one compact metadata row.
- Show objective checklist with `Destroy hostile patrol` complete.
- Show star outcome rows and deterministic reward grid using canonical reward names only.
- Add visible civilian/district consequence row, even if the M01 tutorial consequence is neutral/zero-delta.
- Remove stale Downtown Breakthrough/Supply Crate/Unlock Fragments terms from target notes and visible target.

Required runtime/gameplay data exposed:

- `MissionResultData`
- MissionId, ScenarioSetupId, LevelId, IsoMapId
- objective result rows
- star result rows
- reward grant rows using canonical reward types
- civilian/district consequence summary and deltas
- replay/continue button states

Art/Atlas deliverable path:

- `Design/VisualLockLayered/POP-05_MissionResult/`

Acceptance checks:

- Reviewer can answer mission identity, objective outcome, star outcome, reward grants, and city consequence in one glance.
- No noncanonical reward labels appear.
- Consequence row is visible as a first-class result field.
- Existing POP-05 layered workflow remains: flattened reference, separated layers, contact sheet, manifest, README, and dry-run copy helper.

### Spec 3 - POP-10 Assistant Takeover

Screen/popup id: `POP-10`

Player-facing problem: ARIA takeover is trust-critical. If control ownership is ambiguous, players may feel the game took control from them. The current `POP-10_AssistantTakeover` is a flat visual target, not an implementation-ready layered pack.

Source evidence:

- `WarlineCapture_FTUE_And_Command_Assistant_Design.md`: takeover requires explicit `Do It`, visible ownership banner, cancel/resume affordance, and instant yield on player input.
- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`: assistant surfaces must bind to tutorial/recommendation state and expose control ownership.
- `WarlineCapture_M01_FirstContact_Production_Contract.md`: ARIA M01 actions must use typed ids and command intents, not screen coordinates.

Exact target-lock layered mockup change needed:

- Upgrade `Design/VisualLockLayered/POP-10_AssistantTakeover/` from flat target reference to a full layered target pack when implementation is approved.
- Show an ownership banner: `ARIA controlling. Tap anywhere to resume command.` or final localized equivalent.
- Include current command intent, target summary, `Stop`, `Resume`, and clear cancel/yield affordance.
- Show takeover over blurred/dimmed tactical HUD context without hiding objective, selected unit, or critical markers.
- Include at least two visual states: `AssistantPreview` and `AssistantTakeover`.

Required runtime/gameplay data exposed:

- `AssistantControlOwner`
- active command plan id
- tutorial/recommendation context id
- selected entity or target id
- player override pending state
- cancel/resume availability

Art/Atlas deliverable path:

- `Design/VisualLockLayered/POP-10_AssistantTakeover/`

Acceptance checks:

- Ownership is obvious within one second.
- Stop/cancel/yield controls are visually primary enough to be trusted.
- No popup state implies ARIA can complete a mission unattended.
- Layer pack separates modal frame, banner, buttons, ARIA icon/portrait, intent rows, and context backdrop.

### Spec 4 - POP-11 Commander Identity

Screen/popup id: `POP-11`

Player-facing problem: The player is the Field Commander, separate from ARIA. Commander identity is required for first launch and profile edit, but the current `POP-11_CommanderIdentity` package is flat reference only.

Source evidence:

- `WarlineCapture_FTUE_And_Command_Assistant_Design.md`: first launch flow includes Commander Identity; first version needs name, portrait, default frame, confirm/cancel.
- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`: POP-11 must bind to profile save data, commander portrait/frame/title config, unlock state, and cosmetic ownership.
- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`: the player fantasy is Field Commander making city operation decisions.

Exact target-lock layered mockup change needed:

- Upgrade `Design/VisualLockLayered/POP-11_CommanderIdentity/` from flat target reference to implementation-ready layered target pack.
- Show current portrait preview, commander name input, 3-6 free default portraits, default frame, confirm and cancel.
- Show locked portraits/frames only if the visual includes clear unlock reason affordances.
- Keep ARIA visually separate from the commander portrait.

Required runtime/gameplay data exposed:

- `PlayerProfileSaveData.commanderName`
- `commanderPortraitId`
- `commanderFrameId`
- `commanderTitle`
- portrait/frame/title config list
- cosmetic unlock state
- confirm/cancel validation state

Art/Atlas deliverable path:

- `Design/VisualLockLayered/POP-11_CommanderIdentity/`

Acceptance checks:

- First-launch identity step is understandable without long instructions.
- Commander portrait is not confused with ARIA.
- Defaults are available immediately; locked cosmetics show reason if visible.
- Name input and portrait selection are live UI elements, not baked image text.

### Spec 5 - Operation Readability Slice

Screen/popup ids: `SCN-11`, `SCN-12`, `POP-06`

Player-facing problem: Operation should feel like a district pressure rhythm, not a detached menu. Existing SCN-11/SCN-12 packages are route shells, not live-loop target locks. POP-06 must explain day-end consequences when Operation becomes active.

Source evidence:

- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`: Operation loop is Day Start -> Warnings -> Actions -> Resolution -> End Day -> Drift/Escalation.
- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`: SCN-11, SCN-12, POP-06 must bind to Operation state, district metrics, warnings, action availability, and day summary.
- `WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`: SCN-11/SCN-12 are designed-unavailable shells until live binding.

Exact target-lock layered mockup change needed:

- Revise `Design/VisualLockLayered/SCN-11_OperationDashboard/` as one live Day 1 dashboard state.
- Revise `Design/VisualLockLayered/SCN-12_DistrictDetailActions/` as one selected-district action/risk/consequence state.
- Revise `Design/VisualLockLayered/POP-06_EndOfDayReport/` as one end-of-day delta report.
- The three targets should share one coherent example district and one action chain so PM/user can review the loop.

Required runtime/gameplay data exposed:

- Operation day/time
- district id/name
- trust/security/intel/infrastructure/supply/heat metrics
- warning severity and route/state
- action costs and availability reasons
- before/after deltas
- end-of-day cause/effect explanation

Art/Atlas deliverable paths:

- `Design/VisualLockLayered/SCN-11_OperationDashboard/`
- `Design/VisualLockLayered/SCN-12_DistrictDetailActions/`
- `Design/VisualLockLayered/POP-06_EndOfDayReport/`

Acceptance checks:

- Reviewer can identify the urgent district, recommended next action, cost, risk, expected delta, and end-of-day consequence without debug context.
- Warning severity is not color-only.
- Disabled actions show reasons.
- The three targets read as one operation loop rather than unrelated screens.

## VisualLockLayered Packages Needing New Or Revised Target-Lock Mockups

Immediate, if PM/user approves dispatch:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`
- `Design/VisualLockLayered/POP-11_CommanderIdentity/`
- `Design/VisualLockLayered/POP-10_AssistantTakeover/`

Deferred behind Operation slice approval:

- `Design/VisualLockLayered/SCN-11_OperationDashboard/`
- `Design/VisualLockLayered/SCN-12_DistrictDetailActions/`
- `Design/VisualLockLayered/POP-06_EndOfDayReport/`

No new target-lock mockup needed now:

- `Design/VisualLockLayered/SCN-05_SagaMap/`
- `Design/VisualLockLayered/SCN-06_MissionBriefing/`
- `Design/VisualLockLayered/SCN-07_LoadoutSquadPrep/`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback/`
- `Design/VisualLockLayered/SCN-09_BuildDrawer_M01DisabledState/`
- `Design/VisualLockLayered/SCN-10_UnitCommandWheel/`
- `Design/VisualLockLayered/SCN-10_UnitCommandWheel_TargetingState/`
- `Design/VisualLockLayered/POP-01_ThreatAlert/`
- `Design/VisualLockLayered/POP-01_ThreatAlert_RoutePreviewState/`
- `Design/VisualLockLayered/POP-03_BuildPlacement/`
- `Design/VisualLockLayered/POP-03_BuildPlacement_MetadataValidityState/`

## Rejected Or Deferred Recommendations

- Reject broad immediate dispatch of all high items. The May 10 report is recommendation-only and several targets now exist.
- Reject new SCN-05/SCN-06 Art work for the May 10 reasons; those packages are now route-ready for Chapter 1.
- Reject new M01 SCN-09/SCN-10/POP-01/POP-03 Art redo as a blocker; current state packs satisfy target-review gate, with remaining work belonging to implementation/data-binding validation.
- Defer SCN-07 mission-specific regeneration until M04/M05 loadout or transport/breach depth is the active priority.
- Defer SCN-13 Quick Custom polish until Quick Custom becomes a priority.
- Defer POP-04 Reward Unlock canonical-label polish until reward-unlock implementation is routed.
- Defer Operation slice targets unless PM/user confirms Operation is the next product focus after the paused M01 gameplay iteration.

## Open Questions And Blockers

- PM/user must choose the dispatch subset. Recommended first dispatch is `POP-05_MissionResult` and `SCN-02_MainMenu`.
- For `POP-05`, PM/user should choose whether the revised example is strict M01 First Contact or a later Chapter 1 mission with richer reward/consequence data. Designer recommendation: use M01 for vertical-slice closure.
- For `SCN-02`, PM/user should confirm whether Store remains designed-unavailable or routes to the existing `SCN-14_StoreCommandExchange` shell in the next public build.
- For Operation slice, PM/user must confirm whether Operation is in scope before Art/Atlas spends time revising SCN-11/SCN-12/POP-06.

## Routing

Designer deliverable: complete

Next lane: PM/user review

Possible next lane after PM/user approval: Art/Atlas, only for the approved target-lock packages listed above

Gameplay and QA/HCI: held for this recommendation-validation task

User approval required before Art/Atlas dispatch: yes
