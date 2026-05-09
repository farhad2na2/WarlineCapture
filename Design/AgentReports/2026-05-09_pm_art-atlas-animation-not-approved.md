# PM Review - Soldier Animation Atlas Not Approved

Lane: PM
Task: Retract prior animation approval and route Art/Atlas v2 fix
Files changed:
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-animation-not-approved.md`
Contracts touched:
- M01 AI production asset approval gate
- Soldier atlas animation acceptance
- Gameplay runtime integration gate
User-visible behavior:
- No runtime integration should proceed from the current rejected soldier animation atlas.
- Strategic map remains approved.
- Soldier animations remain blocked until Art/Atlas produces real frame-by-frame motion sequences.
Validation run:
- PM review of latest user feedback in-thread.
Validation result:
- Rejected. User clarified that approval is stopped because run frames appear to repeat the same pose, and this may affect all sequences.
Known gaps:
- Need corrected player rifle squad and enemy patrol animation atlases with visible per-frame pose progression for idle, run, aim, fire, damaged, and death across required facings.
- Need review evidence/contact sheets or previews proving the sequences are animated rather than duplicated still poses.
Cross-lane impacts:
- Art/Atlas owns the v2 fix.
- Gameplay is blocked from integrating the current soldier animation atlas.
- QA/HCI is blocked until Art/Atlas v2 is accepted and Gameplay produces a runtime capture.
Next recommended task:
- Art/Atlas should write `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md` after producing corrected motion-varied sprite sequences.
