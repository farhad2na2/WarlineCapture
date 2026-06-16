# UIShell App Canvas UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab` as a UI Toolkit shell scaffold.

- `UIShellAppCanvas.uxml` preserves the key composition names: `SafeAreaRoot`, `HeaderBar`, `ContentRoot`, `FooterBar`, `ModalOverlay`, `PlaceholderPopup`, and `TooltipLayer`.
- `ContentRoot` exposes named runtime slots for the existing shell regions: `MenuBackgroundRegion`, `LoadingLayer`, `HeaderRegion`, `LeftRegion`, `MiddleRegion`, `RightRegion`, and `FooterRegion`.
- Screen-specific art and layout stay in their own converted UXML folders. This shell asset only defines route, modal, and tooltip layers.
- `HeaderBar`, `FooterBar`, and `ModalOverlay` start hidden to match the inactive Canvas prefab objects.
