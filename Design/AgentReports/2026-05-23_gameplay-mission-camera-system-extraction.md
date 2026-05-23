# Lane
Gameplay

# Task
Resume architecture refactoring while preserving the restored 60 FPS runtime path. Extract M01 camera/framing policy out of mission startup coordination into a dedicated ECS-style system boundary.

# Files changed
- `Assets/Game/Scripts/Systems/MissionCameraSystem.cs`
- `Assets/Game/Scripts/Systems/MissionCameraSystem.cs.meta`
- `Assets/Game/Scripts/Systems/MissionStartupSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-mission-camera-system-extraction.md`

# Contracts touched
- SOLID/ECS architecture contract now states mission startup is owned by `MissionStartupSystem`, while M01 camera/framing policy is owned by `MissionCameraSystem`.
- Architecture guardrail now rejects M01 camera constants, framing math, and direct camera transform writes in `MissionStartupSystem`.
- `GameBootstrap` runtime update order was intentionally left unchanged because managed runtime update extraction is paused after prior FPS regression.

# User-visible behavior
No intended user-visible behavior change. Camera behavior is routed through the same public mission startup calls, but the implementation now delegates camera policy to `MissionCameraSystem`.

# Validation run
- Focused whitespace check on changed architecture files:
  - `git diff --check -- Assets/Game/Scripts/Systems/MissionStartupSystem.cs Assets/Game/Scripts/Systems/MissionCameraSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode architecture tests in `WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-mission-camera-architecture-editmode.xml -logFile /private/tmp/warlinecapture-mission-camera-architecture-editmode-final.log`

# Validation result
- Focused whitespace check passed for the changed architecture files.
- `GameplayArchitectureContractTests`: passed 66/66.
- Note: full `git diff --check` is still blocked by unrelated UI prefab/scene trailing whitespace in existing dirty UI-lane files; those files were not modified by this task.

# Known gaps
- `MissionCameraSystem` is still a managed system called through `MissionStartupSystem`; camera request components remain a future ECS migration.
- `GameBootstrap` still owns the long managed runtime update loop by design for now, because the previous extraction correlated with a runtime FPS regression.
- Initial base/city visual performance remains the known separate performance-sensitive area; this task did not touch `createFactionBases`, city generation, render paths, or the managed runtime update list.

# Cross-lane impacts
- No UI, art, scene, or PM task files were changed for this gameplay architecture slice.
- The report notes unrelated UI-lane dirty files only because they affect whole-repo whitespace validation.

# Next recommended task
Continue with a startup-only architecture slice: replace `GameplaySceneBindingSystem` broad scene lookup with explicit scene references or a small authored binding config, only if the needed scene references are available. Avoid runtime loop extraction and base/city visual changes until there is a focused performance contract around those paths.
