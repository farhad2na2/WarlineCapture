# PM Message To Gameplay

Date: 2026-05-14
Priority: P0
Status: active

PM correction: the previous Gameplay delivery is rejected as incomplete.

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md` implemented only a narrow Build-button HUD behavior.
- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md` proved the runtime visual is not implemented: capture is blank/invalid and does not match the target.

User verified in the actual Game scene that the target gameplay is not implemented. Gameplay must keep working until the Game scene matches the target mockup; do not pass a blocker-only proof report as delivery.

PM clarification from the M01 Designer spec: do not skip the UI or design-spec state. A Game scene that only shows terrain/soldiers, or only shows a blank tactical screen, is not an implementation of M01-01. The visible runtime must show the M01 design specification: objective panel, Star Goals/objective row, neutral/disabled command panel, minimap with start viewport, threat/log mission start row, squad cards, assistant closed, no selected unit, no move/attack/objective/invalid markers, and Build unavailable with `MissionDoesNotAllowBuild`.

PM gate update: `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md` is rejected as an implementation proof. It shows progress, but not an approved match. The capture still fails the target contract: wrong camera/framing, wrong objective copy/count, extra/incorrect objective rows, command bar includes Build and active-looking states, ARIA/assistant is visible, threat feed is noisy, bottom HUD/layout does not match M01-01, and soldier placement/count/readability do not match the target. Continue implementation and return a corrected v2 proof. Do not route QA/HCI and do not close this as a blocker.

PM/user correction after stopping Gameplay: the runtime is using the wrong/old battlefield background. The approved M01-01 target mockup background is the visual lock source. `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` still owns the production ids, including `IsoMapId: iso.ch01.district_edge_01`; do not invent a new mission/map id without updating the contract. The issue to fix is the stale/wrong source art or runtime map content behind that M01 tactical map path. First fix the Game scene/map/background source so the runtime uses the approved M01 mockup battlefield background through the contracted M01 map path, then continue soldiers, HUD, and proof work on that corrected background.

Do not forget the existing code architecture contract. Follow `Design/Architecture/gameplay_solid_ecs_contract.md` and the M01 production/readability contracts. New gameplay work must keep runtime behavior in ECS data plus ECS systems; Unity object code is only for authoring, baking, UI views, bootstrap composition, config assets, and editor tooling. Do not put mission-specific behavior, unit spawning policy, camera/framing policy, UI route rules, or asset-resolution policy into bootstrap/root scene glue. Do not add static logging or new static gameplay facades. Keep changes scoped and document contract compliance in the v2 proof.

PM/user rejection after v2: `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof-v2.md` is rejected. You drifted from the task by replacing or bypassing the designed app flow. The existing loading screen, main menu, custom game mode, and normal navigation/mission launch flow must remain intact. M01-01 must be reached through the existing designed mission launch/runtime path, not by replacing scene startup or making the Game scene boot directly into a special M01 slice.

Restore-first requirement: before any further visual-fit work, revert or remove the v2 scene-startup drift and restore the existing app flow. Do not keep M01-specific shell startup assets or scene startup installers that bypass loading, main menu, custom game mode, route contracts, or existing scene bootstrap responsibilities.

Implement M01-01 through the existing designed game flow and actual runtime path:

- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game/GameSubScene.unity`

Target:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`

Required runtime implementation:

- loading screen, main menu, custom game mode, and normal mission selection/launch flow remain present and functional
- M01-01 state is reached through existing game design/navigation/mission launch contracts, not a direct replacement scene startup
- no new M01-specific shell startup config/installer may bypass the normal app flow
- battlefield visible in Game scene, not blank gray
- battlefield/background must match the approved M01-01 mockup background; wrong/old map background is not acceptable
- keep the contracted M01 production ids unless the contract is deliberately updated; fix stale source art/content behind the map path rather than inventing an untracked map id
- same camera/framing direction as target
- player rifle squad in lower-left tactical area
- enemy patrol in upper-right tactical area
- all visible soldiers present according to the target/spec
- soldiers rendered through existing ECS/runtime presentation, not pasted mockup pixels
- soldier idle animation active for proof capture using approved/available sprite atlas frames
- implementation compliant with `Design/Architecture/gameplay_solid_ecs_contract.md`: runtime behavior in ECS data/systems, scene/bootstrap only for composition, no scene-only hack, direct scene boot replacement, or pasted visual shortcut
- M01-01 no-selection state: no player selection rings, no selected status, no move/attack/objective/invalid marker
- HUD and design-spec state must be visible:
  - objective panel lists `Destroy hostile patrol`
  - Star Goals row visible
  - command panel neutral/disabled until selection
  - allowed command set is Select, Move, Attack, Stop, Hold
  - Build unavailable in M01 and if visible must communicate `MissionDoesNotAllowBuild`
  - minimap shows the M01 start viewport
  - threat/log panel may show mission start
  - squad cards present/readable
  - assistant/ARIA closed
  - no selected squad panel/status
- no flattened mockup PNG used as runtime source

After restore and implementation, provide:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-flow-restored-implementation-proof-v3.md`

Required proof:

- fresh runtime screenshot/capture from actual Game scene
- proof that the existing loading screen, main menu, custom game mode, and normal mission launch path still exist and are not bypassed
- side-by-side/contact-sheet or overlay comparison against the target mockup saved under `Design/AgentReports/Captures/`
- fresh capture names, for example `M01-01_GameSceneRuntimeCapture_v3_1920x1080.png` and `M01-01_GameSceneRuntimeCapture_v3_vs_Target_Comparison.png`
- written match/mismatch assessment covering all soldiers, ECS animation proof, camera/framing, objective/Star Goals, command panel state, allowed command set, squad cards, threat feed, minimap start viewport, assistant closed, Build unavailable reason, no selected rings, no selected status, and no world markers
- written proof must explicitly state the runtime map/background source and show that it matches the approved M01-01 mockup background before soldier/HUD assessment
- validation command, workspace, log path, result, and recommended next steps
- architecture notes listing the systems/files touched and confirming compliance with `Design/Architecture/gameplay_solid_ecs_contract.md`
- explicit list of restored/reverted v2 drift files and confirmation that no M01-specific scene startup replacement remains
- `GameplayArchitectureContractTests` result, or an explicit explanation if the test could not be run

Do not route QA/HCI, Designer, Art/Atlas, or more Gameplay feature work until the actual Game scene implementation and proof exist and PM/user explicitly approves this M01-01 implementation slice.
