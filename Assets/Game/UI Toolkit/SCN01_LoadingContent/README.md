# SCN01 Loading Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` as UI Toolkit assets.

- `SCN01_LoadingContent.uxml` keeps the original Canvas object names for binding-friendly element lookup.
- `SCN01_LoadingContent.uss` uses the same loading background, logo, loading frame, progress bar, fill, spinner, and status chip sprites.
- Runtime progress can bind to `Progress_Fill`, `LoadingPanel_Status`, and `LoadingPanel_Percent`.
