# Campaign inner UI implementation — 2026-10-04

Workspace: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`, branch `codex/campaign-inner-ui`.

## Implementation

The approved Briefing v02 and Preparation v01 layouts are implemented as native Unity prefabs. Both retain the selected mission and existing deployment handlers. Briefing shows the actual mission artwork, chapter, purpose and scrolling objectives, conditions, intel, rewards and star goals. Preparation derives its mission title, summary, objectives, forces, conditions and goals from the selected mission; the Armory button browses the real catalog. Initial mission goals use their actual four/five-minute criteria rather than generic legacy examples.

District Detail, Store, Armory and Command Feed reuse the shared header, margins, typography and RTL layout. Unbound district metrics and fabricated command-feed events, counts and timestamps are hidden. Saved commander facts replace sample feed events. Store purchases remain unavailable, with player-facing availability copy. Armory retains real catalog selection and statistics; unbound example levels, progress and upgrade controls are hidden. Inbox is excluded.

The Persian loading screen now uses explicit localization bindings, the Persian font and shaping, normal horizontal scale and enough room for status and tips. `Starting` is translated. Store runtime updates also use the shared localization binding, fixing Persian square glyphs after locale changes and offer selection.

## Visual-direction review

User approved the actual-reference ImageGen Briefing v02 and Preparation v01 direction before implementation. Approval remains valid. Native captures are separate evidence.

## Native visual review and automated checks

- Initial 21-frame before audit: `Before/20261003-204421`.
- First candidate after capture: `After/20261003-212849`. Builder checks passed, but visual review failed: overlapping legacy briefing panels, missing Persian Deploy caption and clipped Store/Armory text. Evidence retained.
- Revised candidate: `After/20261003-214353`. Legacy overlaps and Deploy caption fixed. Visual review found residual Command Feed samples and cramped Store categories. Evidence retained.
- Full revised matrix: `After/20261003-215101`, 42 route frames plus six loading fixture frames across English/Persian at 1920×1080, 2400×1080 and 2048×1536. All six builders and the checked capture passed. Persian Store square glyphs were subsequently found in visual review and corrected through its runtime localization path.
- Focused Store/Feed capture `After/20261003-215650` produced all 12 route images plus six loading fixture images, but the automated capture failed because the focused-run expected count incorrectly used the full seven-screen matrix. This failed evidence is retained; the count now uses the requested screen set.

All Unity execution uses `Tools/CI/invoke_unity_macos.sh --reuse` with explicit logs and timeouts, while Unity Hub and the existing Editor remain open. Compilation and route-jump screenshots do not establish player readiness. Loading screenshots in the capture matrix instantiate the real prefab as a fixture; they do not prove a real deployment loading transition.

- Final focused Store/Feed/loading matrix: `After/20261003-220855`, 18 images, checked capture and both builders passed. Persian Store glyphs and disabled-purchase styling were reviewed.
- Campaign Chapter I/II button references are restored in the native prefab and authoring path. The temporary Preparation authoring root was removed from the Menu scene through Unity; cleanup validation passed.
- Final C# compilation check: compiling false, updating false, scriptCompilationFailed false. Focused source whitespace check passed; native Unity YAML retains its standard serialization whitespace.

## Normal-input navigation

Checked navigation passed with 21 nonempty screenshots in `After/input-20261003-223606`, wrapper exit 0, `[InnerScreenUiInput] result=Passed` and `[ExistingEditorValidation] result=Passed`. This run includes Chapter I/II/III selection, native Campaign narrative Skip, scroll-to-bottom assertions and Persian Archive controls. An earlier run also passed in `After/input-20261003-220320`. Visual review then found the Persian caption button still crowded its neighbor; an explicit two-line label correction passed the final checked run in `After/input-20261003-224020` (21 nonempty PNGs, wrapper exit 0 and both pass markers). Its Persian playback screenshot was visually reviewed: the caption now fits within its button. Navigation uses an Input System touch device and raycast-checked native controls; no `onClick.Invoke` or injected outcomes.

## Complete normal-input mission

Failed: the normal-input Persian M01 run reached Briefing, Preparation, Deploy and the mission HUD, and dismissed narrative through native touch controls, but ARIA remained disabled. The run timed out without a result or return. Runtime errors reported missing packed render readiness/metrics/state ownership and a virtualized building count mismatch. No injected outcome, troop order or old-candidate win was used. ARIA/result/return, Pause and mission field-guide acceptance remain pending.

Failure evidence: `After/input-20261003-222047` and `Validation/warline-inner-ui-mission-20261004-02.log.gz`. The first attempt also failed on absent Chapter I button bindings; those bindings were repaired and the second attempt reached the HUD. The rendering failure is not established as caused by these UI changes. Existing unrelated Skirmish edits remain preserved.

## Player/device acceptance

Physical-device safe area, touch size and human player acceptance remain pending. Editor captures are not physical-device acceptance.

## Retained failed checks

`input-03` was rejected while the Editor was still importing/compiling. `input-04` failed because the chapter-selection journey had not dismissed the real narrative overlay; `input-05` passed after using the native Skip control. These failures remain in Validation alongside all builder, capture and mission failures. Compilation readiness is checked before subsequent runs.

Settings was captured and inspected through native open/close. Story Archive and playback were reached through touch after fixture progression unlocked archive entries; this fixture is not evidence of completing those missions. Results, Pause and mission field-guide visual acceptance remain pending with the failed full-mission gate. No physical-device acceptance is claimed.

## Final state

This batch is prepared for commit and push on `codex/campaign-inner-ui` in the T7 workspace. The previous five-menu change is already merged; this request does not merge this new batch. Existing unrelated Skirmish work is preserved. Latest loading visual: `After/20261003-220855/loading-fa-IR-2400x1080.png`. Latest touch evidence: `After/input-20261003-224020`. Mission readiness and physical-device acceptance remain open as described above.

## Story Archive background regression — 2026-10-04

The user identified comic dialogue displayed over the main menu in the playback capture. Archive playback used the shared narrative layer whose art-less dialogue states intentionally exposed the scene beneath; for archive replay that scene was the live main menu. Archive playback now enables an opaque backdrop inside the narrative layer and disables it on close/return. Campaign/mission narrative background behavior is unchanged.

Checked native touch regression passed, wrapper exit 0 plus both pass markers: `After/input-20261004-021004` contains 22 nonempty PNGs. Visual review confirms the menu is covered during playback and `home-after-story-fa-IR.png` shows the menu without dialogue after closing. The test also asserts the active opaque backdrop during playback and narrative visibility false after close. The earlier operation-map mission failure remains pending.
