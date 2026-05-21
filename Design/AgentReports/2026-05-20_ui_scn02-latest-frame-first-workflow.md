# Lane
UI

# Task
Save the latest accepted SCN-02 main menu workflow so work can resume without falling back to old baked-panel approaches.

# Files changed
- `Design/VisualLockLayered/SCN-02_MainMenu/LATEST_WORKFLOW.md`
- `Design/AgentReports/2026-05-20_ui_scn02-latest-frame-first-workflow.md`

# Contracts touched
- SCN-02 latest workflow is now explicitly frame-first.
- `scn02_component_menu_layout.json` is the generated frame-first manifest.
- `scn02_component_slot_report.json` is the generated safe-rect/child-slot report.
- Unity build/capture still uses `WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest`.

# User-visible behavior
- Future SCN-02 work should resume from the frame-first canvas approach, not the older composite-plate approach.
- Icons, badges, meters, locks, art, and text should remain separate child objects inside panel safe rects.

# Validation run
- Documentation-only save for the latest workflow.

# Validation result
- Latest accepted review capture remains:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- Latest Unity capture log remains:
  `/private/tmp/warlinecapture-scn02-framefirst-final-unity3.log`

# Known gaps
- This does not claim pixel-perfect target matching.
- Remaining mismatch is primarily source sprite/frame style and proportions, not hidden baked child overlap.

# Cross-lane impacts
- PM/QA should treat the frame-first capture as the latest UI baseline.
- Art can improve frame/source sprites without changing the canvas workflow.

# Next recommended task
For the next SCN-02 pass, continue from `Design/VisualLockLayered/SCN-02_MainMenu/LATEST_WORKFLOW.md` and keep the frame-first safe-rect validation in place.
