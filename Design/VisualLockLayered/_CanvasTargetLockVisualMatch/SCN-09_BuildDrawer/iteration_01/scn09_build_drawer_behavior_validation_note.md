# SCN-09 Build Drawer Behavior Validation Note

Date: 2026-06-24

Scope:
Validate the remaining SCN-09 Build Drawer checklist items without changing hierarchy, layout, or runtime C# behavior.

Evidence:

- Static visual capture: `shadow_canvas_scn09_build_drawer_sprite_pass_graphics_1920x1080.png`.
- Focused contact sheet: `shadow_canvas_scn09_build_drawer_sprite_pass_graphics_1920x1080_focused_contact.png`.
- Runtime view code:
  - `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`
  - `Assets/Game/Scripts/UI/Screens/BuildDrawerView.cs`
  - `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs`
  - `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`

Checks:

- Build progress panel default state:
  - The graphics-enabled capture shows the queue/progress area in the empty state with `NO PRODUCTION QUEUED`.
  - `BuildDrawerCatalogRuntimeView.ApplyEmptyQueue()` hides active and queued production rows, then calls `BuildDrawerView.ApplyQueueSummary(false, ...)`.
  - `BuildDrawerView.ApplyQueueSummary(false, ...)` hides `productionPanelActive` and shows `noProductionView`.
  - The ECS read model sets `NoProductionVisible = PendingProductions.Count == 0`.

- Tab and selected-card imagery:
  - `BuildDrawerCatalogRuntimeView.SelectCategory()` changes `_activeCategory` and calls `Refresh()`.
  - `Refresh()` calls `PopulateItems()`, which binds each card through `BuildDrawerItemView.BindThumbnail(model.CardPortrait)`.
  - `SelectItem()` calls `BindDetail(model)`.
  - `BindDetail(model)` passes `model.ActionPortrait` and `model.CardPortrait` into `BuildDrawerView.BindDetail(...)`, which updates the detail preview and thumbnail Images.
  - The ECS read model mirrors this with `ThumbnailSpriteKey` for catalog rows and `PreviewSpriteKey` for detail.

- Scroll/card clipping:
  - The focused drawer-card crop shows visible card chrome and card text/icons inside the scroll viewport.
  - The lower row is naturally clipped by the scroll viewport at the bottom edge, but no active card button/control chrome is clipped in the visible rows.

Decision:
The remaining SCN-09 checklist items are considered validated for the current sprite-only/no-structure scope. Any deeper live interaction proof should be a separate runtime input-test task, not part of this visual sprite-swap pass.
