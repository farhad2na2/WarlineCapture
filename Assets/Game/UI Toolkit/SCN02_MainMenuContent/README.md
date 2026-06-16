# SCN02 Main Menu Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` as UI Toolkit assets.

- `SCN02_MainMenuContent.uxml` keeps the main Canvas section names and interaction element names for binding-friendly lookup.
- `SCN02_MainMenuContent.uss` uses the same generated MainMenuV15C layered sprites for the background, header, mode cards, commander panel, footer status, and deploy command.
- Future runtime binding points include `Nav_*`, `Card_*`, `InboxButton`, `SettingsButton`, `CommanderPanel`, `DeployOperationButton`, and the resource value labels.
