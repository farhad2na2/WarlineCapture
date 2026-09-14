# M05 implementation and acceptance record

Status: **M5 implemented and accepted in Editor in English and Persian, including all 30 approved voice recordings (2026-09-14)**.

The preceding M1–M4/UI changes were committed and pushed as `2c84bdc53` on `codex/m03-radar-warning` before M5 began.

## Implemented scope

- Authored Chapter 1 mission 5, scenario, operation-map anchors and camera tour.
- Two four-person rifle squads and a heavy APC; hostile gate and transmitter; a timed counterattack; archive occupation objective.
- Opening and victory camera sequences, bilingual comic captions, seven final illustrated panels with 16:9 and 20:9 imports.
- Eight guided lessons that restart on entry, retry and replay. Show Me points to the next UI/world action; Do It performs that action. The field-guide popup includes eight lessons and the existing 57-class reference catalog.
- English and Persian strings in the shared localization catalog; language-neutral comic artwork.
- First-clear 750 XP, 4,000 Credits, ghillie unlock and 35 APC parts through atomic campaign settlement; replay 300 Credits without duplicate first-clear grants.
- M4 → M5 campaign progression, result presentation, campaign return, deadline failure and retry.
- Materials/Oil/Fuel gameplay header, mobile command buttons, content-sized ARIA and selection panels, independent minimap above Build.

## Checks and findings

All evidence below is Editor-only, in isolated copies and disposable profiles. No user progress was changed. Screenshots and raw logs live under `/private/tmp`, not Design.

| Check | Result / evidence |
| --- | --- |
| Final voiced production | Passed `m05-voice-build.log`: 30 imported local clips, seven story lines in each language, 16 tutorial events, updated narrative timing. |
| Final voiced journey | Passed `m05-voiced-journey.log`: guided English first clear (1:12 active combat), Persian replay (0:57), localized voice playback, both victories, correct rewards and campaign return. |
| Final voice regressions | Passed ten suites in `m05-voice-regression.log`, including all 16 M5 lesson routes and the narration enablement gate. |
| Final voice architecture | Passed 139 tests in nine fixtures, zero failures, in `m05-voice-architecture.log`. |
| Captioned production build and focused regressions | Passed ten suites in `m05-release-regression.log`: APC displacement, M5 rules/integration, M1–M4 mission rules, settlement, attempt cleanup and M2 resources. |
| Shared camera/transport/UI regressions | Passed fourteen suites in `m05-shared-regressions.log`, including ten M4 integration cases and fifteen boarding scenarios in each language. |
| Architecture | Passed **139 tests across nine fixtures**, zero failures, in `m05-architecture-release.log`. Existing size and architecture baselines were retained. |
| Guided English real combat | Passed objectives and visible victory with only guidance-button actions; 1:16 active mission time. `m05-guided-bilingual-v3.log`. The combined run failed afterward because its replay driver did not reselect M5 after campaign refresh; that driver has been corrected. |
| Persian deadline → Retry | Passed `m05-timeout-retry-v1.log`: actual 12-minute deadline defeat, visible Retry button, fresh target health, zero clock and lesson one. Only simulation time was accelerated; no health, outcome or objective facts were injected. |
| Movement sample | 497 moving infantry samples in the unassisted English run selected RunAim (418), Run (61) or RunShoot (18), with no moving Idle clip. Renderer index differences during crossfades are not treated as failures; the animation package blends to the new clip over 0.5 seconds. This sampling does not replace physical-device performance QA. |
| Guide visual pass | Passed `m05-guide-readability-final.log` at 1920×1080 English and 2400×1080 Persian. Fixed inherited lesson count, M3 availability wording and tutorial cue bleeding over the modal. |
| Bilingual narrative | All seven story panels played and were captured in English 16:9 and Persian 20:9, with current portraits and localized caption/control text. |
| Final bilingual journey | **Passed** `m05-bilingual-release.log`, wrapper exit 0: guided English victory (1:10 active time), visible rewards, campaign return, fresh Persian replay at lesson one, real-command Persian victory (0:57), replay-only 300 Credits, and second campaign return. No runtime health or objective injections and no manual intervention. |

Bugs found and corrected during this implementation:

1. Runtime target IDs collided with friendly authored scenery IDs. Target matching now also requires hostile ownership and the successful placement origin; a regression covers the collision.
2. The tutorial used a cached command mode and could keep requesting Select. M5 now reads the authoritative selection input mode before deciding its next cue/action.
3. Counterattack actors could fight before release. Their combat now respects the opening acknowledgement and scheduled release.
4. Missing targets could leave initialization pending indefinitely. Readiness has a bounded failure path with localized recovery messaging.
5. Leaving the archive during the final hold could leave a wait-only lesson. Guidance now offers a move target again; losing all rifle units falls back to the surviving APC.
6. Result statistics inherited irrelevant civilian-loss labels. M5 reports assault-unit losses and APC survival, and retains M5 details when reward saving needs retry.
7. Persian replay exposed an APC displacement ECB error: path cleanup could remove UnitTarget after the displacement was recorded. The deferred displacement now uses AddComponent's set-or-add behavior. A regression records the actual displacement, removes the target, then checks successful playback and the new path request.
8. The field guide inherited a twelve-lesson caption and M3 availability descriptions. Its count now comes from content, with M5 availability keys; tutorial cues clear while a guide is presenting.
9. The first probe issued move orders while iterating a live ECS buffer. The probe now snapshots that roster before structural changes; this was a test-driver defect.

The automated guided time is a lower bound with immediate actions, not a measured human session. The production plan now distinguishes a 3–6 minute first-play target including story/reading from active combat duration. No human playtest or Android validation is claimed.

## Media provenance and voice integration

Seven panels were generated with the built-in ImageGen tool using the established M3 comic references, keeping Samira's mustard headscarf, Dalia's headset/sunglasses and ARIA's cyan projection consistent. Final assets are in `Assets/Game/Art/Narrative/M05BreachAssault/Final/`. No text is baked into the images; captions remain in localization/narrative data.

On 2026-09-14 the user explicitly approved sending the prepared scripts to api.elevenlabs.io using the existing paid account. All 30 clips were generated: 14 story recordings and 16 ARIA lessons, totaling 350.40 seconds. The manifest records script text, speaker, locale, provenance and SHA-256 hashes. The exact canonical payload passed validation (4,431 characters). All WAVs passed mono/44.1 kHz, non-silence and unclipped-sample checks. The final Editor builder imported every clip with deferred loading and installed story references and all 16 tutorial events. M5 tutorial narration is now enabled; runtime playback uses local assets with no network TTS. The voice-enabled journey passed in `m05-voiced-journey.log`: actual advancing AudioSources were observed for all 14 story recordings and eight tutorial recordings across the two locales (22 distinct clips); no wrong-language playback was detected. Lessons whose objectives were already completed correctly skipped ahead, so the journey does not claim natural playback of all 16 tutorial recordings. The catalog and routing regressions cover all 16. Narrative panel durations accommodate the longer of the English and Persian clips, with a one-second margin. The replay probe allows 180 seconds for loading, the voiced briefing and opening tour.

## Delivery boundary

The voiced build is playable from the campaign after M4 first clear. No voice-generation approval or media integration remains outstanding. No Android or human playtest is claimed. The two preliminary movement-preflight launches failed/timed out after a new test compile error and project contention; the error was fixed, their wrapper-owned processes timed out, and the corrected ten-suite preflight and final real-play run both passed. The user's Editor and Hub were not terminated.
