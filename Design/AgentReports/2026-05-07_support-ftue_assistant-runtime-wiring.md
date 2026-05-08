Lane: Support/FTUE
Task: Created the M01 ARIA assistant runtime wiring contract for service ownership, live context data flow, recommendation transitions, typed Show Me / Do It / Stop intents, invalid-command recovery, save/session fields, UI button rules, control ownership, validation tests, and blockers.
Files changed:
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/README.md`
- `README.md`
- `Design/AgentReports/2026-05-07_support-ftue_assistant-runtime-wiring.md`
Contracts touched: `WarlineCaptureAssistantService`, `AssistantContextProvider`, `M01AssistantRecommendationProvider`, `AssistantPanelController`, `AssistantHighlightController`, `CommandIntentExecutor`, `AssistantControlOwner`, `TutorialSessionState`, `TutorialSaveData`, `AssistantPanelView.BindRecommendation(title, body, chips, canShow, canExecute, canStop)`, M01 ARIA states, typed intents for select/move/attack/objective focus/Stop, `BattleHudGameplayBridge` rejected-command recovery, M01 save/session replay suppression, and validation test names.
User-visible behavior: No runtime behavior changed. The UI/gameplay/FTUE lanes now have a concrete runtime handoff for producing and executing M01 assistant recommendations without screen-coordinate automation or mission autopilot behavior.
Validation run:
- `rg` checked the runtime plan's M01 ids against `WarlineCapture_M01_FirstContact_Production_Contract.md`, `Chapter01M01PlayableRuntime.cs`, and `BattleHudGameplayBridge.cs`.
- `rg` checked service/type names and binding contracts against `WarlineCapture_FTUE_And_Command_Assistant_Design.md`, `AssistantPanelView.cs`, and `BattleHudGameplayBridge.cs`.
- `rg` checked screen-coordinate references in the runtime plan.
- `git diff --check -- README.md Design/README.md Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
Validation result: Passed doc-level validation. The plan uses locked M01 ids, current bridge reason codes including `MissionDoesNotAllowBuild`, current `AssistantPanelView` button availability binding, and typed intent targets only. Screen-coordinate mentions are prohibitions/tests/non-goals.
Known gaps: No Unity tests were run because this is a planning/contract task. The actual runtime services and public gameplay wrappers for `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget` still need implementation or confirmation from gameplay.
Cross-lane impacts: UI should bind the assistant panel through `AssistantPanelController` and `AssistantPanelView` only. Gameplay should expose stable selection/move/attack wrappers and selected/visible entity state for M01. FTUE should implement recommendation production from `AssistantContextProvider` snapshots and preserve player-control cancellation boundaries. PM should track the open questions in the runtime plan before marking M01 assistant runtime complete.
Next recommended task: Gameplay should confirm or add the public typed command wrappers required by `CommandIntentExecutor`, then FTUE/UI can implement `M01AssistantRecommendationProvider`, `AssistantPanelController`, and the focused `M01AssistantRuntimeTests`.
