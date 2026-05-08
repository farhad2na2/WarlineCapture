Lane: Gameplay
Task: P1 M01 QA log-health classification and gameplay-owned AI log-noise fix.
Files changed:
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`
- `Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md`
Contracts touched:
- M01 fixed tactical runtime guardrail: when `Chapter01M01PlayableRuntime.IsActiveMission()` is active, generic `AIBuildPlan`, `AIProductionPlan`, and `AISquadPlan` ECS plans are disabled after bootstrap initialization.
- Preserved M01 authored runtime entity contract for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.
- Preserved sprite-presenter/destruction contract: `vfx.unit.destroyed.small` remains the destruction sprite contract; no separate `Destroyed` child was reintroduced.
User-visible behavior:
- M01 no longer runs the generic sandbox AI build/production/squad planners while the authored First Contact patrol encounter is active.
- Non-M01 AI plans are left available; the guardrail is gated by the active fixed tactical mission flag.
Validation run:
- Static scene-search scan: `rg -n "FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|GameObject\\.Find|Transform\\.Find|GetComponentInChildren" Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`
- Log-source scan: `rg -n "test-run|test-suite|test-case|failure|error|result=|RenderTexture.Create failed|NullReferenceException|EntitiesGraphicsSystemUtility|AIProduction|AIBuild|AISquad|preview scene|Leak Detected|FreezeDetect|RuntimeCitySpawner" /private/tmp/warlinecapture-m01-log-health-playmode-graphics-results.xml /private/tmp/warlinecapture-m01-log-health-playmode-graphics.log`
- Focused EditMode validation: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01M01PlayableRuntimeTests -testResults /private/tmp/warlinecapture-m01-log-health-editmode-results.xml -logFile /private/tmp/warlinecapture-m01-log-health-editmode.log`
- Focused graphics-enabled PlayMode validation: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-log-health-playmode-graphics-results.xml -logFile /private/tmp/warlinecapture-m01-log-health-playmode-graphics.log`
Validation result:
- Licensing recovered after restart: Unity connected to `LicenseClient-farhad-6000.4.0`, resolved entitlements, and completed both focused test runs.
- EditMode passed: `Chapter01M01PlayableRuntimeTests` 9/9, including `FixedTacticalMissionGuardrail_DisablesGenericAIPlansOnlyWhenActive`.
- Graphics-enabled PlayMode passed: `Chapter01M01PlayModeValidationTests` 3/3.
- Static scene-search scan passed: no banned runtime scene-search calls were introduced in touched files.
- Gameplay-owned AI noise fixed at source: the new graphics-enabled PlayMode log has no `AIProduction MissingProducerBuilding`, no `AIBuild Blocked`, and no `AISquad Waiting` entries.
- Non-headless/editor classification: the new graphics-enabled PlayMode log has no `RenderTexture.Create failed`, no `NullReferenceException`, and no `EntitiesGraphicsSystemUtility.RootsHandlerDelegate` stack. The earlier `RenderTexture.Create failed` is classified as headless/render-device startup noise from URP/RTHandle initialization; the earlier Entities Graphics root exception is package-side editor/headless noise, not gameplay-owned.
- Preview-scene leak and persistent allocation warnings still appear at graphics-enabled editor shutdown with no project gameplay stack in the scanned lines. They remain editor/tooling shutdown warnings for QA/PM tracking, not a gameplay source-code fix in this lane.
Known gaps:
- Player/device validation was not run in this pass. Strongest available evidence is the graphics-enabled editor PlayMode pass, which completed M01 runtime validation and removed the targeted gameplay AI log noise.
- Editor shutdown still reports `Leak detected: 2 preview scene(s)` and `Persistent allocates 24 individual allocations`; no gameplay stack was present in the scanned log lines.
- PlayMode output still includes existing non-task noise such as `Animator is not playing an AnimatorController`; not changed in this lane.
Cross-lane impacts:
- QA/HCI can rerun the integrated 16:9/20:9 M01 capture/log pass with the gameplay-owned AI spam fixed.
- UI and Support/FTUE contracts are unchanged.
- PM can treat the gameplay log-health portion as validated for focused editor/non-headless evidence, while keeping Gate 4 dependent on QA/HCI integrated captures and any required device/player pass.
Next recommended task:
- QA/HCI should rerun the integrated M01 smoke/capture pass at locked 16:9 and 20:9 and confirm visual readability, HUD occlusion, and final log-health status.
