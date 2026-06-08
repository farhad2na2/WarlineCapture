Status: advisory
Topic: Commander Identity screen is defined but has no active lane owner
Docs reviewed:
- `Design/FTUE_And_Command_Assistant_Design.md`
- `Design/Art_Asset_Requirements_Register.md`
- `Design/Art_Asset_Requirements_Register.csv`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
Finding:
- `POP-11 Commander Identity` is well defined in the FTUE design and asset register, including commander name, six default portraits, profile entry points, layer pack, and Unity prefab rows.
- Active lane tasks do not currently assign implementation ownership for the Commander Identity surface. `M01_CRITICAL_PATH.md` explicitly tracks it separately and says it must not block M01 tactical validation unless the user makes it required for first launch.
- UI is waiting for QA/HCI after Gameplay evidence, and Support/FTUE is waiting on assistant integration. Neither lane currently owns `POP-11 CommanderIdentityPopup.prefab`, commander save fields, first-launch routing, or default portrait asset production.
Why it matters:
- The user has already asked where the screen is. If agents continue with only `continue`, none of the active lanes will naturally build this screen because it is intentionally out of the current M01 critical path.
- This can create an expectation mismatch: the design says first launch should include Commander Identity, but the current execution plan treats it as deferred.
Recommended fix:
- Keep Commander Identity deferred until M01 tactical route is stable, unless the user explicitly promotes it into the first-launch MVP.
- When promoted, split ownership clearly:
  - UI: `POP-11 CommanderIdentityPopup.prefab`, profile/main-menu entry hooks, responsive rendered captures.
  - Support/FTUE: first-launch step data, guidance-level ordering, profile save/load contract.
  - Art/PM: six default AI-generated commander portraits plus frame/card-state approval at intended UI sizes.
- Add a dedicated task file when this starts, rather than letting UI or Support/FTUE infer it from the broad FTUE document.
Affected lanes:
- UI
- Support/FTUE
- Art/PM
- QA/HCI
Needs user decision:
- Not immediately. The current plan can keep this deferred while M01 tactical validation continues.
- Before first-launch MVP approval, the user/PM must decide whether Commander Identity is required before M01 or can ship after the first playable tactical slice.
Next task update needed:
- None for the current M01 gameplay capture-fix lane.
- If promoted, create a new active task for UI and Support/FTUE instead of overloading the current waiting tasks.
