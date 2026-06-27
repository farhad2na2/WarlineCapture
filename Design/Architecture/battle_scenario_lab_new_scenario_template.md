# Battle Scenario Lab New Scenario Template

Use this template when adding a new isolated battle scenario after AD-001.

## Scenario Contract

- Scenario id: `DOMAIN-###_ShortStableName`
- Asset path: `Assets/Game/Configs/ScenarioLab/<ScenarioId>.asset`
- Runner entry point: editor-safe method that accepts `BattleScenarioDefinition` and returns `BattleScenarioResult`.
- Live balance rule: do not tune live combat values in the scenario implementation unless the tracker explicitly moves to Phase 6 and the user approves.
- Runtime rule: no UI Toolkit and no MonoBehaviour gameplay `Update()` loops. MonoBehaviours may only hold scene references, bootstrap a run, display passive metrics, or receive button events.

## Definition Checklist

- Create or update a `BattleScenarioDefinition` asset.
- Fill `scenarioId`, `displayName`, `description`, `fixedDeltaTime`, `maxDurationSeconds`, `randomSeed`, `cameraPreset`, and `worldBounds`.
- Add one or more `BattleScenarioVariant` entries with stable `VariantId` values.
- Add success criteria that express the behavior being measured.
- Keep setup data deterministic unless the scenario explicitly tests seeded randomness.

## Runner Checklist

- Build an isolated ECS `World`.
- Spawn only the entities required by the scenario.
- Use existing gameplay ECS systems wherever practical.
- Run systems in a fixed, explicit order.
- Capture metrics during the run, not after guessing from final state only.
- Return a `BattleScenarioResult` with per-variant metrics and comparisons.
- Write failure reasons using `BattleScenarioFailureReason`.
- Add focused EditMode tests for metrics and success criteria.

## Manual Scene Checklist

- Reuse `BattleScenarioLab.unity` when the same scene shell is enough.
- Add only reference/bootstrap/view/button-event MonoBehaviours.
- Add or update passive scene markers for the scenario.
- Make the visual overlay display the same metrics that the JSON report records.
- Save proof images under `Design/VisualLockLayered/_BattleScenarioLab/<ScenarioId>/`.

## Suite Runner Registration

The suite runner currently discovers `BattleScenarioDefinition` assets in `Assets/Game/Configs/ScenarioLab`.

When a new runner is implemented:

- Register the scenario id in `BattleScenarioLabSuiteRunner.RunDefinition`.
- Write an individual report to `/private/tmp/warline-scenario-lab-<ScenarioId>.json`.
- Keep skipped future scenario assets reported as `Skipped: true` until their runner exists.

## Batch Commands

Create or update AD-001 definition:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabValidationRunner.CreateOrUpdateAd001DefinitionAsset -logFile /private/tmp/warline-scenario-lab-create-ad001.log
```

Run AD-001 metrics:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabValidationRunner.RunAirDefenseAd001 -logFile /private/tmp/warline-scenario-lab-ad001.log
```

Validate the manual scene:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabValidationRunner.ValidateManualSceneSmoke -logFile /private/tmp/warline-scenario-lab-scene-smoke.log
```

Capture AD-001 visual proof:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabVisualProofCapture.CaptureAd001VisualProof -logFile /private/tmp/warline-scenario-lab-visual-proof.log
```

Run the scenario suite:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite -logFile /private/tmp/warline-scenario-lab-suite.log
```

Suite index output:

```text
/private/tmp/warline-scenario-lab-suite-index.json
```
