# Lane
Gameplay

# Task
Fix soldier movement animation disappearing in top-down camera LOD.

# Files changed
- `Assets/Game/Scripts/Systems/UnitRenderBudgetSystem.cs`
- `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs`
- `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs.meta`

# Contracts touched
- Runtime render-budget LOD contract: moving visible character units must not be switched to static far impostors.

# User-visible behavior
- Soldiers that are visible and moving from the top-down RTS camera stay on an animatable mesh LOD, or fall back to detail if the LOD is not animatable.
- Idle distant soldiers can still use static far impostors to preserve render budget.

# Validation run
- Unity 6000.4.0f1 EditMode test run in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Filter: `UnitRenderBudgetSystemTests`.

# Validation result
- Passed: 3/3.
- Result file: `/private/tmp/warlinecapture-unit-render-budget-tests-3.xml`.

# Known gaps
- I did not run a visual play-mode capture in the main editor; this fix is validated by the render-budget policy test and compile/test execution in the Unity clone.

# Cross-lane impacts
- Art is unchanged.
- Rendering/performance impact is constrained to moving visible character units; idle far characters still use impostors.

# Next recommended task
- Verify in Play Mode from the normal RTS camera distance with several moving squads and watch FPS/FrameRateDiag to confirm the visual fix does not regress the active army render budget.
