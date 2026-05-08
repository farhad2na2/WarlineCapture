Status: advisory
Topic: PREFAB-05 Assistant Panel UI/support dependency
Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`
Finding:
The UI lane is assigned to start the `PREFAB-05_AssistantPanel` Unity Canvas prefab while the support/FTUE lane is assigned to write the detailed implementation contract for the same surface. If UI goes beyond shell/hierarchy/chrome before the support contract lands, it may invent data fields, recommendation ids, Show Me / Do It semantics, or runtime binding names.
Why it matters:
ARIA is a cross-lane feature. Guessing behavior in the UI prefab can create mismatches with gameplay command intents, FTUE state, and player-control/takeover rules.
Recommended fix:
Allow UI to proceed only on visual shell, hierarchy, live TMP labels, and placeholder binding seams. Runtime data binding, recommendation ids, and button semantics should wait for the support/FTUE contract or be explicitly marked placeholder.
Affected lanes:
- UI
- Support/FTUE
- Gameplay, indirectly through `BattleHudGameplayBridge` and typed command intents.
Needs user decision:
No.
Next task update needed:
Yes. Tighten `Design/AgentTasks/ui_current.md` so UI does not define final ARIA runtime behavior before the support/FTUE contract lands.
