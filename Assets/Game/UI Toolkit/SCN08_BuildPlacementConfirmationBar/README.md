# SCN08 Build Placement Confirmation Bar UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab` as UI Toolkit assets.

- `SCN08_BuildPlacementConfirmationBar.uxml` preserves the runtime reference names from the Canvas prefab: `Title`, `Status`, `Cost`, `Duration`, `Instruction`, `CancelButton`, `RotateButton`, and `ConfirmButton`.
- The root is a full-screen overlay so the confirmation bar can sit in the same match HUD shell region as the Canvas version.
- The actual bar uses the same placement ratios from `BuildPlacementConfirmationBarPrefabSetupEditor`.
- The styles use the cleaned panel/icon sprites already assigned by the Canvas prefab.
