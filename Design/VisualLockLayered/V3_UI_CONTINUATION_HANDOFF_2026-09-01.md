# V3 UI Continuation Handoff — 2026-09-01

This checkpoint intentionally pauses the V3 UI migration so it can continue on
another computer. The repository contains the current builders, runtime views,
prefabs, shared atlases, tests, target-lock evidence, and iteration notes. No
screen is user-accepted unless its own notes explicitly say so.

## Non-negotiable visual and workflow contract

- Every V3 prefab must be compared with its canonical target lock at runtime.
- Post the target, post the current runtime capture, list visible differences,
  fix them, and recapture before moving to another screen.
- Validate exact `1920x1080` and `4800x2160` output. Wide layouts must use the
  full width without empty side gutters.
- ARIA and other edge-owned controls must remain on their intended edges;
  portraits and background art must crop without stretching.
- Use the shared semantic gradient set and one constant `3 px` border contract.
  Existing `V3GradientGraphic` fills remain a single resolution-independent
  shared renderer; Image-based gradients use the one atlas sprite for their
  semantic color and never create a screen-local PNG.
  Adjacent frames must not overlap or cut through navigation/header/footer
  regions.
- Replace old or placeholder icons with sharp V3 icons. Reuse shared assets and
  atlases; do not add screen-local duplicates.
- Preserve all existing runtime bindings and verify the controls function.
- Work through the open Unity Editor with Unity CLI/Pipeline. On macOS, follow
  `AGENTS.md`: never invoke Unity directly and never add `-batchmode`.

## Shared brand logo — corrected and validated

- Canonical sprite:
  `Assets/Game/Art/UI/V3Shared/Sprites/Brand/ui_v3_brand_logo_mainmenu.png`
- Dedicated one-item atlas:
  `Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_Brand_01.spriteatlas`
- Shared prefab source:
  `Assets/Game/Art/UI/V3Shared/Prefabs/UI_V3_MainMenuLogo.prefab`
- Migration/validation entry point:
  `Game/UI/V3/Validate Shared Brand Logo`
- Last passing gate:
  `[V3SharedBrandLogoMigrationBuilder] result=Passed prefabs=17 references=18 ... canonicalBitmap=1 duplicate=0`
- `V3SharedBrandLogoMigrationBuilder` rejects alternative logo sprites and
  procedural standalone `WARLINE` text.
- The former procedural approximation and older metallic Commander Profile
  logo are not used by any migrated V3 logo root.

## Shared semantic gradients — foundation complete

- Canonical green, red, amber, blue, cyan, and graphite sprites live once under
  `Assets/Game/Art/UI/V3Shared/Sprites/Core/Gradients/`.
- All six are packed once in
  `Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_CoreChrome_01.spriteatlas`.
- `V3UiFoundationBuilder` verifies exact atlas membership and SHA-256 uniqueness.
- Screen-local gradient PNGs are forbidden. Procedural `V3GradientGraphic`
  remains the shared sharp path for dynamic or four-corner gradients and does
  not allocate any duplicate texture.

## First Launch and comic — dual-aspect runtime proof complete

Current review candidate:
`SCN-00_FirstLaunch/iterations/iteration_06/`.

- All four states were captured from the real Menu scene at both required sizes.
- Latest markers:
  - `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=1920x1080 suffix=16x9`
  - `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=4800x2160 suffix=20x9`
- Edit Mode now scales the `1672x941` composition to the active canvas through
  `MainMenuV3SectionLayoutView` using `[ExecuteAlways]` and driven transforms.
- The capture route restores Game view to `1920x1080` and does not close the
  user's Editor.
- ARIA preserves aspect ratio and the outdoor identity state no longer leaks
  comic chrome.
- The Language Choice screen uses the corrected Main Menu V3 logo at both
  aspect ratios. A real Play Mode click selected Persian and Continue advanced
  to Persian dialogue.
- User acceptance is still pending.

## Immediate next task — finish Match HUD wide header anchoring

Target:
`SCN-08_MatchHudV3/reference/SCN-08_MatchHudV3_Final_Target.png`.

Fresh comparison before this checkpoint proved:

- the left selection rail reaches the left edge at `4800x2160`;
- the bottom command rail expands across the wide canvas;
- the header targets did not keep their authored reference X positions, leaving
  ARIA near the left instead of the top-right and leaving the resource/status
  group off-center.

Root cause: after `MainMenuV3SectionLayoutView` became `[ExecuteAlways]`, its
`DrivenRectTransformTracker` was still active while
`MatchHudV3PrefabBuilder.Build()` restyled an already-built prefab. Unity saved
`AriaAssistantButton` and `ResourceStrip` with `x=0`.

The source fix is in
`Assets/Game/Scripts/Editor/MatchHudV3PrefabBuilder.cs`:

- remove existing responsive layout drivers before restyling;
- re-add them only after all reference-space positions are final;
- fail validation unless ARIA is authored at `x=1318` and ResourceStrip at
  `x=414`.

The updated `Game.Editor.dll` compiled successfully in the source Editor, but
the prefab was not rebuilt with the new assembly before this pause. The last
two images in `/private/tmp/warline-match-hud-v3-*.png` are therefore rejected
evidence and must not be frozen as a new iteration.

On the next computer:

1. Open the project and wait for script import/compilation.
2. Through the connected Editor, run
   `Game/UI/V3/Capture Match HUD V3 Review` (it rebuilds first).
3. Confirm `SCN08_MatchHudContent.prefab` serializes ARIA `x=1318` and
   ResourceStrip `x=414`; the new validation enforces this.
4. Post the target and both new captures. Confirm ARIA is in the top-right,
   the resource/status group is centered, the left rail touches the left edge,
   the command rail fills the width, and no element overlaps.
5. Audit every visible Match HUD icon against the target. Replace any remaining
   legacy/placeholder symbols with sharp shared V3 sources, then recapture.

## Review-frozen/candidate screens with existing dual-aspect evidence

These are implemented candidates, not blanket product acceptance. Read each
latest `iterations/.../notes.md` before editing:

- Splash/Loading, Main Menu, Commander Profile, Campaign Operations, Mission
  Briefing, Loadout/Squad Prep, Operations Dashboard, District Detail.
- Match Full Map, Unit Command Wheel, Build Placement Confirmation, Build
  Drawer, Tutorial Presentation, ARIA Command Assistant, Assistant Takeover.
- Threat Alert, Confirm Raid, Build Placement popup, Reward Unlock, Mission
  Result, End of Day Report, Settings, Intel Reveal.

The Settings history includes rejected early iterations. Only compare from the
latest target and latest candidate: tabs must not touch or be crossed by the
frame, gradients must be visible, and header/middle/footer borders must all be
the same thickness.

## Staged or still pending full live acceptance

Continue in this priority order after Match HUD:

1. Match-related remaining states and icon-quality pass.
2. End-match screens.
3. Pause Options (`POP-07_PauseOptions/WORK_IN_PROGRESS.md`).
4. Ability Upgrade Detail (`POP-09_AbilityUpgradeDetail/WORK_IN_PROGRESS.md`).
5. Skirmish Setup (`SCN-13_SkirmishSetup/WORK_IN_PROGRESS.md`).
6. Store/Command Exchange (`SCN-14_StoreCommandExchange/WORK_IN_PROGRESS.md`).
7. Inbox (`SCN-15_Inbox/WORK_IN_PROGRESS.md`).
8. Events (builder/prefab exist; create a fresh target comparison and WIP note
   if dual-aspect evidence is not already complete).
9. Ranking (`SCN-17_Ranking/WORK_IN_PROGRESS.md`).
10. Armory (`SCN-19_Armory/WORK_IN_PROGRESS.md`).

For every staged screen, ignore stale statements about a locked macOS session;
they describe the conditions at the time those notes were written. Re-evaluate
the current computer and use a connected Editor when available.

## Shared-art guardrails

- Shared registry: `V3_SHARED_ASSET_REGISTRY.csv`.
- Atlas/art strategy: `V3_SHARED_LAYERED_ART_ATLAS_STRATEGY.md`.
- Migration runbook: `V3_PREFAB_MIGRATION_RUNBOOK.md`.
- Calibration/request prompts: `V3_CALIBRATION_ASSET_PROMPTS.md`.
- Main menu commander art follows
  `SCN-02_MainMenuV3/SCN-02_COMMANDER_VARIANT_CONTRACT.md`: use a separate baked
  commander/background composition per commander so characters are not visibly
  cut out or stretched. Do not duplicate the shared WARLINE logo.

## Checkpoint verification performed

- `Game.Editor.dll` is newer than the final Match builder source change, proving
  the checkpoint source compiled in Unity.
- The First Launch dual-aspect runtime markers and the all-screen shared-logo
  validation passed before this pause.
- Pending local Pipeline jobs created during the final Match investigation were
  canceled; the user's Editor was not terminated or restarted.
- The checkpoint commit is intended to include the entire working tree so the
  repository can transfer cleanly to the next computer.

## First Launch V3 skip confirmation — completed 2026-09-01

The legacy skip confirmation has been replaced in
`FirstLaunchNarrativeSequence.prefab` with a sharp V3 modal. It uses procedural
shared chrome only: a constant 3 px frame, cyan/blue and red/orange gradients,
and authored warning/pause/skip symbols. No new raster or duplicate UI asset was
added.

English and Farsi bindings are present for the title, body, keep-watching, and
skip-intro actions. Farsi uses the shared Arabic font, RTL shaping, and right
alignment. Runtime interaction was checked in both languages; Keep Watching
closes the modal and Skip Intro reaches Match. The focused integration suite
passes 10 tests.

Accepted dual-aspect comparisons and notes are in
`SCN-00_FirstLaunch/iterations/iteration_07/`.
