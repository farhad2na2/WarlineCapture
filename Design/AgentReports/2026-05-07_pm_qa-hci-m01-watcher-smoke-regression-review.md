Gate: QA/HCI M01 smoke/readability
Status: needs fixes
Reason:
- The automated QA/HCI smoke baseline is green and materially improves confidence: gameplay runtime, tactical binding, sprite renderer, UI assistant binding, assistant runtime, assistant context, and command intent tests all passed in the QA workspace.
- Gate 4 is not accepted yet because the integrated human/player-route smoke pass and locked 16:9/20:9 capture matrix are still missing.
- Remaining PlayMode log warnings need player/device or non-headless classification before active balance QA can rely on timing/stability observations.
Validation accepted:
- `Chapter01M01PlayModeValidationTests`: passed 3/3.
- `Chapter01M01PlayableRuntimeTests`: passed 8/8.
- `Chapter01TacticalRuntimeBindingTests`: passed 6/6.
- `Chapter01M01SpriteRendererTests`: passed 4/4.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: passed 7/7.
- `M01AssistantRuntimeTests`: passed 9/9.
- `AssistantContextProviderTests`: passed 7/7.
- `CommandIntentExecutorTests`: passed 14/14.
- The old `RuntimeCitySpawner` / `FreezeDetect` hitch did not reproduce in this QA PlayMode smoke log.
- The accepted `M01_SpriteRenderer_CloseCapture.png` is valid current review-art evidence for unit grounding and scale, not final art approval.
Validation still needed:
- Integrated human/player-route smoke with visible UI/HUD/assistant surfaces.
- Locked 16:9 and 20:9 capture set for match start, squad selected, move feedback, attack feedback, invalid recovery, assistant open, assistant takeover/Stop, and result popup.
- Player/device or non-headless classification for package-side `NullReferenceException`, headless `RenderTexture.Create failed`, preview-scene leak warning, persistent allocation warning, and AI plan log noise.
- Final hostile non-color readability treatment and `vfx.unit.destroyed.small` remain open for final art/integration readiness.
Cross-lane notices:
- Gameplay owns the log/device classification and AI plan noise cleanup/classification.
- UI owns the locked capture matrix and visual verification of assistant ownership/status, player-input release, result-flow Stop, HUD occlusion, and 16:9/20:9 layout.
- Support/FTUE has no immediate code task unless UI/QA finds assistant recommendation behavior that fails the integrated route.
- QA/HCI remains the Gate 4 owner and should review the next Gameplay/UI handoffs before active balance QA starts.
Next gate/task:
- Dispatch Gameplay to classify/fix remaining log-health risks.
- Dispatch UI to run the locked integrated capture matrix.
- QA/HCI should remain waiting/watching until those handoffs land, then decide whether Gate 4 can pass or needs another focused fix.
