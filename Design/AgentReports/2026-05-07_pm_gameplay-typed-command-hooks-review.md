# PM Review: Gameplay Assistant Typed Command Hooks

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_gameplay_assistant-typed-command-hooks.md`

## Decision

Accepted as the gameplay-owned M01 assistant command execution boundary.

## Validation Checked

- `/private/tmp/warlinecapture-m01-assistant-command-results.xml`: `M01AssistantCommandRuntimeTests` passed 10/10.
- `/private/tmp/warlinecapture-m01-playable-results.xml`: `Chapter01M01PlayableRuntimeTests` passed 7/7.
- `/private/tmp/warlinecapture-chapter01-runtime-binding-results.xml`: `Chapter01TacticalRuntimeBindingTests` passed 4/4.
- `/private/tmp/warlinecapture-battlehud-bridge-results.xml`: `BattleHudGameplayBridgeConnectionTests` passed 6/6.
- `/private/tmp/warlinecapture-campaign-objective-results.xml`: `WarlineCaptureCampaignObjectiveTests` passed 7/7.
- `/private/tmp/warlinecapture-m01-playmode-results.xml`: `Chapter01M01PlayModeValidationTests` passed 3/3.

## Accepted Behavior

- Gameplay now exposes bounded M01 command hooks for `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`.
- The hooks resolve locked M01 ids/anchors and route through selection, move, attack, and HUD command-result feedback instead of UI child paths or screen coordinates.
- Invalid ids, missing selection, missing anchors, dead squad, dead patrol, and M01 build attempts return rejected command results with bridge-compatible reason codes.

## Known Gaps Accepted

- This is the gameplay execution layer only. It does not implement assistant recommendation state, Show Me highlighting, path preview, ownership, or persistence.
- PlayMode still logs the known `EntitiesGraphicsSystemUtility.RootsHandlerDelegate` `NullReferenceException`, preview-scene leak warning, and `RuntimeCitySpawner` hitch. Keep this as a QA/perf cleanup gate before active balance QA.

## Cross-Lane Notices

- Support/FTUE can now connect `CommandIntentExecutor` / `Do It` actions to the accepted gameplay hooks.
- UI can keep button execution behind typed assistant intents instead of adding gameplay logic in the panel.
- QA/HCI can validate select, move, attack, and build-lock recovery as typed runtime behavior after Support/FTUE wires the executor.

## Next Recommended Task

Support/FTUE should connect assistant `Do It` actions through a `CommandIntentExecutor` boundary that calls these gameplay hooks, then UI should validate live button enabled/disabled states and ownership feedback.
