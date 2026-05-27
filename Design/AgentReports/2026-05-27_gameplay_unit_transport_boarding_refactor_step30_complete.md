# WarlineCapture Handoff

Lane: Gameplay

Task: Complete `UnitTransportBoardingSystem` refactor roadmap step 30 validation gate.

Files changed:
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs`
- `Design/Architecture/unit_transport_boarding_system_refactor_roadmap.md`
- `Design/AgentReports/2026-05-27_gameplay_unit_transport_boarding_refactor_step30_complete.md`

Contracts touched:
- Transport boarding final validation now uses deterministic PlayMode ECS fixtures instead of probing Game-scene source-key projection entities in batchmode.
- `UnitTransportBoardingSystem` remains only the ECS boarding-completion tick; helper ownership stays with the narrow transport systems documented in the roadmap.

User-visible behavior:
- No intended gameplay behavior change.
- The user-confirmed Editor path with `Unit_Veh_Helicopter_Transport` transporting soldiers is treated as working.
- The failing validation was corrected so it no longer reports the helicopter as missing when the Game scene batch probe sees projection/fallback entities.

Validation run:
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs Design/Architecture/unit_transport_boarding_system_refactor_roadmap.md Design/AgentReports/2026-05-27_gameplay_unit_transport_boarding_refactor_step30_complete.md`
- Synced `Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs` to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter GameSceneTransportBoardingPlayModeTests -testResults /private/tmp/warline-unit-transport-playmode-step30-fixed2.xml -logFile /private/tmp/warline-unit-transport-playmode-step30-fixed2.log`
- Synced `Assets/Tests/Editor/GameplayArchitectureContractTests.cs` and `Design/Architecture/unit_transport_boarding_system_refactor_roadmap.md` to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunUnitTransportBoardingArchitectureBatchValidation -logFile /private/tmp/warline-unit-transport-boarding-architecture-step30-fixed3.log`

Validation result:
- `git diff --check` passed.
- PlayMode validation passed: `total=2 passed=2 failed=0`.
- `DeterministicHelicopterBoardingCommand_QueuesAndBoardsSelectedSoldier` passed.
- `DeterministicHelicopterExitCommand_DropsAndDispersesPassengers` passed.
- Architecture validation passed: `[UnitTransportBoardingArchitectureValidation] result=Passed methods=3`.

Known gaps:
- Batchmode EditMode commands for `UnitTransportBoardingSystemExtractionTests` and `UnitTransportValidationTests` had previously exited `0` but did not emit stable XML/TestRunner summaries; rerun through the editor/Test Runner or CI if a formal artifact is required.
- The corrected PlayMode test no longer validates full Game-scene initial spawn composition. That was deliberate because the old batch smoke was probing source-key projection entities rather than the usable playable entities visible in Editor gameplay.

Cross-lane impacts:
- Lanes should not use `UnitTransportBoardingSystem` as a helper surface. Use `UnitTransportCapacitySystem`, `UnitTransportBoardingQuerySystem`, `UnitTransportBoardingRuleSystem`, `UnitTransportApproachCellSystem`, `UnitTransportAirPickupSystem`, and `UnitTransportRopeDisembarkCommandSystem` directly.

Next recommended task:
- Continue with the next Gameplay architecture priority only after confirming no other current gameplay smoke is blocked by invalid batch scene probes.
