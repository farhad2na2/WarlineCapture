# Lane
Gameplay

# Task
Step 22 - migrate remaining editor validation tests to a narrow building gameplay harness before deleting the `BuildingPlacementSystem` facade.

# Files changed
- `Assets/Tests/Editor/BuildingGameplayTestHarness.cs`
- `Assets/Tests/Editor/BuildingGameplayTestHarness.cs.meta`
- `Assets/Tests/Editor/AIProductionValidationTests.cs`
- `Assets/Tests/Editor/AIBuildPlannerValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/BaseBreachValidationTests.cs`
- `Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs`
- `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Building facade retirement contract now blocks editor tests from constructing `BuildingPlacementSystem`.
- Added temporary editor-only `BuildingGameplayTestHarness : BuildingGameplaySystem` boundary for legacy runtime validation setup.
- Contract now asserts `BuildingGameplayTestHarness` cannot appear in production scripts.
- Retirement audit now records Step 22 complete and states there is no allowed editor facade construction.

# User-visible behavior
No intended gameplay behavior change. This is test/architecture migration only.

# Validation run
- `rg -n "new\\s+BuildingPlacementSystem\\s*\\(" Assets/Game Assets/Tests/Editor -g '*.cs'`
- `rg -n "BuildingGameplayTestHarness" Assets/Game -g '*.cs'`
- `git diff --check -- <Step 22 files>`
- Unity batchmode in `WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step52-editor-harness-migration-rerun.log -testResults /private/tmp/warline-step52-editor-harness-migration-rerun.xml`

# Validation result
- No `new BuildingPlacementSystem()` remains in production or migrated editor validation tests; only architecture-test string literals remain.
- No production references to `BuildingGameplayTestHarness`.
- `git diff --check` passed.
- Unity batchmode exited code 0 and compile-clean. Unity did not emit the requested test XML in this validation clone, so the architecture test execution result could not be parsed from XML.

# Known gaps
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` still exists as the one-line wrapper until Step 23.
- `GameplayArchitectureContractTests.cs` still intentionally contains facade references to enforce temporary debt rules; these must be removed or rewritten in Step 24 after deletion.
- Test harness is temporary; later tests should migrate from `BuildingGameplayTestHarness` to narrower owning systems where practical.

# Cross-lane impacts
- None expected. No runtime behavior or art/data contracts changed.

# Next recommended task
Step 23 - delete `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` and its `.meta`, then update references that still allow the one-line wrapper.
