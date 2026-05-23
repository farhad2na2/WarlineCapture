# WarlineCapture Handoff

Lane: Gameplay

Task: Fix ObjectDisposedException from BuildingRuntimeBoundarySystem completing BuildingRuntimeSpawnRequest after runtime spawn structural changes.

Files changed:
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs
- Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs.meta
- Design/AgentReports/2026-05-24_gameplay-boundary-spawn-buffer-invalidated.md

Contracts touched:
- BuildingRuntimeBoundarySystem no longer keeps a DynamicBuffer<BuildingRuntimeSpawnRequest> handle alive across BuildingRuntimeSpawnSystem.TryPlaceRuntimeBuilding.
- Pending spawn request indices are collected before runtime spawn processing.
- Each completed spawn request is written back through a freshly acquired buffer handle after any structural changes.
- Added an EditMode regression test that enqueues a BuildingRuntimeSpawnRequest, runs BuildingPlacementSystem.Update, and verifies completion survives runtime spawn structural changes.

User-visible behavior:
- Fixes the reported ObjectDisposedException when AI/build runtime spawn requests create entities and then complete the request in the same boundary update.
- No intended gameplay behavior change.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -logFile /private/tmp/warlinecapture-boundary-spawn-buffer-regression.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-boundary-spawn-buffer-contract.log
- git diff --check
- Attempted: AIBuildPlannerValidationTests

Validation result:
- Passed. BuildingRuntimeBoundaryValidationTests reported 1 total, 1 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151780302428180.xml.
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151778437836440.xml.
- Passed. git diff --check reported no whitespace errors.
- AIBuildPlannerValidationTests did not reproduce the buffer exception, but failed on an existing stale assertion: expected faction money 10000, actual 30000.

Known gaps:
- AIBuildPlannerValidationTests should be updated in a separate task to use the current BuildingRuntimeBoundary request flow instead of the older direct placement expectation.

Cross-lane impacts:
- AI build planner spawn requests should no longer crash the managed gameplay update when runtime building placement causes ECS structural changes.

Next recommended task:
- Update or replace the stale AIBuildPlannerValidationTests expectation so AI build planner validation covers the current ECS boundary request/response path end to end.
