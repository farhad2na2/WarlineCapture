# WarlineCapture Handoff

Lane: Gameplay

Task: Fix initial-spawn/loading regression after moving InitialUnitsSpawnSystem away from the BuildingPlacementSystem facade.

Files changed:
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs
- Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-24_gameplay-initial-spawn-loading-regression.md

Contracts touched:
- Added BuildingFactionProductionSpawnPointReadModel as the ECS boundary read model for owned building production spawn slots.
- GameBootstrap now ensures the production spawn-point read-model buffer on the building runtime boundary entity.
- BuildingRuntimeBoundarySystem publishes production spawn points from runtime building data and forces a read-model publish after successful runtime spawn requests.
- InitialUnitsSpawnSystem resolves initial air-unit spawn positions from BuildingFactionProductionSpawnPointReadModel instead of the old BuildingPlacementSystem.TryGetFactionProductionSpawnPoint facade path.
- Initial building request completion no longer retries a whole base forever after a completed request batch contains a failed wall/building request; this prevents the loading gate from being held by one blocked initial building request.
- Removed the remaining hard unit-spawn gate that skipped all non-M01 unit spawning while InitialBuildingsSpawned was still zero. That gate could leave loading stuck if initial base building requests were unavailable, blocked, or partially failed.
- Added an air-unit fallback spawn-cell path so helicopters/air units do not hold InitialUnitsSpawnInitialized forever when a helipad/airport production slot read model is missing or late.
- InitialUnitsSpawnSystem now allows soldiers to spawn while building requests are still pending, but it does not add InitialUnitsSpawnInitialized until required initial building/base requests have been issued and processed. This keeps the building retry path alive when the configured building read model is late.
- Optional configured initial building entries now skip individually if their faction or configured spawnable read model is missing, instead of preventing already-queued faction base requests from being marked issued.
- Added InitialBuildingCompletionWaitFrames and a 300-frame fail-open so the startup loading gate cannot remain stuck forever if initial building/base requests are still unresolved.
- Initial faction base layout planning now skips unresolved optional layout prefab keys instead of aborting the entire base batch. Required wall/gate/core resolution still gates the base batch.

User-visible behavior:
- Initial air units, including transport helicopters, can again spawn from helipad/airport production slots through the ECS boundary path.
- The loading screen should no longer remain blocked only because initial base building/wall requests failed, were unavailable, had not completed before unit spawning, or because an initial air unit could not resolve an exact platform slot.
- Initial base/building requests should no longer be skipped just because soldiers finish spawning first.
- Optional initial building config mistakes should no longer poison the entire initial base batch. If the required base path remains unresolved for 300 frames after units/blockers finish, loading clears with an `[InitialSpawn] fail-open initial building completion...` warning.
- Initial base buildings should now be queued even when an optional layout key such as a generic tent alias is not present in the configured building spawnables.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-initialbase-optionalskip-architecture.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -logFile /private/tmp/warlinecapture-initialspawn-final-boundary.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter InitialFactionBaseValidationTests -logFile /private/tmp/warlinecapture-initialbase-optionalskip-base.log
- Attempted PlayMode reproduction: GameSceneTransportBoardingPlayModeTests.GameScene_NearbySoldierClickingTransportHelipadArea_WalksAndBoards
- git diff --check

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151820509129250.xml.
- Passed. BuildingRuntimeBoundaryValidationTests reported 1 total, 1 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151806316447830.xml.
- Passed. InitialFactionBaseValidationTests reported 7 total, 7 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151820329309150.xml.
- Passed. git diff --check reported no whitespace errors.
- PlayMode reproduction in /Users/farhad/Projects/WarlineCapture-CodexUnity1 did not produce a useful end-to-end validation because that clone's loaded Game scene reported zero InitialUnitsSpawnConfig entities during the diagnostic run; the failure there is scene/test setup, not the same loading-gate state the user reported.

Known gaps:
- Needs a user/editor visual check in the active opened Unity project because the validation clone does not currently bake/load InitialUnitsSpawnConfig in the Game scene under the PlayMode batch test.
- BuildingPlacementRuntimeComponent still exists for other consumers; this fix only restores the initial spawn path without reintroducing the facade dependency.

Cross-lane impacts:
- AI/building boundary migration can continue with air-unit initial spawn restored through ECS read models.

Next recommended task:
- Add or repair a reliable PlayMode startup test scene that includes InitialUnitsSpawnConfig so loading completion, initial tents, soldiers, and transport helicopters are validated end to end in CI.
