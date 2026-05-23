# Lane
Gameplay

# Task
Extract `GameBootstrap.EnsureRuntimeRoots()` into a small ECS-style system boundary while preserving the exact runtime root names and parent transform behavior.

# Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/RuntimeRootSystem.cs`
- `Assets/Game/Scripts/Systems/RuntimeRootSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-runtime-root-system-extraction.md`

# Contracts touched
- Added `GameplayArchitectureContractTests.GameBootstrapMustDelegateRuntimeRootCreation`.
- Guardrail verifies root creation lives in `RuntimeRootSystem`, `GameBootstrap` calls that system, root names remain `RuntimeBlockers`, `RuntimeCity`, and `RuntimeUi`, and parenting still uses `SetParent(owner, false)`.
- Guardrail rejects the retired bootstrap-root variant so this boundary follows the ECS-style `*System` contract.

# User-visible behavior
No intended user-visible behavior change. Runtime roots are still created under the bootstrap transform with the same names and the same `worldPositionStays: false` parenting behavior.

# Validation run
- Focused whitespace check:
  - `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/RuntimeRootSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gamebootstrap_responsibility_audit.md Design/AgentReports/2026-05-23_gameplay-runtime-root-system-extraction.md`
- Unity EditMode architecture tests in `WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-runtime-root-system-architecture.xml -logFile /private/tmp/warlinecapture-runtime-root-system-architecture.log`

# Validation result
- Focused whitespace check passed.
- `GameplayArchitectureContractTests`: passed 67/67.

# Known gaps
- `GameBootstrap` still owns managed system construction/wiring, menu startup binding, gameplay feature initialization, and the long managed update loop.
- This task intentionally did not touch per-frame runtime order, `createFactionBases`, city visuals, or render paths.

# Cross-lane impacts
None. No UI/art/scene files were changed.

# Next recommended task
Extract the `Start()` menu/UI startup binding into a small ECS-style system boundary, preserving the current try/catch fallback and dependency bind order.
