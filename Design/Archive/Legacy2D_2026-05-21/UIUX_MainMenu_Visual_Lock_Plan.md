# WarlineCapture Main Menu Visual Lock Plan

Date: 2026-05-21

Status: Historical implementation plan superseded by `UIUX_MainMenu_Visual_Contract.md`, `UIUX_Mockup_To_Canvas_Conversion_Plan.md`, and the command-base `SCN-02B_MainMenuAlt` target. Keep this file as process history for the original Main Menu visual-lock pass. Do not use embedded Saga/Quick Custom/2D references below as active product direction.

## Goal

Make `Screen_MainMenu.prefab` visually match the original mockup reference before applying the same method to the rest of the UI/UX screens.

Original reference:

- `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-02_main_menu_mode_select.jpg`

Historical landscape visual target:

- `Design/VisualLock/MainMenu/MainMenu_Landscape_Visual_Target.png`

Active command-base target:

- `Design/VisualLockLayered/SCN-02B_MainMenuAlt/reference/MainMenuAlt_CommandTarget_Source_1672x941.png`

Runtime visual-lock background:

- `Assets/Game/Art/UI/Generated/MainMenu/MainMenu_Landscape_Visual_Target.png`

Implementation target:

- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- Existing parallel UI shell remains active.
- Existing legacy `UI_Canvas` remains deactivated but not removed.

## Why Start With Main Menu

The Main Menu is the best first visual-lock target because it defines the app-wide visual language:

- top profile/resource bar
- left navigation
- mode cards
- bottom utility strip
- Synty military HUD frame style
- mode-card illustration style
- typography scale
- icon and button language

Once this is locked, the same UI kit can drive Settings, Skirmish, Campaign, Operations, and popups.

## Phase A - Reference Breakdown

Create a screen-specific visual contract from `SCN-02_main_menu_mode_select.jpg`.

Record:

- reference resolution and aspect ratio
- safe-area margins
- top bar height
- left nav width
- bottom bar height
- card sizes and spacing
- logo/avatar/resource positions
- typography sizes and weights
- dominant colors and opacity values
- panel/frame treatment
- button states
- required icons and card art

Output:

- `Design/UIUX_MainMenu_Visual_Contract.md`

## Phase B - Asset Inventory

Map every visible visual element to one of three sources:

1. Existing project asset
   - use accepted WarlineCapture UI kit sprites, imported Oxanium fonts, existing logo, approved command-base UI art, and current 3D unit/building presentation renders
2. Generated replacement asset
   - use image generation for mode-card artwork, city/military backgrounds, avatar/card illustrations
3. Temporary coded shape
   - use only when the mockup element is structural and can be recreated cleanly with Unity UI

Expected generated assets:

- Campaign card art
- Operations card art
- Skirmish card art
- Commander avatar placeholder matching the mockup style
- optional main menu background plate if Synty assets are insufficient

Generated assets should be stored under:

- `Assets/Game/Art/UI/Generated/MainMenu/`

Prompt and source notes should be stored under:

- `Design/UIUX_MainMenu_Art_Generation_Guide.md`

## Phase C - UI Kit Lock

Before changing the whole screen, create or refine reusable prefabs:

- `HudFrameView`
- `HudTabButtonView`
- `ModeCardView`
- `ResourceCounterView`
- `SideNavButtonView`
- `FooterUtilityButtonView`

Rules:

- Use Oxanium font family.
- Use the same Synty/HUD frame visual treatment everywhere.
- Avoid flat placeholder panels where the mockup uses framed military HUD panels.
- Keep touch targets mobile-safe, at least 80 px high at 1920x1080 reference.

## Phase D - Main Menu Prefab Pass

Update `WarlineCaptureUiPhase1PrefabBuilder` so the generated `Screen_MainMenu.prefab` matches the visual contract.

Corrected implementation slice:

- Do not use the landscape target image as a full-screen runtime background.
- Use the landscape target image as a visual reference only.
- Split the mockup into separate Canvas panels, sliced sprites, icons, card art, real TMP text, and real stateful buttons.
- Preserve current functional behavior while rebuilding the UI as responsive Unity Canvas components.
- Track visual similarity with screenshots, but do not rely on a flat image overlay as the final UI.

Keep existing behavior:

- Settings opens Settings.
- Skirmish can launch the existing QuickCustom-backed game path until the Skirmish setup screen replaces it.
- Campaign and Operations can remain placeholder modal buttons until their production routes are connected.

Change visuals only:

- exact layout zones
- mode-card composition
- resource counter styling
- icon placement
- frame art
- background and card art
- typography scale and alignment

## Phase E - Screenshot Harness

Add a reproducible Unity screenshot method:

- open or instantiate `UIShellAppCanvas.prefab`
- route to `MainMenu`
- render at `1920x1080`
- save screenshot to `Artifacts/UIVisual/MainMenu/current.png`

Reference copy:

- `Artifacts/UIVisual/MainMenu/reference.png`

Diff output:

- `Artifacts/UIVisual/MainMenu/diff.png`
- `Artifacts/UIVisual/MainMenu/report.json`

## Phase F - Pixel Review Gate

Use pixel comparison as a QA gate, but interpret it correctly.

Hard pixel-perfect equality is only realistic if the reference mockup itself is used as a background. Because we are rebuilding the UI in Unity with real assets, the better gate is:

- exact structural alignment for major UI regions
- near-match for colors and typography
- no unexpected gaps, overlaps, clipping, or missing elements
- generated card art accepted visually against the mockup style

Initial thresholds:

- Major layout region bounds: within 8 px at 1920x1080
- Text baseline and alignment: within 6 px
- Button/card size: within 8 px
- Overall pixel difference: tracked but not used alone as pass/fail
- Manual review required for generated art and icons

## Phase G - Tests

Keep existing functional tests and add visual-lock tests that validate:

- Main Menu prefab has the locked hierarchy.
- Main Menu references required generated art assets.
- Main Menu uses Oxanium fonts.
- Main Menu mode-card click targets still work.
- Screenshot harness can render a nonblank 1920x1080 image.
- Visual report JSON is generated.

## Main Menu Acceptance Checklist

- Screen opens from Splash through the Start button.
- Settings button still routes to Settings.
- Skirmish click still starts the existing game flow while runtime internals keep QuickCustom naming.
- The main menu visually matches the active command-base target in layout and visual language.
- All text is readable on Android landscape.
- No text overlaps at 1920x1080 or common Android landscape aspect ratios.
- Generated/Synty art is committed with prompts and source notes.
- Screenshot diff artifacts are available for review.
- Focused Unity EditMode tests pass.
- `git diff --check` is clean.

## After Main Menu

Apply the same method in this order:

1. Settings and Accessibility
2. Skirmish Setup
3. Splash / Loading
4. Tactical HUD
5. Build Drawer and Command Wheel
6. Popups
7. Campaign Map / Briefing / Loadout
8. Operations Dashboard / District Detail
9. Profile and Progression
