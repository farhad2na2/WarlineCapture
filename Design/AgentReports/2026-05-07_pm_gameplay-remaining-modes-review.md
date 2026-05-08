Status: accepted
Reason: Gameplay completed the remaining BattleHud bridge modes for Hold, Stop, Build, and Special, added focused coverage, and reported passing bridge and match overlay tests. The remaining Hold/Stop visual-button invocation is a UI-lane follow-up, not a gameplay blocker.
Validation accepted:
- `git diff --check` on changed gameplay/UI files passed.
- `BattleHudGameplayBridgeConnectionTests` passed 4/4.
- `WarlineCaptureUiMatchOverlayTests` passed 15/15.
Validation still needed:
- Manual scene validation remains pending.
- UI must connect visual Hold/Stop controls to `RTSSelectionSystem.IssueHoldPositionOrder` and `RTSSelectionSystem.IssueStopOrder` if not already wired.
Cross-lane notices:
- UI can assume `BattleHudGameplayBridge` supports Move, Attack, Hold, Stop, Build, and Special command modes.
- FTUE/support can reference the shared command-mode contract for future steps, but M01 assistant scope should remain select/move/attack/invalid recovery unless explicitly expanded.
Tracking updates:
- No dashboard update yet; this closes a cross-lane handoff but does not materially change project completion estimates.
Next task:
- Gameplay should continue M01 production by wiring metadata-driven playable spawn/objective/result flow for `saga.ch01.m01.first_contact`.
