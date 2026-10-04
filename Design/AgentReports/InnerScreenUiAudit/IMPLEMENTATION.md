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


## Farsi comic and system Back follow-up — 2026-10-04

Commit `662aa80` was pushed after the user's explicit approval. This follow-up mirrors comic portrait, frame, dialogue, voice-track and Next positions for Farsi while retaining unmirrored portrait artwork. English restores authored positions. Responsive expansion is accounted for once, including when the parent layout has already expanded before dialogue Awake.

A single system Back coordinator consumes Escape (Unity's Android Back mapping), reuses native screen Back route handlers and existing gameplay actions, dismisses popups before screen navigation, opens Pause from Match, and resumes on Back from Pause. Root Home delegates to Android's ordinary background/minimize behavior. Transitions consume Back to prevent accidental double navigation. Archive Back returns to its chooser or closes it; campaign narrative follows its existing skip/confirmation controls. Result/debrief handlers preserve their existing settlement guards. Inbox remains excluded.

All ten reachable menu Back controls are 180 × 86: Campaign, Operations, Commander, Skirmish setup, Briefing, Preparation, Armory, Store, District and Feed. The inner Settings buttons now open the existing popup instead of an empty Settings route. Closing it clears modal state and restores the underlying inner screen without adding navigation history.

### Automated and normal-input evidence

- `warline-ui-back-20261004-01.log.gz`: failed District Back due to a destroyed selected UI object. Fixed with Unity-aware null checks.
- `02` and `03`: failed inner Settings checks; the authored OpenSettings route intent opened an empty route. Corrected to popup dispatch.
- `04`: wrapper exit 0, `[InnerScreenUiInput] result=Passed`, `[SystemBackFixture] result=Passed`, `[ExistingEditorValidation] result=Passed`. Ten routes passed system Back in English and Farsi using Input System keyboard events, with touch/raycast navigation and Settings restoration. Pause opens, Back resumes, and match Settings dismisses in an isolated shell-state fixture. This does not claim a real mission completion.
- Visual review rejected `After/input-20261004-025226` comic captures despite automated pass: Next was off-screen due to duplicated width expansion, and locale/resolution changed before asynchronous screenshots finished. Both defects are corrected; bounds and capture completion checks were added. These rejected captures and full log are retained.
- `05`: focused validation was scheduled before the new entry point was imported and failed with MissingMethodException. Retained as failed evidence.

### Android recommendation and remaining acceptance

Recommended policy: one step per press; dismiss the top popup or cancel confirmation first; use screen history for inner menus; open Pause during a live match and resume from Pause; delegate root Home to Android rather than quitting the process. Confirm only explicit abandonment of an unfinished match. This is our product policy, informed by Android's native Back/predictive navigation conventions.

Official references: [Unity Back mapping](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/input/backbuttonleavesapp), [Android predictive Back design](https://developer.android.com/design/ui/mobile/guides/patterns/predictive-back?hl=en), [Android predictive Back integration](https://developer.android.com/guide/navigation/custom-back/predictive-back-gesture).

No ADB device is connected. Physical Back button, gesture navigation, IME dismissal and Android 13–16 predictive Back commit/cancel remain device acceptance gates. No custom predictive animation is claimed. The previously failed normal-input M01 operation-map/ARIA/result/return gate remains open.

- `06`: focused comic/Pause wrapper passed with nine images in `After/input-20261004-030056`. Panel bounds and reading order passed. Visual review found the gold advance icon flipped around its left pivot and protruding outside the Next panel; navigation-icon mirroring now centers its pivot. The English capture retained the current Farsi story line during live locale switching, so the final harness reopens playback for each language rather than claiming live narrative retranslation. A subsequent focused run verifies archive playback Back and the actual icon bounds.


### Final native review

`07`: final focused wrapper exit 0, `[InnerScreenUiInput] result=Passed`, `[SystemBackFixture] result=Passed` and `[ExistingEditorValidation] result=Passed`. `After/input-20261004-030524` contains nine nonempty images, including properly localized English playback and Farsi playback at 2400×1080, 1920×1080 and 1920×1200. Native review confirms portrait on the right, Next on the left, contained mirrored chevrons, full archive backdrop and clean Home after close. The advance chevron retains its existing blink behavior. Keyboard Back returns playback to the archive chooser in both languages and closes the chooser; touch uses the native archive controls. Pause Back/Resume and match Settings dismissal pass as shell fixtures, with no real mission readiness claim. The ten-route normal-input navigation pass is `04`; only comic/icon geometry changed afterward.

Current implementation and checks are complete for this follow-up. Device button/gesture/predictive Back and complete normal-input mission acceptance remain pending as described above. Pause's existing authored sample/debug contents in the isolated fixture are not accepted as a player-ready mission screen.
