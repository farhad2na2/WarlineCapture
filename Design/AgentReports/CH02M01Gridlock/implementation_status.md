# Gridlock implementation evidence

Source baseline: `950297fcc`. Work in progress. **Playable voiced checkpoint with revised Fadi comic art; final production acceptance remains incomplete.**

The implementation uses the authored route-clearing variant. Shared city surface assets are unchanged. The candidate corridor runs from (680, 426) to (814, 426); it passed a five-cell-wide wheeled-surface survey. Live city blocker sampling was exploratory, not a traversal result. Proposed actor configs are canonical civilian Male 01 (Fadi), civilian Male 02 / Female 01 (workers), and the canopy truck (3×3 cells, speed 8, 220 HP). Crew health is the canonical 55 HP. Crew combat is disabled only for this mission.

## Verified checkpoints

- `/private/tmp/warline-gridlock/rules-01.log`: wrapper exit 0 and `[GridlockRules] result=Passed cases=6`. This was an earlier rules-only source snapshot, not final integration acceptance.
- `/private/tmp/warline-gridlock/config-02.log`: wrapper exit 0, `[GridlockConfig] result=Passed chapter=2 mission=1 crew=3 rifles=8 relief=1 hostiles=10`, and catalog refresh reports six missions / six maps. Assets were generated in an isolated validation copy.

## Failed attempts retained

- `rules-02.log`: C# buffer assignment error, then wrapper timeout (124). Source assignment corrected afterward.
- `config-01.log`: attempted while the compile-failed validation Editor still owned the copy. No required pass marker; failed validation regardless of exit code.
- `captioned-01.log`: traversal probe had a missing namespace import and missing move-queue argument. Both source errors corrected afterward; rerun pending.

## Remaining acceptance

A01–A12 are all pending. Scripted blocker-clearing/traversal diagnostics passed as recorded below. No manual-input win, final accepted Watch matrix, final art/voice review, device result, or unfamiliar-player review is claimed. One developmental Watch win is recorded below.

`CH02M01GridlockTraversalProbe` is explicitly a **scripted mechanics diagnostic**. It uses an isolated profile, direct ordinary order queues and scripted story skip. It is not ARIA, manual-input, fairness, or pacing acceptance. It must never be cited as one of the six mandatory Watch wins.

The earlier captioned scaffold has been replaced by the chapter-opening and 12-panel content checkpoint below. Final voice and complete gameplay acceptance remain pending. The supplied vehicle fuel policy is mission scoped.

## 2026-09-21 runtime and normal-selection checkpoint

- Captioned build `captioned-03.log` passed with the explicit production marker; art, voice and acceptance remain pending.
- Traversal probes 01–03 failed and are retained under `/private/tmp/warline-gridlock`. They exposed a Burst query declaration and normal-building rejection of authored road obstructions. The roster is now allowed its bounded prefab initialization period; missing initialized identities still fail closed.
- Added a registered mission-road-obstruction request kind under the existing building owner. It permits road occupancy only for the current catalog definition and registered site request, with surface/collision/owner checks. It does not relax player construction placement.
- `traversal-04.log` exited 0 and recorded `[GridlockTraversal] result=Passed actualTravel=133.6 scope=scripted-mechanics-only aria=NotRun manual=NotRun`. Both owning blockers were removed, the relief vehicle actually navigated 133.6 world units, and the hospital hold completed. This diagnostic used direct ordinary command requests and is **not A04/A05/A07 evidence**. Shared map defenses interfered with that run; Gridlock was subsequently added to the existing dormant-map-defense policy and combat must be rerun.
- User correction: tutorials and Watch must use actual gameplay controls, including world taps and drag selection; no ARIA-only “Select mission group” shortcut. Removed that shortcut from the shared guidance path, added normal Select → tap/drag cues and visible drag endpoints for the existing touch driver. No direct group selection is issued by this path.
- `normal-selection-01.log` passed 11 shared selection/command/wait checks. The drag-box test failed in `normal-selection-02.log` and `normal-selection-03.log` because its camera pixel rectangle did not match the test Game View. The fixture now explicitly matches the screen, without relaxing production visibility checks.
- `normal-selection-04.log` exited 0 and passed all 12 shared guidance checks, 15 ARIA decision checks, and the captioned build checkpoint. Drag endpoints propagate through the existing touch actuator. These are focused checks, not live-input or mission acceptance.
- `traversal-05.log` exited 0 and passed actual 133.6-unit traversal after the dormant-map-defense correction. Enemies remained alive during the opening; the diagnostic still used direct order requests and is not manual/ARIA acceptance.
- `watch-01.log` failed to compile the new developmental probe because it omitted the ECS observation namespace; its wrapper timed out with exit 124. The import was corrected. This is a probe failure, not a successful Watch run. `CH02M01GridlockWatchProbe` scripts preparation, then uses touch on the visible Watch/Start controls and observes without intervention after Start; it is explicitly not public-entry acceptance evidence.
- `watch-02.log` reached real Watch confirmation and normal drag selection, then stopped before completion (exit 1; original probe did not record stop reason).
- `watch-03.log` reproduced a HUD-occluded drag box, then bounded handback while focused (exit 1). Shared guide visibility now checks actual UI raycasts at the drag start, end and midpoint; a covered group retains Show Me.
- `watch-04.log` and `watch-05.log` failed the new edit-mode fixture before gameplay. The fixture needed explicit event-system/raycaster lifecycle initialization, since no player loop runs in that execute-method test. The assertions were retained.
- `watch-06.log` passed 12 guidance and 15 decision cases. Watch then used actual gestures to move rifles/Fadi/workers, completed site A, and handed back at step 5 when a spread group still exceeded the unobscured viewport. Its failure capture is `/private/tmp/warline-gridlock/developmental-watch-last.png` (overwritten by later diagnostics). Group camera framing now reserves space for both HUD rails and the command bar. No win is claimed.
- Added a finite reserve staging correction: registered counterattack entities and linked presentation leave active play only after initialization, and enter after the public warning delay without replacing/healing them. Previously their movement was held but rifles could kill them before warning. The counterattack destination now actually intersects site A's contest radius. `watch-07.log` passed the release/linked-presentation test, and its live log retains six defeated screens until the delayed reserve enters.
- `watch-07.log` exited 0: 12 guidance checks, 15 decision checks, captioned rebuild, reserve test, then `[GridlockWatch] result=Passed actualTravel=133.5 scope=developmental-watch publicEntry=NotRun manual=NotRun`. Watch started through actual touch on Watch/Start, then completed both work sites and delivery at normal speed without intervention. Seven rifles, Fadi and both workers survived; 42 gesture actions were recorded before the final hold. **This is one developmental win, not any of the six final A05 cases:** public-entry, final content, locale/layout/recovery coverage and result settlement review are still outstanding.
- Added a manual-touch feasibility mode to the isolated probe. It accepts individual screen gestures through the same touch actuator, captures the visible Game View, and never issues entity selections or gameplay orders. Scripted profile/deployment/briefing preparation keeps it distinct from public-entry acceptance.
- Authored map hashing now includes the serialized generated map, so geometry changes cannot retain an unchanged hardcoded content hash.
- The source and generated validation assets are not yet a certified final build. Final art/voice, chapter opening, complete public-entry/manual/ARIA journeys, persistence/retry coverage, affected regressions and device/player review remain open.

- `manual-01.log` failed after foreground loss and a native-window tool timeout. `manual-02.log` failed its wall-clock timeout with site A work underway; neither is a manual win. The second run visibly selected eight rifles by a normal drag (`/private/tmp/warline-gridlock/manual-eight-selected.png`), selected all three civilians with another drag, moved them with normal Move/world taps, and began work after positioning Fadi individually. Normal formation offsets can leave him outside the five-unit work radius.
- `crew-selection-03.log` exited 0 with `[GridlockCrewSelection] result=Passed`, all 12 shared guidance checks, all 15 ARIA decision checks and the captioned build marker. Added mission identity labels for Fadi/road workers and crew group summaries; civilian groups no longer claim to be infantry soldiers. The preceding two wrapper attempts exited 65 because sandbox process visibility prevented the Hub check; the same checked wrapper succeeded outside that sandbox.

- `manual-03.log` exited 1 on the fixture wall-clock timeout. Normal gestures selected eight rifles, Fadi, two workers, and then a mixed eleven-unit group; site A reached 25/25 and its owning obstruction visibly disappeared (`/private/tmp/warline-gridlock/manual-site-a-cleared.png`). The group traversed the cleared section to site B. All eleven friendlies survived and the warned reserve was defeated. Site B and delivery were not completed before timeout, so this is partial manual feasibility evidence, not G0 closure, A04, or a manual win.

## Chapter and presentation checkpoint — 2026-09-21

- Added five-panel `seq.ch02.open.broken_grid`, played before the three-panel mission brief. Seen state persists through the existing campaign progress store; retries do not consume first-clear rewards again. A canonical Fadi speaker and a dialogue portrait derived from `Chr_Civilian_Male_01` replace the generic radio identity.
- Generated twelve distinct compositions, with 16:9/20:9 Unity sprite crops, using the built-in image-generation tool. Source/prompt provenance and rejected revisions are retained alongside this report. Captions remain outside the bitmap. The D02 map was revised after the real dialogue overlay obscured its clue; Fadi's thumbnail portrait was improved without changing his civilian appearance.
- `opening-01.log`: wrapper exit 0; opening persistence/no-reward check, crew selection, twelve shared Show Me checks, fifteen then-current ARIA cases, four-sequence/twelve-line captioned build passed.
- `watch-opening-01.log`: failed, wrapper exit 1. Opening handed off to brief, and Watch progressed through site A, then stopped at active clock 134 seconds when application focus was lost (`StopReason=2`). This remains a failed developmental run and is not an A05 case.
- `art-import-01.log`: wrapper exit 0, twelve sources/twenty-four crop sprites imported, and two successive imports retained all twenty-four sprite identities.
- `comic-review-01.log`: the capture harness completed but visual review rejected its locale/transport setup (English body under Persian UI; stale prefab transport labels). It is not localization acceptance.
- `comic-review-02.log`: wrapper exit 0, all twenty-four panel/locale captures completed using the shipping locale resolver and transport helper. PNGs in `/private/tmp/warline-gridlock/comic-review-02/` are 1920×1080 EN and 2400×1080 FA. Representative UI review verified connected Persian, localized speaker identity, readable transport and English captions. D02 and the Fadi portrait were revised afterward, so their final captures must be checked separately.
- Added a nonblocking route-open radio report, scoped to the current session/attempt/source and verified connected-route fact. Unsafe mission-state diagnostics remain visible even when guidance is cleared.
- ARIA now bounds repeated target/goal cycles; changing Select/Move controls cannot indefinitely reset its objective-progress timeout. Final gameplay regression is still required.
- `archive-01.log`: wrapper exit 0; nine Gridlock rule checks, twelve M1–M5/Gridlock Show Me checks, seventeen ARIA decision checks, content rebuild and stable crops passed. The actual Campaign Story Archive button replayed the chapter opening and returned without changing the mission session, phase, profile, progression or rewards.
- The exact 22 bilingual script pairs (44 clips, 5,277 characters) are prepared in `voice_payload_review.json`. The existing paid provider account was checked read-only. Automatic approval review rejected sending this internal text to ElevenLabs without explicit destination/payload authorization. No Gridlock voice generation ran; an explicit approval question is pending. Local importer/playback preparation does not constitute voice acceptance.

Remaining: human voice listening review; guidance-disabled observation/Watch flow; six final uninterrupted A05 cases including public menu entry, alternate layouts/locales and recovery; full manual A04 journey; retry/settlement and affected mission regressions; device and unfamiliar-player release checks. Do not mark A01–A12 complete from the focused checkpoints above.

## Installed presentation checkpoint — 2026-09-21

- `comic-revisions-01.log`: wrapper exit 0. All four revised Fadi brief / D02 debrief captures were visually inspected in English 16:9 and Persian 20:9. Fadi remains recognizably civilian; the wall-map clue stays visible above the caption panel. Captures: `/private/tmp/warline-gridlock/comic-review-03/`. This is presentation review, not full art/mission acceptance.
- `final-checkpoint-01.log`: wrapper exit 0, opening persistence and crew identity checks, twelve shared selection-guidance checks, seventeen ARIA decision checks, captioned content generation, and twenty-four stable sprite identifiers passed. This run includes the latest visited-goal watchdog implementation.
- Installed the validated Unity-authored narrative, portrait, campaign/briefing previews, localization and Addressables references into the working project. Seventy-three installed files are recorded with SHA-256 hashes in `installed_asset_manifest.json`; overwritten presentation assets were backed up under `/private/tmp/warline-gridlock/pre-final-art-install/`.
- Main and isolated validation C# sources match. Source `git diff --check` passed for `Assets/Game/Scripts`, `Assets/Tests`, and `Tools/Audio`. The already-running main Editor was not stopped or restarted.
- No final A05 win or full mission acceptance is claimed. The pending voice approval and remaining acceptance matrix above are unchanged.

## Fadi comic revision and approved voice generation

- User explicitly approved sending the prepared dialogue to ElevenLabs. Generated all 44 clips (24 story, 20 tutorial) using the existing subscription; the prior egress-approval blocker is resolved. `gridlock_voice_manifest.json` records provider, voices, text hashes, file identities and duration.
- Replaced Fadi's game-mesh appearance in CH02-O04, B02, B03 and C01 with a consistent illustrated comic treatment. Retained the portrait, civilian clothing, scene composition and other characters. Exact edit prompts and source outputs are recorded in `fadi_comic_style_revision.json`.
- `voice_file_validation.json`: 44 non-silent mono 44.1-kHz PCM files and 22 unique tutorial/radio event references passed file validation. This does not assert a human listening review.
- Connected the route-open report to the locale-specific recorded ARIA event through the existing narration arbiter. Runtime generation/network TTS remains disabled.
- `voiced-comic-01.log`: repository wrapper exit 0; all 24 localized story clips played to completion through the shipping player, without restarts or locale mismatches. All 24 EN 16:9 / FA 20:9 captures completed. The eight revised Fadi panel/locale captures were visually reviewed.
- Imported and installed all 44 clips, their Unity metadata, the rebuilt audio event catalog, and EN/FA story bindings. All 44 installed GUID references and approved caption hashes were checked. Source diff whitespace checks passed. These changes do not close the remaining gameplay acceptance matrix or assert a human listening review.
