# WarlineCapture UI/UX Phase 3 Immediate Implementation Plan

Date: 2026-05-02

Status: First implementation slice completed.

## Goal

Phase 3 upgrades the parallel `Screen_MainMenu` into the first real `SCN-02 Main Menu / Mode Select` surface using the Phase 2 shared components.

This phase still does not replace the legacy menu by default. The current `UI_Canvas / Panel_Main` flow and `Button_Game` fallback must remain available until Quick Custom launch is implemented and validated in the next phase.

## Target Screen

Upgrade:

```text
Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab
```

Target hierarchy:

```text
Screen_MainMenu
  TopProfileBar
    LogoImage
    CommanderAvatar
    CommanderNameText
    LevelText
    ResourceCounterList
      Resource_Money
      Resource_Trust
      Resource_Intel
    SettingsButton
  LeftNav
    ProfileButton
    InboxButton
    StoreButton
    EventsButton
    RankingButton
  ModeCardList
    ModeCard_Saga
    ModeCard_Operation
    ModeCard_QuickCustom
  BottomUtilityBar
    ChatButton
    SocialButton
    CommanderButton
```

## Behavior

- `SettingsButton` routes to `Settings`.
- `ModeCard_QuickCustom` routes to `QuickCustomSetup`.
- `ModeCard_Saga` opens a placeholder popup until `SagaMap` exists.
- `ModeCard_Operation` opens a placeholder popup until `OperationDashboard` exists.
- `ProfileButton`, `InboxButton`, `StoreButton`, `EventsButton`, `RankingButton`, `ChatButton`, `SocialButton`, and `CommanderButton` open placeholder popups.
- Do not make the parallel shell default yet.

## Data

Use constants for the first slice:

- Commander name: `COMMANDER MANDEL`
- Level text: `LVL 01`
- Money: `10,000`
- Trust: `72`
- Intel: `18`

Later phases should replace these constants with:

- `PlayerProfileState`
- `ResourceWalletState`
- `ModeUnlockState`

## Immediate Work Order

1. Completed: update the builder to generate the full Phase 3 main-menu hierarchy.
2. Completed: reuse Phase 2 component-style construction for resource counters and mode cards.
3. Completed: add placeholder modal buttons for non-implemented navigation.
4. Completed: keep Quick Custom as the only real mode route.
5. Completed: add `WarlineCaptureUiMainMenuTests`.
6. Completed: regenerate prefabs in `WarlineCapture-CodexUnity`.
7. Completed: run focused main-menu tests.
8. Completed: run existing shell and component tests.
9. Completed: run `git diff --check`.
10. Defer Android build unless script or route changes require it.

## Completion Definition

Phase 3 is complete when:

- Main menu prefab has the full target hierarchy.
- Logo uses `Assets/Game/Textures/Logo.png`.
- Settings routes to `Settings`.
- Quick Custom routes to `QuickCustomSetup`.
- Placeholder buttons are wired for non-implemented surfaces.
- Text uses Oxanium-family TMP fonts.
- Focused main-menu tests pass.
- Existing shell/component tests pass.
- `WarlineCaptureUIBootstrap` remains defaulted to `UseLegacyMenu`.
- `git diff --check` is clean.

Validation on 2026-05-02:

- `WarlineCaptureUiMainMenuTests`: 4 passed, 0 failed.
- `WarlineCaptureUiShellTests`: 11 passed, 0 failed.
- `WarlineCaptureUiComponentPrefabTests`: 6 passed, 0 failed.
- `git diff --check`: clean.

## Next Phase

Phase 4 should implement `Screen_QuickCustomSetup` as the first real setup flow:

- Enemy count.
- Difficulty.
- Starting resources.
- Economy/build/production speed controls.
- AI aggression/expansion/target priority.
- Launch Mission action that starts the existing gameplay path with a payload.
