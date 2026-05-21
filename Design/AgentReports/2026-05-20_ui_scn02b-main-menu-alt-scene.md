# Lane
UI

# Task
Create a separate Unity main menu canvas scene from the selected Synty-inspired command-tent target concept.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureMainMenuAltSceneBuilder.cs`
- `Assets/Game/Art/UI/Generated/MainMenuAlt/MainMenuAlt_CommandTarget_3840x2160.png`
- `Assets/Game/Art/UI/Generated/MainMenuAlt/MainMenuAlt_CommandTarget_3840x2160.png.meta`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_SyntyCommandTarget.prefab`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_SyntyCommandTarget.prefab.meta`
- `Assets/Game/Scenes/DesignTargets/SCN02B_MainMenu_SyntyCommandTarget.unity`
- `Assets/Game/Scenes/DesignTargets/SCN02B_MainMenu_SyntyCommandTarget.unity.meta`
- `Design/VisualLockLayered/SCN-02B_MainMenuAlt/reference/MainMenuAlt_CommandTarget_Source_1672x941.png`
- `Design/VisualLockLayered/SCN-02B_MainMenuAlt/reference/MainMenuAlt_CommandTarget_3840x2160.png`
- `Design/AgentReports/Captures/MainMenuAlt/MainMenuAlt_CommandTarget_1672x941.png`
- `Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_3840x2160.png`
- `Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_1672x941.png`

# Contracts touched
- New isolated design-target scene: `SCN02B_MainMenu_SyntyCommandTarget`.
- New isolated prefab: `Screen_MainMenu_SyntyCommandTarget`.
- Uses `WarlineCaptureRoute.MainMenu` for test route identity.
- Scene includes its own world-space Canvas, UICamera, and EventSystem.

# User-visible behavior
- Opening the new scene shows the selected command-tent main menu target.
- The visible UI is the selected target concept image for now, because this task explicitly did not require layered assets.
- Transparent Unity button hit zones are placed over Campaign, Operations, Skirmish, Store, Commander, Settings, top mail/settings, commander panel, cards, and Deploy Operation.

# Validation run
- Unity3 licensing-workaround build/capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -executeMethod WarlineCaptureMainMenuAltSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-mainmenu-alt-synty-command-final-unity3.log`
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureMainMenuAltSceneBuilder.cs Assets/Game/Art/UI/Generated/MainMenuAlt/MainMenuAlt_CommandTarget_3840x2160.png Assets/Game/Scenes/DesignTargets/SCN02B_MainMenu_SyntyCommandTarget.unity Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_SyntyCommandTarget.prefab Design/VisualLockLayered/SCN-02B_MainMenuAlt/reference/MainMenuAlt_CommandTarget_3840x2160.png`

# Validation result
- Unity build/capture passed.
- Final review capture:
  `Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_1672x941.png`
- `git diff --check` passed.

# Known gaps
- This is not layered yet; the visual is a full-screen concept target with transparent Unity hit zones.
- Text, panels, and icons are not individually editable until this target receives layer generation or frame-first extraction.

# Cross-lane impacts
- PM/Design can review this as a separate visual-direction scene without affecting the existing SCN-02 main menu workflow.
- UI can later convert this target to the saved frame-first workflow if it is approved.

# Next recommended task
If this visual direction is accepted, request/generate layered assets for this exact target and convert it to a frame-first editable canvas.
