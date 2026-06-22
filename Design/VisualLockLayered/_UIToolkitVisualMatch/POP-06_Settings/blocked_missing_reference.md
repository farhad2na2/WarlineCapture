# POP-06 Settings - Blocked Missing Reference

Status: Blocked.

Reason:

No saved Target Lock reference image exists for POP-06 Settings under `Design/VisualLockLayered`.

Existing UI Toolkit files:

- `Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uxml`
- `Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uss`

Required to continue:

- Add a canonical POP-06 Settings Target Lock reference PNG under a `Design/VisualLockLayered/POP-06_Settings/reference/` folder.
- Update `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md` to point POP-06 at that reference.
- Resume the same shadow-project UI Builder loop after the reference exists.

Validation:

- Confirmed missing reference with `find Design/VisualLockLayered -maxdepth 3 -type f | rg 'POP-06|Settings|reference'`.
- Did not edit POP-06 UXML/USS because there is no visual target to compare against.
