# Lane
Gameplay

# Task
Architecture refactor step 1: move gameplay-start runtime flag reset out of `GameBootstrap` and into `RuntimeGameplayStateSystem`.

# Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/AgentReports/2026-05-23_gameplay-runtime-state-start-reset-boundary.md`

# Contracts touched
- `GameplayArchitectureContractTests.RtsSelectionSystemMustUseRuntimeGameplayStateBoundary` now verifies `RuntimeGameplayStateSystem.ResetForGameplayStart` exists.
- The same guardrail verifies `GameBootstrap` calls that boundary and does not directly assign the gameplay-start reset flags.

# User-visible behavior
No intended user-visible behavior change. `BeginGameplay` still starts gameplay, suppresses the first world click, clears build/selection/fullscreen-map/camera-input state, and clears the pending initial camera focus flag.

# Validation run
- Focused whitespace check:
  - `git diff --check -- Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- Unity EditMode architecture tests in `WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-runtime-state-reset-architecture.xml -logFile /private/tmp/warlinecapture-runtime-state-reset-architecture.log`

# Validation result
- Focused whitespace check passed.
- `GameplayArchitectureContractTests`: passed 66/66.

# Known gaps
- `GameBootstrap` still owns runtime root creation, managed system construction/wiring, menu startup binding, gameplay feature initialization, and the long managed update loop.
- This task intentionally did not touch per-frame runtime update order or `createFactionBases`/city visual paths.

# Cross-lane impacts
None. No UI/art/scene files were changed.

# Next recommended task
Step 2: extract runtime root creation from `GameBootstrap.EnsureRuntimeRoots` into a dedicated composition boundary while preserving root names, parent transforms, and zero-local-transform behavior.
