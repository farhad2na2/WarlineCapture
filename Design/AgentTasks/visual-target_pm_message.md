# PM Message For Visual Target

Date: 2026-05-08

Create a gameplay-only M01 visual target package before PM asks the user for final selected-readability approval.

Important separation:

- Gameplay visual targets go under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- UI/HUD targets stay under `Design/VisualLock/` and `Design/VisualLockLayered/`.
- Do not mix gameplay target files into UI mockup folders.
- Use UI target mockups only for alignment, not ownership.

Reference for UI/HUD alignment:

- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`

Own the in-world gameplay target:

- soldier scale and silhouette,
- road/building/door scale,
- selected marker under soldiers,
- move/attack marker size,
- enemy readability,
- idle/run pose expectations,
- bad examples that must not pass.

Expected report:

`Design/AgentReports/2026-05-08_visual-target_m01-selected-readability-package.md`

Do not commit or push.
