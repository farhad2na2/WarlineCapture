# SCN08 Match HUD V02 Layer Notes

Target reference:

- `../reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png`

Unity sprites:

- `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/`

Rules applied:

- The generated chrome multipanel sheet is not imported into Unity.
- Panels that contain multiple nested sections are rejected for implementation because Toolkit needs independent panel frames, live text, and separately aligned content.
- The logo is used only as a separate logo lockup sprite. No panel with a baked logo is imported.
- The Unity V02 folder uses single-purpose frame sprites, live Toolkit labels, separate icons, and a no-UI background plate.

Rejected source retained only for traceability:

- `source/REJECTED_SCN08_V02_Chrome_Multipanel_DoNotImport.png`

Validation:

- `validation/SCN08_V02_icons_contact_sheet.png` confirms the V02 icon sheet was cut into independent sprites without visible green background.
