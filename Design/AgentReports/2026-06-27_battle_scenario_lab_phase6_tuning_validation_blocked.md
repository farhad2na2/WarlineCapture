# Battle Scenario Lab Phase 6 Tuning Validation Blocked Then Resolved

Date: 2026-06-27

## Scope

Implemented the approved Phase 6 deterministic air-defense tuning slice:

- Promoted radar/satellite support values into `AirDefenseSupportTuning`.
- Updated live `Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset` radar support values:
  - radar support range bonus: `100`
  - radar lock time multiplier: `0.5`
  - radar tracking bonus: `0.2`
  - radar turn-rate bonus: `50`
- Routed `UnitGridAuthoring`, `BuildingRuntimeEntityCompositionSystemHelper`, and Scenario Lab support-provider setup through the shared constants.
- Kept V1 deterministic; no stochastic hit chance was added.

## Static Validation

- `git diff --check`: passed.
- Old-value scan for the previous radar support values in relevant runtime/config paths: clean.

## Blocked Unity Validation Attempts

Initial post-tuning Scenario Lab suite reruns did not reach tests because Unity entered the known licensing loop before executing the suite.

Attempt 1, main project:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite -logFile /private/tmp/warline-scenario-lab-suite-phase6-tuning.log
```

Result: blocked before tests by `LicenseClient-farhad` unsupported protocol / reconnect loop.

Attempt 2, documented shadow-project workaround:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite -logFile /private/tmp/warline-scenario-lab-suite-phase6-tuning-shadow.log
```

Result: blocked before tests by licensing reconnect/headless loop, including `com.unity.editor.headless was not found`.

Attempt 3, non-headless shadow-project fallback:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite -logFile /private/tmp/warline-scenario-lab-suite-phase6-tuning-shadow-ui.log
```

Result: blocked before tests by `LicenseClient-farhad` unsupported protocol / reconnect loop.

## Resolution

The licensing issue later cleared during the same work session. The selector-enabled manual scene was regenerated and smoke validated, then the full post-tuning Scenario Lab suite passed.

Successful commands:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabSceneBuilder.CreateManualSceneShell -logFile /private/tmp/warline-scenario-lab-create-scene-selector.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabValidationRunner.ValidateManualSceneSmoke -logFile /private/tmp/warline-scenario-lab-selector-smoke.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite -logFile /private/tmp/warline-scenario-lab-suite-phase6-tuning-rerun.log
```

Result: AD-001 through AD-010 plus GM-001 and DR-001 all passed and were non-skipped. Suite index: `/private/tmp/warline-scenario-lab-suite-index.json`.
