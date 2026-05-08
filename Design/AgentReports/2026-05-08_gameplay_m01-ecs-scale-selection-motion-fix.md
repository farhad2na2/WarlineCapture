Lane:
Gameplay

Task:
Fix the rejected temporary Gate 4 M01 runtime issues for ECS-only unit presentation, metric scale, grounded selection, readable infantry movement, and visible move animation.

Files changed:
- `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset`
- `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01SpriteRendererCaptureBuilder.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/TacticalMaps/Chapter01TacticalScaleContract.cs`
- `Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Contracts touched:
- Public M01 unit presentation remains ECS entity / atlas-backed via `MissionRuntimeAtlasQuadRuntime`.
- Public M01 unit visuals no longer instantiate Unity `SpriteRenderer` components and no longer use the `M01RuntimeSpriteRenderers` root name.
- `MissionRuntimeSpriteRendererRuntime` remains rejected by validation for public M01 player/enemy unit presentation.
- M01 infantry scale now consumes the metric scale target near `0.20` instead of the rejected `0.1505` result.
- M01 command/decor building scale contract is updated from `0.14` to `0.80` for door/road-context readability.
- Selection is rendered as small grounded per-soldier markers instead of one large group marker.
- M01 infantry run/walk speed is clamped by runtime mission contract to `0.42` / `0.28` world units per second with no road-speed boost.
- Moving infantry advances a runtime atlas presentation animation phase and changes soldier pose while moving.

User-visible behavior:
- The public M01 runtime root is now `M01RuntimeEcsAtlasQuads`, not `M01RuntimeSpriteRenderers`.
- Player and enemy infantry units are visible through mesh-based ECS atlas quads with no unit `SpriteRenderer` components under the public runtime object.
- Player rifle squad soldiers are larger at the public camera scale and remain four distinct soldiers under one controllable squad entity.
- Selection is a subtle grounded marker under each soldier instead of a huge screen-covering marker.
- The unclear blue selected marker is replaced with a restrained warm grounded treatment.
- Move-to-cover should read slower and more like infantry movement rather than a teleport.
- Soldier quads visibly bob/stride while the ECS move state is active.

Validation run:
- Synced focused Gameplay files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Ran:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-gameplay-ecs-scale-selection-motion-results.xml -logFile /private/tmp/warlinecapture-gameplay-ecs-scale-selection-motion.log`

Validation result:
- Passed: `Chapter01M01PlayModeValidationTests` 8/8, exit code 0.
- Public quick custom and campaign launch paths still reach M01 production Match route.
- Public campaign golden path still reaches result popup.
- Opening-control safety window still prevents lethal hostile fire before first move.
- ECS atlas presentation assertions pass for player and enemy units.
- Public M01 player/enemy unit visuals assert no `MissionRuntimeSpriteRendererRuntime` component and no Unity `SpriteRenderer` components under the atlas runtime instance.
- Runtime object naming asserts no `SpriteRenderer` string on the public M01 atlas root.
- Infantry scale assertion passes near `0.20`.
- M01 movement speed assertion passes at calibrated infantry run/walk values.
- Move animation proof passes: animation phase advances and soldier pose changes while moving.
- Selection marker proof passes: marker count matches soldier count and each marker stays small under a soldier.
- M01 remains infantry-only.

Generated capture paths:
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`

Known gaps:
- This is still not final art signoff.
- Current infantry frames remain temporary; the runtime now animates motion through the ECS atlas presentation phase/pose while Art/Atlas owns final multi-frame run artwork.
- Enemy patrol final variant and final impact/destroyed VFX remain Art/Atlas work.
- Designer and Art/Atlas still need their rejected-temporary-art scale/readability handoff reports before QA/HCI final rerun.
- Physical-device touch ergonomics were not run in this Gameplay pass.

Cross-lane impacts:
- Art/Atlas should consume the new runtime scale/marker constraints when producing its scale/readability package.
- Designer should codify the same `0.20` infantry, door/road-context building scale, grounded per-soldier selection, and realistic infantry motion rules.
- QA/HCI can rerun after Art/Atlas and Designer reports land alongside this Gameplay report.
- UI and Support/FTUE have no new owner action unless QA/HCI finds a concrete HUD or assistant regression.

Next recommended task:
Art/Atlas and Designer should complete their rejected-temporary-art handoffs, then QA/HCI should rerun the focused Gate 4 route using the refreshed public captures and the `/Users/farhad/Projects/WarlineCapture-CodexUnity1` runtime proof.
