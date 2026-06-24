# SCN-08 Match HUD Canvas Runtime-Bound Inventory

Purpose:
Record the protected runtime bindings for the SCN-08 Canvas Match HUD before Target Lock visual edits. This artifact is the guardrail for the Phase 4 HUD pass: visual work can change sprites, sliced Image setup, PPU/multiplier, RectTransform sizing, padding, masks, and Canvas Button transitions, but it must not rename/remove runtime-bound fields, route sections, or expected Button/Image/Text targets.

Source prefab:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`

Reference:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN08 Match HUD.png`

Related later Phase 4 surfaces:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab`
- `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`
- `Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab`

## Shell Install Contract

`UIShellContentView.InstallMatchHud()` installs `SCN08_MatchHudContent.prefab` into four Canvas shell regions:

- `Header` section into `HeaderRegion`.
- `Left` section into `LeftRegion`, then binds `MatchHudSelectionPanelView`.
- `Right` section into `RightRegion`, then binds `MatchHudRightQuickRailView`.
- `Footer` section into `FooterRegion`, then binds `MatchHudFooterContentView`.
- `MiddleRegion` is cleared for the match HUD.

Do not convert the Match HUD to the shared menu header. SCN-08 owns a unique gameplay header.

## Protected Root/Section Names

Keep these roots and section identities stable:

- `SCN08_MatchHudContent`
- `HeaderContent`
- `LeftContent`
- `RightContent`
- `Footer`
- `FooterContent`
- `BattlefieldLayer`
- `TransportPassengerDrawer`
- `RightQuickRail`
- `CommandRail`
- `CommandButtons`
- `SquadTray`
- `MinimapPanel`
- `ObjectivesPanel`
- `SelectedSquadPanel`

Visual edits may add decorative child Images under these roots when needed, but must keep the bound components and their serialized object references intact.

## Header / Objectives Runtime Bindings

Component:

- `MatchHudObjectivesElapsedView`

Protected serialized field:

- `elapsedText`

Visible prefab/object family:

- `HeaderContent`
- `ResourceStrip`
- `CreditsSlot`
- `FuelSlot`
- `SupplySlot`
- `CivilianRiskSlot`
- `ObjectivesPanel`
- `Elapsed`
- `KeepLossesLow`
- `ProtectCivilians`
- `NeutralizeHostiles`

Visual pass notes:

- This is the first Phase 4 visual target after inventory.
- Keep the HUD/gameplay header separate from the approved menu header.
- Tune HUD resource/objective panel PPU and 9-slice before moving text.
- Preserve elapsed text binding and objective labels.

## Left Selection Panel Runtime Bindings

Component:

- `MatchHudSelectionPanelView`

Protected serialized fields:

- `selectedSquadPanel`
- `selectedPortraitImage`
- `titleText`
- `subtitleText`
- `currentOrderText`
- `healthFillImage`
- `healthText`
- `badgeRoot`
- `badgeImage`
- `returnAction`
- `destroyAction`
- `boardAction`
- `passengerChipRoot`
- `passengerChipButton`
- `passengerChipLabel`
- `passengerDrawer`
- fallback portrait sprites for generic, soldier, vehicle, aircraft, transport, building, and mixed-force selections

Visible prefab/object family:

- `SelectedSquadPanel`
- `Portrait`
- `PortraitFrame`
- `Title`
- `Subtitle`
- `CurrentOrderBanner`
- `OrderLabel`
- `OrderText`
- `OrderValue`
- `HealthFrame`
- `HealthFill`
- `HealthText`
- `Badge`
- `ReturnButton`
- `DestroyButton`
- `BoardButton`
- `PassengerChip`
- `TransportPassengerDrawer`

Visual pass notes:

- `boardAction` selected state is applied through `boardAction.targetGraphic as Image` plus `button.spriteState.selectedSprite`; keep its visible target graphic valid.
- `healthFillImage` is runtime-driven; preserve the fill Image and do not replace it with a static bar.
- Passenger chip and passenger drawer are runtime-visible; keep them readable but hidden/default states should not disrupt the static HUD capture.

## Right Quick Rail Runtime Bindings

Component:

- `MatchHudRightQuickRailView`

Protected serialized field:

- `buildButton`

Visible prefab/object family:

- `RightContent`
- `RightQuickRail`
- `BuildCommand`

Visual pass notes:

- Hit testing prefers `buildButton.targetGraphic.rectTransform`; the Button target graphic must remain the visible full-frame chrome or an equivalent rect that covers the intended clickable area.
- Build button needs default, hover/focus, selected/current, pressed/impact, and disabled states before the minimap/right-rail family is considered done.

## Footer Content Runtime Bindings

Component:

- `MatchHudFooterContentView`

Protected serialized fields:

- `commandControls`
- `runtimeFeedback`
- `minimap`
- `squadTray`

Visual pass notes:

- The footer is a composed runtime surface. Do not flatten command controls, feedback, minimap, and squad tray into one baked background.
- Split each family into separate live Canvas panels with safe padding and repeated rhythms.

## Command Controls Runtime Bindings

Component:

- `MatchOverlayCommandControlsView`

Protected serialized fields:

- `selectButton`
- `moveButton`
- `attackButton`
- `scanButton`
- `buildButton`
- `holdButton`
- `stopButton`
- `commandWheelStopButton`
- `commandWheelPanel`
- `commandTabGroup`

Visible prefab/object family:

- `CommandButtons`
- `SelectCommand`
- `MoveCommand`
- `AttackCommand`
- `ScanCommand`
- `BuildCommand`
- `HoldCommand`
- `StopCommand`
- `SupportCommand`
- `CommandFocus`

Visual pass notes:

- Command buttons must match the premium full-frame hover/selected/pressed treatment already established during UI Toolkit and Canvas menu work.
- Do not use small colored overlays for state. Use visible chrome-level state sprites and safe scale/translate impact only if it does not overlap neighbors.
- Preserve Button objects and hit rects; `ContainsScreenPoint` checks the Button transform.

## Command Tab Runtime Bindings

Component:

- `MatchOverlayCommandTabGroupView`

Protected serialized fields:

- `tabs`
- `defaultSelectedIndex`

Each `MatchOverlayCommandTabView` protects:

- `button`
- `frameImage`
- `normalFrameSprite`
- `selectedFrameSprite`

Visual pass notes:

- Selected/current state is sprite-driven through `frameImage`.
- All repeated tabs must use one repeated rhythm.

## Runtime Feedback Bindings

Components:

- `BattleHudRuntimeFeedbackView`
- `BattleHudTacticalFeedbackView`

Protected `BattleHudRuntimeFeedbackView` fields:

- `tacticalFeedback`
- `commandTabGroups`
- `feedbackPanel`
- `feedbackText`
- `feedbackIcon`
- `feedbackActionsRoot`
- `boardAllButton`
- `boardAllButtonLabel`
- `cancelButton`
- `cancelButtonLabel`
- `neutralIcon`
- `readyIcon`
- `warningIcon`
- `errorIcon`

Protected `BattleHudTacticalFeedbackView` fields:

- `selectedEntityPanel`
- `commandModeBanner`
- `worldCommandMarkerLayer`
- `invalidCommandToast`
- `minimapCameraBridge`
- `selectedEntityNameText`
- `selectedEntityStatusText`
- `commandModeText`
- `invalidCommandText`

Visible prefab/object family:

- `Feedback`
- `FeedbackPanel`
- `Actions`
- `BoardAllButton`
- `CancelButton`
- `CommandFocus`
- `SelectedEntityPanel`
- `CommandModeBanner`
- `InvalidCommandToast`

Visual pass notes:

- Feedback actions are visibility/interactable driven; keep Button and label bindings.
- Toast/banner panels need enough padding for runtime messages and must not overlap command controls or squad cards.

## Minimap Runtime Bindings

Component:

- `MatchHudMinimapView`

Protected serialized fields:

- `mapImage`
- `mapRect`
- `viewportRect`
- `zoomInButton`
- `zoomOutButton`
- `markerRoot`

Visible prefab/object family:

- `MinimapPanel`
- `Map`
- `Viewport`
- `ZoomIn`
- `ZoomOut`
- `MarkerRoot` or marker child objects such as friendly/hostile markers

Visual pass notes:

- `mapImage` must remain raycast-enabled and preserve the map/viewport interaction area.
- `viewportRect` position and size are runtime-driven. Do not replace it with a static screenshot frame.
- Zoom buttons need full state coverage and safe hit rectangles.

## Squad Tray Runtime Bindings

Component:

- `MatchHudSquadTrayView`

Protected serialized fields:

- `normalFrameSprite`
- `selectedFrameSprite`
- `cards[5]`
- each card `Button`
- each card `FrameImage`
- each card `PortraitImage`
- `disabledFlashSeconds`
- `cardLabelFont`

Visible prefab/object family:

- `SquadTray`
- `SquadCard1`
- `SquadCard2`
- `SquadCard3`
- `SquadCard4`
- `SquadCard5`
- per-card `Frame`
- per-card `Portrait`
- per-card `HealthFrame`
- per-card `HealthFill`
- per-card `NumberBadge`

Visual pass notes:

- Selected state is applied by swapping `FrameImage.sprite` between `normalFrameSprite` and `selectedFrameSprite`; selected must be a full chrome state.
- The five squad cards must stay one repeated family. The highlighted mockup card is the selected-state example, not a unique first-card layout.
- The current code creates a `NameStrip/Label` child at runtime in `Awake`; account for this in captures before judging text spacing.
- Health fills and badges must not overlap the frame chrome.

## Passenger Drawer Runtime Bindings

Components:

- `MatchHudTransportPassengerDrawerView`
- `MatchHudTransportPassengerItemView`

Protected drawer fields:

- `drawerRoot`
- `headerText`
- `emptyStateRoot`
- `emptyStateText`
- `contentRoot`
- `itemTemplate`
- `exitAllButton`
- `exitAllLabel`
- `closeButton`
- `closeLabel`

Protected passenger item fields:

- `portraitImage`
- `nameText`
- `roleText`
- `healthFillImage`
- `healthText`
- `exitButton`

Visual pass notes:

- Passenger rows are pooled from `itemTemplate`; template changes must work for repeated runtime rows.
- `healthFillImage` is runtime-filled and must remain an Image set up for horizontal fill.
- Exit/close buttons need full selectable state coverage when the drawer family is styled.

## Editing Guardrails

- Keep all serialized component references intact.
- Keep runtime-bound GameObject names where existing runtime/editor tools may search or report them.
- Prefer replacing sprites and tuning existing Images over deleting/rebuilding objects.
- If a Button currently uses a transparent or incorrect target graphic, retarget it only to a visible full-frame Image that still covers the same intended hit area.
- Fix sliced sprite import/PPU/Image multiplier before adjusting anchors or shrinking content.
- Finish each family before moving to the next: header/objectives, left selection, minimap/right rail, command buttons, squad tray, passenger drawer.
- Capture focused crops after each visual family pass in the shadow project before marking the related checklist item complete.

## Next Slice

Current next target after this inventory:

- SCN-08 Match HUD unique gameplay header/resources/current-order area.

Known visual work still open:

- HUD resource/objective/header panels need a focused baseline crop and Target Lock comparison.
- Selected-unit/current-order stack still needs a separate live-panel padding pass.
- Command buttons and squad cards need full-frame hover/selected/pressed state proof.
- Minimap/right quick-rail controls need state coverage and panel alignment proof.
