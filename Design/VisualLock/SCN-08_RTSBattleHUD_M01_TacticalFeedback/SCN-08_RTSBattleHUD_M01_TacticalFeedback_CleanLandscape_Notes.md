# SCN-08_RTSBattleHUD_M01_TacticalFeedback

State target promoted from `Candidates/SCN-08_RTSBattleHUD_M01_TacticalFeedback_CleanTarget_Candidate_v2_alt.png` on 2026-05-07 after rejecting the earlier rough tactical-feedback target.

This target is not a replacement for the base HUD chrome. It is the clean M01 tactical-feedback state target for `saga.ch01.m01.first_contact`.

Required new UI items shown here:

- `BattleHud.SelectedEntityPanel`
- `BattleHud.CommandModeBanner`
- `BattleHud.WorldCommandMarkerLayer`
- `BattleHud.InvalidCommandToast`
- `BattleHud.MinimapCameraBridge`
- selection, move, attack, invalid, objective/minimap feedback over a close-up tactical map, not a strategic preview

Canvas implementation must use `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback` before prefab work. Do not use the previous rough target as an implementation gate.
