Status: advisory
Topic: Assistant runtime wiring plan has stale open questions after accepted handoffs
Docs reviewed:
- `Design/AssistantRuntime_M01_Wiring_Plan.md`
- `Design/AssistantPanel_M01_Implementation_Contract.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md`
- `Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`
Finding:
- `AssistantRuntime_M01_Wiring_Plan.md` still has an `Open Questions / Blockers` table asking for public select/move/attack wrappers, selection state source, enemy visibility source, objective/result events, and the first takeover banner approach.
- The same plan's implementation checklist and later handoffs indicate several of these are already accepted or implemented: typed select/move/attack wrappers, live selection/patrol context mapping, command intent executor routing, live `AssistantContextProvider`, and UI assistant runtime binding with visible takeover/control status.
- `aria.takeover_banner` also exists as an asset-register row, so `POP-10 Assistant Takeover` is no longer an entirely untracked asset concept even if final art remains missing.
Why it matters:
- Agents reading the source plan may treat accepted work as still blocked and either stop for clarification or re-implement completed contracts.
- This is especially risky for Support/FTUE and UI because both lanes are currently waiting; stale blockers can make them drift into broad assistant refactors instead of staying focused on M01 critical-path validation.
Recommended fix:
- When the next documentation cleanup pass is assigned, update the wiring plan's `Open Questions / Blockers` table to distinguish:
  - Resolved by accepted handoff.
  - Implemented but awaiting integrated QA.
  - Still missing final art/asset approval.
- Do not reopen accepted typed command, context provider, or UI binding work unless QA/HCI finds a concrete regression.
Affected lanes:
- Support/FTUE
- UI
- QA/HCI
- Gameplay
Needs user decision:
- No immediate user decision required.
Next task update needed:
- Not needed during the current Gameplay sprite capture-fix task.
- Recommended before asking Support/FTUE or UI to resume broader assistant work after M01 visual evidence is accepted.
