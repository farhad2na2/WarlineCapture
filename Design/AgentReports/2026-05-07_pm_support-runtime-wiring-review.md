Status: accepted
Reason: Support/FTUE completed the M01 ARIA assistant runtime wiring contract. The plan defines runtime ownership, live context flow, M01 recommendation transitions, typed Show Me / Do It / Stop intents, invalid-command recovery, save/session fields, UI button rules, control ownership, and validation targets without using screen-coordinate automation.
Validation accepted:
- Doc-level validation passed per the support report.
- The plan references current M01 ids, `AssistantPanelView.BindRecommendation`, `BattleHudGameplayBridge`, and `MissionDoesNotAllowBuild`.
- Screen-coordinate references are prohibitions/tests/non-goals, not implementation requirements.
Validation still needed:
- Unity/runtime tests are still future work.
- Gameplay must confirm or add public typed command wrappers: `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`.
- UI/FTUE must implement `AssistantPanelController`, `WarlineCaptureAssistantService`, `M01AssistantRecommendationProvider`, and related tests before M01 assistant runtime can be marked complete.
Cross-lane notices:
- Gameplay has a new dependency from the assistant runtime plan: stable typed command wrappers for ARIA `Do It`.
- UI can now continue from placeholders to the assistant presentation controller using the runtime wiring plan.
- Support/FTUE current task is complete and should be refreshed before its auto-continue loop repeats the same work.
Tracking updates:
- No dashboard update.
Next task:
- PM should refresh the support lane task. Recommended next support task: convert open questions from `WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md` into a short cross-lane checklist and keep it updated as gameplay/UI implement the runtime pieces.
