# Base Assault implementation evidence

Status: in progress. Internal acceptance has not passed. Android and unfamiliar-player evaluation remain separate gates.

Plan: [prototype plan](../../Roadmap/Skirmish_Prototype/PLAN.md).

## Observed baseline — 2026-09-17

Launched the actual SCN13 setup command in the wrapper-owned isolated Editor at `/private/tmp/warline-readiness-validation`, from baseline `674b406d3`.

- Setup offers five presets, several difficulties, fog, intel and victory choices despite a single generic launch route.
- Configuration store retained only the AI snapshot; seed and non-AI choices were lost. Menu startup did project the AI subset, but did not establish a complete skirmish session.
- Loaded identity `skirmish` / `scenario.skirmish.desert_base_standard`.
- Live faction query after startup: neutral 4,735 health entities (4,734 buildings), player 264 (243 buildings), enemy 6 (all buildings). No active enemy combat units were present at the observation. This is not the planned bounded force.
- Default initial-unit config had no authored factions, 120 player Materials and 655 AI Materials. Map ownership supplied the apparent player force.
- HUD showed an empty tutorial panel and broad unit categories; it did not explain a Base Assault objective or time limit.
- Grid is 2048 × 1024, cell size 1, origin (0,0,0).
- Live navigation/road/blocker sampling found clear 36 × 24 ground footprints at (820,460) and (1180,460). These are candidate Main Base positions; final spawn, full supply footprints and both routes still require gameplay validation.

[English baseline capture](Baseline/en-launch.png).

## Foundation checks

Connected-Editor validation on 2026-09-17: `SkirmishSessionTests` — 9 passed. Checked seed/result persistence without campaign writes, legacy setup migration, duplicate Deploy ownership, and explicit skirmish map identity. Assets generated through `SkirmishPrototypeBuilder.Rebuild()` in the isolated Editor.

## Working balance (not yet accepted)

Both sides: 8 rifle soldiers in two authored batches, 1 light armored car, 2 tray trucks and 1 tanker; main Barracks, fabrication depot, oil pump, refinery, fuel storage. 220 Materials, capacity 600, 160 usable Fuel seed, 0 tactical Credits. Existing fabrication definition consumes 4 Oil for 20 Materials every 30 seconds. Supply throughput, refinery timing, actual roster grouping and sustainable replacement rates remain under validation.

Do not describe the prototype as complete until the acceptance matrix and normal-speed bilingual journeys are recorded.

## ARIA stability correction — 2026-09-17

The campaign Show Me availability updater re-enabled the action after the skirmish read model hid it, changing the measured rail height during successive HUD updates. Skirmish now owns a persistent briefing and does not enter campaign tutorial scheduling or expose its inactive actions. Accessibility and content updates use the same expanded layout choice.

- `SkirmishAriaStability`: passed with exact locale assertions for `en` and `fa-IR`; repeated model, highlight and accessibility updates preserve visibility, text and geometry.
- Live English: 600 Canvas render callbacks produced one state: height 364, briefing visible, Show Me hidden. Live squad selection and screen-position Move accepted and soldiers reached the destination.
- Inspected [English](AriaStability/aria-fixed-en.png) and [Farsi](AriaStability/aria-fixed-fa.png) captures. Farsi expands to fit its text and remains above the minimap. These captures verify the panel; the close base camera and surrounding vegetation still need gameplay review.
- The validation Editor's original two-hour wrapper expired during broader checks; restarted through the same repository wrapper. No user Editor was interrupted.

Additional current evidence: roster mapping regression passed; supply trucks delivered Oil and fabrication increased Materials from 220 to 320 during normal simulation. One earlier normal-speed 15-minute idle run drew at the deadline, but failed the intended AI pressure/pacing review. Real combat defeat, six final bilingual journeys, repeated lifecycle checks and the remaining acceptance matrix are still outstanding.

## Attack route validation

A live player Attack order previously stopped infantry against scenery around x=975. Skirmish now uses the existing interruptible path/breach order to approach a firing position before direct combat; automatic target acquisition does not overwrite that path. The focused firing-position regression passed. The new live English run used squad-card/Attack button hit tests and a screen-position enemy-base click. Soldiers traversed past x=1046, destroyed the enemy base and reached Victory at 147.406 seconds, with 0 player losses, 12 enemy unit losses and 1 enemy building lost. No health/position/resources/result facts were injected. [Result](victory-path-en.png).

This is a functional victory check, **not a balance pass**: 2:27 is below the planned 8–12-minute pacing range. Remaining work includes AI pressure/defeat, authored base sites (avoid road/vegetation overlap), production UI rejection feedback, startup recovery, final bilingual journeys, repeated replay cleanup and campaign regressions. All five setup/navigation tests now have explicit Passed markers.

Campaign guidance regression: `RunShowMeValidation` passed all 10 M1–M5 selection/command/destination/waiting cases after the ARIA changes.
