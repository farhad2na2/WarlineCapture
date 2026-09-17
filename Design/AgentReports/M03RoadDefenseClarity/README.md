# M3 road-defense clarity — 2026-09-16

## Reported problems and changes

- The opening tour used the forward-post anchor beside decorative aircraft and could restore a stale camera snapshot. It now introduces the defenders and convoy approach, returning to the defenders at the command-view zoom. Tutorial phase changes no longer queue unrelated camera moves.
- A second `ReturnWarningCamera` control survived removal of the opening-tour return control. Both are hidden in the prefab, its builders, and runtime presentation.
- Road barriers previously had permission to overlap roads but no requirement to occupy one, and could align with walls. They now snap to a local road center, face across it, and require a road under their center plus the matching rotation. Physical obstructions still reject placement; ordinary buildings still reject road overlap.
- Virtualized sidewalk meshes now bake independent gameplay footprints, so medians remain excluded even when their renderers are culled. The closed gate arm is shown in the placement preview; runtime gates retain their opening behavior.
- The initial gate search assumed the unrotated footprint and could select a distant north-south road. M3 now keeps the authored gate anchor for local preview alignment rather than moving to an unrelated road.
- The warning strip uses a compact localized two-line summary, a 22-point font, 104-unit height and a 72-unit focus button. Full intelligence remains in the warning popup.
- The three retired/optional tool lessons no longer interrupt Hold. Defense follows Hold immediately; optional Scan and production remain available. Internal lesson IDs remain stable, while the displayed progress count is nine.
- A completed optional Scan reports either no vehicles or the contact count, plus an instruction to keep defending. The feedback is shown for eight seconds without changing the squad's Hold order.
- The Farsi Hold recording was locally trimmed at 8.45 seconds to remove the obsolete Stop sentence. The manifest's text, spoken text, duration and hashes were updated. No new generation or upload was used.

## Validation

- English and Farsi real mission journeys reached three-star Victory with no civilian losses, through both convoys and the final debrief. They exercised real UI buttons and defensive combat, rather than forcing a win.
- The journeys verified that Hold remains active after optional Scan, that a localized Scan result is visible, that retired tool lessons are skipped, and that no battle Continue click is required.
- Farsi placement replay passed drag, 1.5-second hold, release, rotation in both directions and confirmation on the actual convoy road. Camera position remained stable. Explicit assertions distinguish the raised median from asphalt and reject a Barracks on the same road plot.
- Inspected captures at 1920×1080, plus result screens at 16:9 and 20:9. Warning text is enlarged, return-camera controls are absent, and the closed gate arm spans the asphalt.
- The Farsi Hold recording was transcribed locally after trimming; the obsolete Stop sentence is absent. See `voice-edit.json` and `hold-voice-transcript.txt`.
- Final focused EN/FA feedback containment checks passed for both zero-contact and vehicle-count results. Corrected an existing text rectangle wider than its background and shortened the English messages. Road placement regressions also passed on the final assembly.

Logs are summarized in `validation-evidence.txt`; selected captures are in `captures/`. Two intermediate Farsi checks failed because the test compared unshaped/shaped text, and a separate pointer replay lost focus. These are not counted as complete passes. Final Farsi mission evidence comes from `m03-clarity-fa6.log`; the successful real placement check comes from `m03-clarity-fa3.log`.

The main Editor's Pipeline import requests timed out while it displayed script compilation, so gameplay validation used the isolated QA project through the required macOS GUI wrapper. Generated HUD and localization assets were copied back to the main project. No Unity processes were terminated. This is Editor validation, not an Android device performance certification.
