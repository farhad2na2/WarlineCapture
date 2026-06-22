# POP-07 Inbox - Blocked Missing Reference

Status: Blocked.

Reason:

No saved Target Lock reference image exists for POP-07 Inbox under `Design/VisualLockLayered`.

Existing UI Toolkit files:

- `Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uxml`
- `Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uss`

Required to continue:

- Add a canonical POP-07 Inbox Target Lock reference PNG under a `Design/VisualLockLayered/POP-07_Inbox/reference/` folder.
- Update `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md` to point POP-07 at that reference.
- Resume the same shadow-project UI Builder loop after the reference exists.

Validation:

- Confirmed missing reference with `find Design/VisualLockLayered -maxdepth 3 -type f | rg 'POP-07|Inbox|reference'`.
- Did not edit POP-07 UXML/USS because there is no visual target to compare against.
