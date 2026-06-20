# SCN03 Commander/Profile Content UI Toolkit Conversion

This folder contains the UI Toolkit Commander/Profile content screen for the bright command-table art direction.

- This screen is mounted into `CommanderProfileScreenSlot`; it does not include its own header because the Main Menu header is persistent.
- Canvas behavior/text parity source: `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`.
- New-art assets are reused from `MainMenuBrightCommand` and `Armory/LayeredOneGo`.
- Do not reference stale Commander/Profile generated folders from this screen.
- Runtime binding should keep views as cached references only; route/back/tab actions must enqueue ECS shell requests.
