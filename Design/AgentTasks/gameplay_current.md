# Gameplay Current Task

Date: 2026-05-14
Status: active
Priority: P0 reject v2 drift, restore game flow, then implement M01-01 through existing game design path

## Assignment

PM correction: the previous Gameplay handoff is not acceptable.

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md` implemented only a narrow Build-button HUD behavior.
- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md` proved the opposite of approval: the runtime capture was blank/invalid and did not visually match the target mockup.

User verified in the actual Game scene that the target gameplay is not implemented. Gameplay must not treat this as a blocker-only proof task and must not pass it onward.

Gameplay must implement the actual M01-01 target in the Game scene and then provide proof. The Game scene must show the M01 tactical start/no-selection state matching the approved target mockup, including all soldiers on the battlefield with ECS animation.

PM clarification from the M01 Designer spec: do not skip the UI or design-spec state. A Game scene that only shows terrain/soldiers, or only shows a blank tactical screen, is not an implementation of M01-01. The visible runtime must show the M01 design specification: objective panel, Star Goals/objective row, neutral/disabled command panel, minimap with start viewport, threat/log mission start row, squad cards, assistant closed, no selected unit, no move/attack/objective/invalid markers, and Build unavailable with `MissionDoesNotAllowBuild`.

PM gate update after latest Gameplay report: `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md` is rejected as an implementation proof. It is useful evidence, but it is not an acceptable delivery. The proof capture still fails the target mockup contract: wrong camera/framing, wrong objective copy/count, extra/incorrect objective rows, command bar includes Build and active-looking states, ARIA/assistant is visible, threat feed is noisy, bottom HUD/layout does not match M01-01, and the soldier placement/count/readability do not match the approved target. Gameplay must continue implementation and return a corrected v2 proof, not route to QA/HCI and not close as a blocker.

PM/user correction after stopping Gameplay: the latest runtime is also using the wrong/old battlefield background. The approved M01-01 target mockup background is the visual lock source for this slice. `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` still owns the production ids, including `IsoMapId: iso.ch01.district_edge_01`; do not invent a new mission/map id without updating the contract. The issue to fix is the stale/wrong source art or runtime map content behind that M01 tactical map path. Gameplay must not continue tuning soldiers/HUD on any battlefield background that diverges from the approved mockup. First fix the Game scene/map/background source so the runtime uses the same approved M01 mockup battlefield background through the contracted M01 map path, then continue soldiers, HUD, and proof work on that corrected background.

Architecture contract reminder: follow `Design/Architecture/gameplay_solid_ecs_contract.md` and the M01 production/readability contracts. New gameplay work must keep runtime behavior in ECS data plus ECS systems; Unity object code is only for authoring, baking, UI views, bootstrap composition, config assets, and editor tooling. Do not put mission-specific behavior, unit spawning policy, camera/framing policy, UI route rules, or asset-resolution policy into bootstrap/root scene glue. Do not add static logging or new static gameplay facades. Use the existing data-driven mission/map/UI pathways, keep changes scoped, and document any architecture touchpoints or unavoidable gaps in the v2 report.

PM/user rejection after v2: `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof-v2.md` is not acceptable. Gameplay drifted from the assignment by replacing or bypassing the designed app flow. The game design requires the existing loading screen, main menu, custom game mode, and navigation flow to remain intact. M01-01 implementation must be reached through the existing designed mission launch/runtime path, not by replacing the scene startup or making the Game scene boot directly into a special M01 slice.

Restore-first requirement: before any further visual-fit work, revert or remove the v2 scene-startup drift and restore the existing app flow. Specifically, do not add/keep new M01-specific shell startup assets or scene startup installers that bypass the existing loading/main-menu/custom-game-mode flow. Do not remove or bypass loading, main menu, custom game mode, route contracts, or existing scene bootstrap responsibilities.

## Read First

- `Design/AgentReports/2026-05-14_pm_gameplay-game-scene-full-implementation-required.md`
- `Design/AgentReports/2026-05-14_pm_gameplay-game-scene-proof-rejected-continue-implementation.md`
- `Design/AgentReports/2026-05-14_pm_gameplay-v2-drift-rejected-restore-game-flow.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof-v2.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md`
- `Design/AgentReports/2026-05-14_designer_m01-imagegen-sample-alignment-review.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/CameraLock_M01_DefaultStart.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`

## Required Implementation

Implement M01-01 through the existing designed game flow and actual runtime path:

- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game/GameSubScene.unity`

Runtime must match `M01-01_TacticalStart_1920x1080.png` as closely as current assets allow:

- loading screen, main menu, custom game mode, and normal mission selection/launch flow remain present and functional
- M01-01 state is reached through existing game design/navigation/mission launch contracts, not a direct replacement scene startup
- no new M01-specific shell startup config/installer may bypass the normal app flow
- tactical battlefield visible, not a blank/gray capture
- battlefield/background must match the approved M01-01 mockup background; do not use the older/different `iso.ch01.district_edge_01` look if it diverges from the approved target
- keep the contracted M01 production ids unless the contract is deliberately updated; fix stale source art/content behind the map path rather than inventing an untracked map id
- same camera/framing direction as the target mockup
- player rifle squad present in the lower-left tactical area
- enemy patrol present in the upper-right tactical area
- all visible soldiers present according to the target/spec
- soldiers rendered through the existing ECS/runtime presentation path, not static pasted mockup pixels
- soldier idle animation active for proof capture, using approved/available sprite atlas frames
- implementation compliant with `Design/Architecture/gameplay_solid_ecs_contract.md`: runtime behavior in ECS data/systems, scene/bootstrap only for composition, no scene-only hack, direct scene boot replacement, or pasted visual shortcut
- M01-01 no-selection state: no player selection rings, no selected squad status bar, no move/attack/objective/invalid world marker
- enemy affiliation/readability baseline if existing runtime supports it; if not, document exact missing runtime support after implementing the soldiers/camera/HUD baseline
- HUD baseline visible and matching the Designer spec:
  - objective panel lists `Destroy hostile patrol`
  - Star Goals row is visible
  - command panel is neutral/disabled until selection
  - allowed command set is Select, Move, Attack, Stop, Hold
  - Build is unavailable in M01 and if visible must communicate `MissionDoesNotAllowBuild`
  - minimap shows the M01 start viewport
  - threat/log panel may show mission start
  - squad cards are present/readable
  - assistant/ARIA is closed
  - no selected squad panel/status is shown
- no flattened mockup PNG imported or used as runtime source

## Required Proof After Implementation

After restore and implementation, provide:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-flow-restored-implementation-proof-v3.md`

Required proof artifacts:

- fresh runtime screenshot/capture from the actual Game scene
- proof that the existing loading screen, main menu, custom game mode, and normal mission launch path still exist and are not bypassed
- target mockup path: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- side-by-side/contact-sheet or overlay comparison saved under `Design/AgentReports/Captures/`
- fresh capture names must be unique, e.g. `M01-01_GameSceneRuntimeCapture_v3_1920x1080.png` and `M01-01_GameSceneRuntimeCapture_v3_vs_Target_Comparison.png`
- written match/mismatch assessment covering all soldiers, ECS animation proof, camera/framing, objective/Star Goals, command panel state, allowed command set, squad cards, threat feed, minimap start viewport, assistant closed, Build unavailable reason, no selected rings, no selected status, and no world markers
- written proof must explicitly state the runtime map/background source and show that it matches the approved M01-01 mockup background before assessing soldier/HUD alignment
- validation command, workspace, log path, result, and any capture command used
- architecture notes listing the systems/files touched and confirming compliance with `Design/Architecture/gameplay_solid_ecs_contract.md`
- explicit list of restored/reverted v2 drift files and confirmation that no M01-specific scene startup replacement remains
- `GameplayArchitectureContractTests` result, or an explicit explanation if the test could not be run
- recommended next steps

## Current Routing

Current owner:
Gameplay

Next action:
First restore the existing loading/main-menu/custom-game-mode flow and remove the v2 scene-startup drift. Then fix the wrong/old background/map source and continue implementing M01-01 through the normal designed mission launch/runtime path until the v3 capture visually matches the approved mockup and the written proof explicitly passes every required item. Do not stop at a blocker report unless there is a real external blocker after attempting implementation and capture.

Held until approval:
QA/HCI, further Gameplay slices, Art/Atlas full sequence production, and selected-state implementation.
