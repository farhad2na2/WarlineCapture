Status: needs fixes
Topic:
M01 Gate 4 route captures depend on missing feedback marker assets
Docs reviewed:
- `Design/Art_Asset_Requirements_Register.md`
- `Design/Art_Asset_Requirements_Register.csv`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
Finding:
Gate 4 asks UI/QA to prove route-driven states for squad selected, move feedback, attack feedback, invalid command recovery, and result/objective flow. The M01 production contract requires visible feedback markers for those states, but the asset register still marks the core marker/VFX rows as missing: `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
Why it matters:
UI can technically produce route-driven screenshots without these final/approved marker assets, but QA would then be judging placeholder or absent feedback for the exact states that make the tactical route readable. That can produce a false Gate 4 pass or another loop where QA rejects screenshots because movement/attack/selection/destruction feedback is not AAA-quality or not visible enough.
Recommended fix:
Before final Gate 4 acceptance, route this as either:
- A UI/GamePlay temporary-evidence rule: the route-driven capture report must explicitly label marker/VFX feedback as temporary review evidence and not final asset approval; or
- An art/integration task to provide first-pass approved marker assets for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small` before QA/HCI final visual readability sign-off.
At minimum, QA/HCI rerun should call out whether those feedback states are absent, placeholder, or readable enough for current M01 validation.
Affected lanes:
- UI
- Gameplay
- QA/HCI
- Art/Support tracking
Needs user decision:
Not immediately for tooling. Yes before final art approval: the user should approve or reject the marker/VFX visual quality if screenshots rely on temporary assets.
Next task update needed:
Yes. Add marker/VFX status to the UI route-driven capture report requirements and QA/HCI rerun checklist, so Gate 4 does not accidentally treat missing marker assets as final approved feedback.
