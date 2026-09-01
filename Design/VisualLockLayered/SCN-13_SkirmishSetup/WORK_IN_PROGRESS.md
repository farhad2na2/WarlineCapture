# SCN-13 Skirmish Setup V3 Work In Progress

Status: canonical target audited and V3 source rebuild staged. Unity prefab
generation, focused validation, and exact-size Play Mode comparisons are pending
because the macOS login session is locked and no Unity Pipeline Editor is
connected.

## Canonical Target Lock

`reference/SCN-13_SkirmishSetupV3_Target.png`

No current runtime capture is presented as an iteration. The existing prefab is
the rejected legacy 4800-reference implementation: it has only four preset
cards, ornate raster chrome, flat fills, oversized spacing, and no valid
1920x1080 or 4800x2160 Play Mode evidence against the current target.

## Staged V3 Source

- `Assets/Game/Scripts/Editor/SkirmishSetupPrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/SkirmishSetupV3SegmentVisual.cs`
- `Assets/Game/Scripts/UI/Screens/SkirmishSetupV3CycleControl.cs`
- `Assets/Tests/Editor/SkirmishSetupScreenTests.cs`
- exact 1920x1080 and 4800x2160 capture entry points in
  `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`

The staged composition follows the 1672x941 lock: five operation preset cards,
an aspect-fill Sahrin preview, objective and intelligence metrics, opposing-force
controls, match-rule controls, and the three-button footer. All large surfaces
use directional procedural gradients and independent 3 px borders. The central
title, operation panel, and Randomize Seed action expand on 20:9 while the right
header chips, rules column, and Launch Mission action remain pinned to the right
edge, avoiding empty side gutters.

No new operation/unit raster art was generated. The screen reuses the existing
Sahrin preview, existing menu plates and unit portraits, and shared V3 icon
sources. Lock, check, target, dice, and chevron marks are procedural so they do
not create duplicate atlas entries. Map and preset art use masked
`AspectRatioFitter.EnvelopeParent` crops and cannot stretch.

The visible table rows remain functional: Starting Materials, Income,
Aggression, and Win Condition cycle their existing runtime-backed controls;
enemy count, difficulty, Intel Reveal, Fog of War, seed, reset, randomize, and
launch preserve the existing `QuickCustomScreenView` contracts.

Offline Roslyn audits pass for the updated UI contracts, full UI runtime,
isolated V3 builder, and focused SCN-13 test source. This is compile evidence,
not a substitute for Unity generation or live visual validation.

## Pending Commands

Run only through `Tools/CI/invoke_unity_macos.sh` after the login session is
unlocked:

```text
-quit -executeMethod Game.Editor.SkirmishSetupPrefabBuilder.Build
-quit -executeMethod SkirmishSetupScreenTests.RunFocusedValidation
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureSkirmishSetup1920x1080
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureSkirmishSetup4800x2160
```

Do not create an immutable iteration until both exact live captures have been
posted, compared against the target, and corrected for any visible mismatch.
