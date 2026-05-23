# WarlineCapture Gameplay Handoff

## Lane
Gameplay

## Task
Extract mission startup and M01 camera/framing policy from `GameBootstrap`.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/MissionStartupSystem.cs`
- `Assets/Game/Scripts/Systems/MissionStartupSystem.cs.meta`
- `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-mission-startup-camera-extraction.md`

## Contracts touched
- `GameBootstrap` now delegates mission startup, M01 visual-root visibility, fixed tactical mission day/night guardrails, initial camera focus, and M01 production camera pose refresh to `MissionStartupSystem`.
- `MissionStartupSystem` is the owner for M01 mission startup and camera/framing policy and does not introduce static runtime helpers.
- Gameplay architecture tests now reject the migrated mission/camera policy method declarations in `GameBootstrap`.
- `GameBootstrap` still owns only the legacy configured faction spawn resolver for fallback initial focus.
- M01 runtime startup now creates the command point entity when the command-point anchor exists and assigns the initial hostile patrol path request.

## User-visible behavior
- Intended behavior is unchanged: active M01 startup still initializes the mission, hides legacy scene roots for fixed tactical play, disables day/night visuals for the fixed tactical mission, focuses the opening camera, and refreshes the production camera frame.
- M01 command point creation and hostile patrol initial movement are now covered by the focused runtime test path.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/MissionStartupSystem.cs Assets/Game/Scripts/Systems/MissionStartupSystem.cs.meta Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `Chapter01M01PlayableRuntimeTests`
- Unity EditMode `AI`
- Unity EditMode `M01`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed `60/60`.
- `Chapter01M01PlayableRuntimeTests`: passed `10/10`.
- `AI`: passed `34/34`.
- `M01`: failed `47/50` due stale atlas presentation tests expecting old v2 sprite names while runtime resolves current v32 bootfix POT atlas sprites. The focused `Chapter01M01PlayableRuntimeTests` subset inside this sweep passed `10/10`.

## Known gaps
- `GameBootstrap.TryGetConfiguredFactionSpawnCell` remains as a legacy fallback resolver until the faction spawn config is moved behind an ECS/system-owned boundary.
- `MissionStartupSystem` still accepts managed scene shell objects (`Chapter01MissionTacticalRuntimeBinder`, `Camera`, `DayNightSystem`, and legacy visual root `GameObject`s) because those dependencies are still serialized scene references.
- Fixed tactical generic AI disabling still lives in `AIStartupSystem`; a later slice should move that mission-specific policy fully under the mission startup boundary.
- Broader M01 atlas presentation tests need a separate atlas contract update if v32 bootfix sprites are now the accepted source of truth.

## Cross-lane impacts
- Art/visual-lock lane may need to update M01 atlas presentation expectations from v2 sprite names to the current v32 bootfix POT atlas contract.
- UI lane touched only by an existing playmode validation callsite update from the removed bootstrap camera method to `MissionStartupSystem`.

## Next recommended task
Move the remaining fixed tactical AI disabling policy from `AIStartupSystem` into the mission startup boundary, then extract the next `GameBootstrap` slice: scene/UI binding lookup or runtime update sequencing.
