# SCN-19 Armory V3 Work In Progress

Status: source rebuild staged; Unity build, focused validation, and live visual
comparison are pending.

Canonical target lock:
`reference/SCN-19_ArmoryV3_Target.png`

The file named `SCN-19_ArmoryV3_Final_Target.png` is a contact sheet and is not
the implementation target.

## Staged Source

- `Assets/Game/Scripts/Editor/ArmoryV3PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryV3CategoryTabVisual.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryV3CatalogItemVisual.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs`
- `Assets/Game/Scripts/UI/Screens/ArmoryCatalogItemView.cs`
- `Assets/Tests/Editor/ArmoryV3PrefabTests.cs`
- exact-size capture entry points in
  `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`

The implementation reuses the existing unit/building catalog portraits, uses
shared V3 icons, procedural directional gradients, and independent 3 px borders.
It must not be moved into `iterations/` or described as matched until the actual
Play Mode captures at 1920x1080 and 4800x2160 both pass visual comparison.

## Pending Commands

Run only through `Tools/CI/invoke_unity_macos.sh` after the macOS login session
is unlocked:

```text
-quit -executeMethod Game.Editor.ArmoryV3PrefabBuilder.Build
-quit -executeMethod ArmoryV3PrefabTests.RunFocusedValidation
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureArmory1920x1080
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureArmory4800x2160
```

