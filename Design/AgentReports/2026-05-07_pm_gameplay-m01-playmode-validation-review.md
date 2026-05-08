# PM Review: Gameplay M01 PlayMode Validation

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_gameplay_m01-playmode-validation.md`

## Decision

Accepted as the M01 technical PlayMode validation baseline.

Active manual HCI and balance QA remain blocked until the integrated player route, assistant runtime, HUD mount, and log/readability gates are ready.

## Validation Checked

- `/private/tmp/warlinecapture-m01-playmode-results.xml`: `Chapter01M01PlayModeValidationTests` passed 3/3.
- `/private/tmp/warlinecapture-m01-playable-results.xml`: `Chapter01M01PlayableRuntimeTests` passed 7/7.
- `/private/tmp/warlinecapture-chapter01-runtime-binding-results.xml`: `Chapter01TacticalRuntimeBindingTests` passed 4/4.
- `/private/tmp/warlinecapture-campaign-objective-results.xml`: `WarlineCaptureCampaignObjectiveTests` passed 7/7.
- `/private/tmp/warlinecapture-battlehud-bridge-results.xml`: `BattleHudGameplayBridgeConnectionTests` passed 6/6.

## Accepted Behavior

- M01 PlayMode runtime now places command squad and hostile patrol on metadata anchors in the Game scene.
- Camera start uses `camera.default_start` instead of jumping to generated base core for active M01.
- Selection, attack-state command execution, combat damage, survival guard, result readiness, and M01 build rejection are covered by focused tests.

## QA Caveat

The PlayMode result still logs repeated `NullReferenceException` entries, preview-scene leak warnings, and a `RuntimeCitySpawner` hitch. These do not fail the current automated tests, but QA/HCI should keep active balance testing blocked until gameplay either documents them as benign editor/test cleanup noise or fixes the underlying runtime issue.

## Cross-Lane Notices

- UI can now frame M01 HUD work around the stable `camera.default_start` opening and stable runtime ids.
- Support/FTUE can target `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and the M01 anchor ids for assistant recommendations.
- QA/HCI can use this as the technical smoke baseline, but should not start balance conclusions until manual player-route validation is available.

## Next Recommended Task

Gameplay should implement the assistant-facing typed command runtime hooks requested by the assistant runtime plan:

- `TrySelectRuntimeEntity`
- `TryIssueMoveToAnchor`
- `TryIssueAttackTarget`

Include focused validation and a short log-hygiene note covering the current PlayMode `NullReferenceException`, preview-scene leak, and `RuntimeCitySpawner` hitch state.
