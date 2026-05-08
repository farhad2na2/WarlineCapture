Lane: Support/FTUE
Task: Created the M01 implementation contract for `PREFAB-05_AssistantPanel` and ARIA recommendation states, then referenced it from the FTUE design, UI alignment doc, Design README, and root README.
Files changed:
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/README.md`
- `README.md`
- `Design/AgentReports/2026-05-07_support-ftue_assistant-panel-contract.md`
Contracts touched: `PREFAB-05_AssistantPanel`, M01 FTUE ids, M01 runtime ids, ARIA Show Me / Do It / Stop behavior, player-input cancellation boundaries, `BattleHudGameplayBridge` select/move/attack/invalid-command feedback dependency, asset-register implications.
User-visible behavior: No runtime behavior changed. UI/gameplay/FTUE agents now have a concrete handoff for the assistant panel, M01 recommendation states, safe assistant control takeover, visible Stop/cancel behavior, and live data requirements.
Validation run:
- `rg` checked M01 mission/runtime/FTUE ids against `WarlineCapture_M01_FirstContact_Production_Contract.md` and `WarlineCapture_FTUE_And_Command_Assistant_Design.md`.
- `rg` checked `BattleHudGameplayBridge` methods, `TacticalCommandMode`, and `TacticalCommandReasonCode` names against `BattleHudGameplayBridge.cs` and `WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`.
- `rg` checked `AssistantPanelView` exposed fields and `BindRecommendation` against `AssistantPanelView.cs`.
- `rg` checked coordinate references in the new contract.
- `git diff --check -- README.md Design/README.md Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
Validation result: Passed doc-level validation. The contract references current locked M01 ids, current bridge methods/reason codes, and current assistant panel serialized field names. Coordinate mentions are prohibitions/non-goals only; no ARIA action requires screen coordinates. Stop/cancel affordances and player-control return rules are explicitly required.
Known gaps: No Unity tests were run because this is a documentation/contract task. `PREFAB-05.CloseButton` and `PREFAB-05.Portrait` are marked optional first pass because the current `AssistantPanelView` does not expose them. Hold/Stop/Build/Special bridge wiring remains a gameplay-lane gap and is explicitly not claimed as complete for M01 assistant coverage.
Cross-lane impacts: UI should implement/test `PREFAB-05_AssistantPanel` against the new element ids and live TMP/button requirements. Gameplay should feed selection, Move, Attack, and rejected-command feedback through `BattleHudGameplayBridge`. FTUE/support should use typed ids and bounded command intents for Show Me / Do It, not screen coordinates or arbitrary UI clicks. Art should keep flat assistant references as visual targets only until asset-register rows are approved.
Next recommended task: UI lane should add focused assistant panel validation for required fields, live text binding, and Show Me / Do It / Stop button states, then gameplay/FTUE can wire M01 recommendation state production from live match context.
