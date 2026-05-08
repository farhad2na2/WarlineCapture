Gate: Gate 4 QA/HCI M01 Smoke And Readability
Status: accepted for Gameplay log-health slice; Gate 4 still needs integrated QA
Reason:
Gameplay updated the M01 log-health handoff after Unity licensing recovered. The focused validation now proves the generic AI build/production/squad log noise is fixed at source for active M01 fixed tactical gameplay, while non-M01 AI plans remain available.
Validation accepted:
- `Chapter01M01PlayableRuntimeTests` passed 9/9, including `FixedTacticalMissionGuardrail_DisablesGenericAIPlansOnlyWhenActive`.
- `Chapter01M01PlayModeValidationTests` passed 3/3 in graphics-enabled PlayMode.
- Static scan found no banned scene-search calls in touched gameplay files.
- The new graphics-enabled PlayMode log has no `AIProduction MissingProducerBuilding`, no `AIBuild Blocked`, and no `AISquad Waiting` entries.
- The new graphics-enabled PlayMode log has no `RenderTexture.Create failed`, no `NullReferenceException`, and no `EntitiesGraphicsSystemUtility.RootsHandlerDelegate` stack.
Validation still needed:
- QA/HCI integrated 16:9 and 20:9 M01 capture/log pass.
- QA/HCI classification of remaining editor-shutdown preview-scene and persistent allocation warnings during the integrated pass.
- Player/device validation if QA or PM decides editor/non-headless evidence is insufficient for final Gate 4.
Cross-lane notices:
- Gameplay can stop this log-health task unless QA/HCI finds a new gameplay-owned blocker.
- UI remains on the critical path for the integrated capture matrix.
- QA/HCI can use this accepted Gameplay evidence when running the next Gate 4 readiness pass.
- Support/FTUE has no new action.
Next gate/task:
UI should land `2026-05-07_ui_m01-integrated-capture-matrix.md`. QA/HCI should then run the integrated Gate 4 readiness review using a new report filename, not the earlier smoke-regression filename.
