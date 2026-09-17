# Findings and fixes

Opened 2026-09-16. Status: reproduced internal findings closed with scoped evidence; candidate build and artifact checks passed; device/player gates remain open. Scope is bounded by [PLAN.md](PLAN.md).

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-01 | P1 | Some legacy “guided” replays drive Show Me / Do It instead of the current player action; hidden Do It is no longer valid acceptance | Current readiness replays repaired; legacy probes excluded from acceptance | CampaignTutorialEntryEditorProbe.Journey.cs now uses visible, hit-tested controls and screen-position world commands. Hidden Do It is asserted absent; old guided probes remain lower-level historical evidence. |
| QA-02 | P1 | Existing green placement/next-step tests did not compare the completed building's pose | Fixed in prior placement work; preserve regression | M03PlacementCommit validates registered cell, root/model pose and foundation after Confirm |
| QA-03 | P0 | M1 opening instructs “Select mission group” but its dynamically created button is invisible/unusable after the camera introduction | Fixed; all ten opening checks passed | Readiness opening replay, completed isolated profile with assistant Off. Selection inherited Do It's locked CanvasGroup and transient disabled visual state. Prepare Continue and selection actions before the intro lock snapshots controls. EN/FA M1 full routes also completed. |
| QA-04 | P0 | Re-entering M1 after M4 leaves group selection unable to progress, despite a usable button | Fixed; focused routing and all ten opening checks passed | The persistent mission root retains an extraction-state component. Selection dispatched by component presence rather than current mission identity. Gate transport/specialist routing to M4. Regression covers M1/M2/M3/M5 with empty and retained transport state, plus normal M4 selection. |

QA-03 failed-run logs: `/private/tmp/readiness-openings-02.log` and `readiness-openings-03.log`. The diagnostic shows `Button.interactable=True` while its copied `CanvasGroup.interactable=False`. The first repaired replay passed M1–M5 English openings, then exposed an obsolete M5 test expectation: lesson 2 now selects through the visible group button, so expecting an offscreen-world Show Me there is invalid. Removed that assumption from the opening check; idle Show Me remains a separate later-world-target check, not waived gameplay acceptance.

QA-04 reproduction: enter M1–M5, leaving each through Pause → Exit, then re-enter M1 in Farsi and press the guided group-selection button. The second M1 entry stalled at lesson 1 (`/private/tmp/readiness-openings-05.log`, `entry=5 stage=2`). The locale change exposed the replay route; the cause is mission-state routing, independent of language.

For each new gameplay finding record: mission/locale/step, reproduction, expected versus actual, player impact, cause, changed files, exact validation and remaining coverage. Do not add speculative gameplay defects as confirmed bugs.

QA-03 / QA-04 validation: [opening regression evidence](Evidence/OpeningRegression/validation.txt), wrapper exit 0, 2026-09-16. EN entries are completed-profile replays; FA entries follow abort-and-return and are classified Retry by the runtime. This validates entry/action recovery, not complete missions.

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-05 | P1 | M4 Farsi opening starts scrolled into the middle of the rescue explanation | Fixed; focused scroll and rendered Farsi opening passed | Screenshot `Evidence/OpeningRegression/m4-fa-opening.png`. Preserve scroll only if the previous layout was actually scrollable; a fitting viewport can report normalized position zero, which must not become the initial position after the minimap settles. Hide the Team navigation during the introduction to leave room for the objective; it returns after Continue. Evidence/M04OpeningLayout/. |

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-06 | P0 | M2 replay constructs its Barracks but remains on the placement lesson and points back to Build | Fixed; focused lifecycle and complete EN/FA routes passed | Baselines were sampled before the previous match’s building cleanup. Wait for the current attempt’s resource initialization, then capture counts. Regression includes the old one-building summary before cleanup and a new placement afterward. Evidence/M02Journey/. |
| QA-07 | P1 | M2 results claim a hostile patrol was neutralized although this is now a construction/recruitment mission | Fixed; EN/FA fixtures and subsequent complete recovery journeys passed | Actual English route ends at 00:49 with enemies 0/0. Bind result objectives and recruitment statistics to M2 facts using catalog text, preserving its intended two-objective design. |
| QA-08 | P2 | M2 displays a nine-step counter although its current normal route ends after internal lesson six | Fixed; EN/FA training views show 5/5 | Narration and action routing still use nine as a mission discriminator. Display the five current player lessons while retaining internal narration/action IDs. M1 move/attack phase numbering now applies only to M1. Do not add combat merely to fill obsolete lesson slots. |

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-09 | P2 | M3 Move cue returns while some selected soldiers are still completing their route | Fixed; focused regression and EN/FA travel replays passed | Both-language replay issued repeated Move commands before Hold. The target read model checked one soldier while mission completion checks the selected group. Read movement across the selected live group in this attempt; cover old-attempt/unselected actors in a focused test. |

Historical first-pass observation: M3 originally gave roughly 65 seconds of arrival time after Hold. The later player-feedback work in QA-21 supersedes that timing: first arrival is 25 seconds and second arrival 75 seconds, with persistent wave/countdown status. Human pacing review remains a separate gate.

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-10 | P1 | M4 Board guidance frames the entire command wheel instead of the Board sector | Fixed; sector frame and crowded-layout arrow reviewed in EN/FA | Every wedge shares the full-wheel RectTransform. Use visible wedge bounds for guidance; replay hit-tests a real point on the sector rather than its portrait hole. Captures before/after saved with run 03. A follow-up keeps an arrow without a caption when the full caption cannot fit. The final shared-cue replay visibly confirmed both APC and helicopter Board arrows in EN/FA; see Evidence/SharedCues/. |
| QA-11 | P1 | M2 partial failure still says “Command squad lost” when no squad was lost | Fixed; focused regression and EN/FA rendered recheck passed | Synthetic failure capture exposed the inherited M1 cause. Use a general mission-failed status and retain actual completed Barracks / missing squad facts. |

M2 localization import now has a table-only Editor entry so result keys can be applied without rebuilding unrelated narrative prefabs or fonts. The validated import added exactly five EN and five FA records; existing records were preserved. Both-language result fixtures now render all new objective and summary text. These fixtures do not replace the completed gameplay routes.

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-12 | P1 | M5 asks for Attack repeatedly while the correctly accepted attack is already executing | Fixed; focused wrong/correct/dead-target regression passed; EN/FA routes verified | Track the commanded EngageTarget separately from movement. Wait while that exact live target is being attacked; retain corrective guidance for a wrong target or canceled order. Shared early-mission attack guidance uses the same check. |
| QA-13 | P1 | After the M5 radar is destroyed, archive recovery runs with the camera over empty ground and no visible recovery area | Fixed; EN/FA recovery views and complete routes verified | Mark the actual authored archive radius with a green ring, without click crosshairs. Frame it once when recovery starts offscreen, and let Show Me locate it if the player later pans away. Preserve the existing countdown and reset-on-leaving rule. |

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-14 | P1 | M2 hides ARIA after recruitment while the squad is still training; retaining the lesson then exposes a stale Build cue | Fixed; focused lifecycle and EN/FA recovery routes passed | The acknowledged QueueRifle recommendation was discarded, and a legacy test required that behavior. Retain its context until production completes. Closing destroys and unbinds the popup, then rebuilds ARIA. Preserve the gameplay query through that presentation rebind; do not depend on the popup snapshot. Add EN/FA training copy, verify builder closure and capture the wait. |

QA-14 was found during cancellation/pause recovery, despite both routes reaching victory. Run 02 recorded lesson 0, phase Engage, produced 0, and QueueRifle still active. Run 03 retained the lesson but reopened Build because closing destroyed its queue presenter. Runs 04–06 exposed the additional ARIA rebind clearing the retained query. The final check must prove useful visible waiting feedback, not merely mission completion.

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|
| QA-15 | P2 | Selected Barracks title includes coordinates and is truncated in English | Fixed; current sequential campaign route passed | The selected-building title now contains its localized display name, without internal grid coordinates. |

QA-14 final evidence: `Evidence/M02Recovery/`, run 07. Both languages show the localized training wait with Build closed, then complete and return to campaign. Focused validation covers retaining the query after popup destruction, releasing the wait on an empty queue, and clearing it on match unbind. The query is also preserved through ARIA presentation rebinds. Catalog import added two further EN/FA training keys without changing existing translations.

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-16 | P0 | M3 Build is unusable after completing/replaying M1 and entering M3 | Fixed; focused ownership checks and current EN/FA Build routes passed | Current-source sequential replay stops at M3 lesson 3. The old right-rail restriction sets Build's CanvasGroup false, which the cinematic lock can snapshot and later restore after mission restrictions change. Keep availability on Button.interactable; keep cinematic group ownership separate. Also guard repeated disabled-visual clears so an absent reason cannot restore empty or stale colors. Evidence/CrossMissionBuild/ records the failures; Evidence/SharedCues/ shows the final visible, enabled Build and arrow. The M1-to-M3 click route passed in run 03; the remaining appearance fix was rechecked in M3 EN/FA in run 04. |

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-17 | P1 | M3 temporarily drops its defense instruction between convoy waves | Fixed; EN/FA full defense replays retain guidance between waves | Run 03 recorded lesson 0 with phase Engage and no guidance, then returned to lesson 11. The warning ledger's inactive interval must not clear the ongoing mission lesson. Keep the Hold/wait context until its actual mission milestone. |
| QA-18 | P2 | Bottom-right Build frame is visible but its arrow is missing beside the minimap | Fixed; corner geometry and EN/FA rendered Build arrows passed | The safe approach lies just beyond the old three-step diagonal search. Extend the bounded search to five steps, still reserving the entire bounce envelope and avoiding controls, copy and minimap. Add the observed 1920×1080 corner to geometry tests. |

| ID | Priority | Finding | State | Evidence / next action |
|---|---|---|---|---|
| QA-19 | P1 | M3 says to wait on Hold while its world marker still uses click crosshairs over the defenders | Fixed; wait/action-style regression and full EN/FA defense routes passed | Shared run 04, lesson 11 screenshot. Use a plain yellow area ring during travel/wait, retaining the existing position and camera behavior. Restore crosshairs for actual destination/attack actions; do not change combat timing. |

QA-17 / QA-19 final recheck: Evidence/M03FinalWait/validation.txt, wrapper exit 0. Both EN/FA routes completed with a plain yellow wait marker and continuous defense instructions during Engage. The strengthened guard checks missing recommendations, hidden ARIA and empty body text as well as a lost lesson ID.


## Player feedback — 2026-09-16 evening

The previous success routes did not reject a brief repeated tutorial prompt, measure defense pacing, or compare the runtime M5 gate against the authored opening. These are gaps in that acceptance pass.

| ID | Priority | Finding | State | Acceptance |
|---|---|---|---|---|
| QA-20 | P1 | M2 briefly cues Build again after training empties the queue, before delivery/final mission facts finish | Fixed; EN/FA completion routes and per-frame cue checks passed | Keep training context during delivery and after current-attempt produced-unit records appear. Cancellation still releases the wait. Check each rendered frame after training begins through the finale. |
| QA-21 | P1 | M3 has long silent waits and loses its small timer between scout warnings | Fixed; final EN/FA normal-speed replays and rendered status checks passed | Shorter authored convoy delays; persistent wave/countdown/combat status independent of warning-ledger activity. Inspect EN/FA text and complete defense at normal speed. |
| QA-22 | P1 | M5 gate moves away from compound entrance | Fixed; exact placement, rendered EN/FA gate views, and both complete routes passed | Compound fence was incorrectly subject to carriageway-only placement, then relocated by generic search. Preserve its authored 8×2 clearance (visual-only sizing shrank depth to 1), require the authored objective origin, and verify actual blocking geometry. |

The M5 gate anchor was already a footprint origin. The initial center-versus-corner hypothesis was rejected after inspecting the authored asset; the confirmed cause was the shared road-barrier validation and relocation policy.

## Completion pass — 2026-09-16

| ID | Priority | Finding | State | Acceptance |
|---|---|---|---|---|
| QA-23 | P1 | M2 Build remains visually gray after M1 although the control accepts input | Fixed; fresh EN/FA campaign and out-of-order replay chains passed | A cloned disabled material outlived its restriction owner. Restore the normal material only when no disabling owner remains; preserve simultaneous cinematic/mission restrictions. |
| QA-24 | P1 | English gameplay can play Farsi tactical confirmations and warnings | Fixed; EN → FA → EN regression and current full-playback EN/FA routes passed | Resolve voice variants from the active game language for each request, not a cached first-launch preference. Exercise EN → FA → EN with the same playback bridge. |
| QA-25 | P1 | Gameplay narration continues after victory/defeat | Fixed; victory/defeat regressions and full-playback route outcome guards passed | At the mission outcome boundary stop existing gameplay sources and reject late gameplay voice requests. Preserve UI/music and the separately owned comic voice source. |
| QA-26 | P1 | ARIA mission instructions are too small | Fixed; EN 1280×720 and FA 2400×1080 rendered review passed | Body text now uses 24 reference pixels and titles 26, without auto-shrinking. Current EN M1–M5 and wider FA M1–M5 screens were reviewed. Reflow with scrolling when necessary; keep action buttons outside the text viewport. |
| QA-27 | P2 | The nine authored legacy M1 comic lines lack voice clips and Persian caption bindings | Approved 18 clips generated/imported; all 18 played once to completion in standalone player checks | Bind every line in EN/FA, preserve bindings on rebuild, and set each line's deadline from the longer recording. Local machine transcripts detect the intended language for all 18; this is not a native-speaker sign-off. Follow-up inspection established that these legacy sequences are excluded from the live campaign and have no panel artwork. Keep M1's current interactive opening; do not expose unfinished comic panels merely to exercise the recordings. |
| QA-28 | P2 | Selection descriptions such as Heavy APC remain English in Farsi | Fixed; config/catalog import and Farsi Heavy APC rendered review passed | Add the missing 30 unit descriptions as reusable localization records. |
| QA-29 | P1 | Scene grid native storage survives scene entity destruction until world teardown | Fixed; focused disposal regression and repeated-mission allocation traces passed for Game-owned grid storage | Keep storage on cleanup components, complete tracked readers, dispose when GridConfig disappears, then remove the cleanup components. Do not classify separate URP/Editor shutdown allocations as gameplay leaks. |
| QA-30 | P1 | Build guidance loses its arrow after canceling M3 placement at 1280×720 | Fixed; EN 1280×720 cancellation replay and FA 2400×1080 placement review passed | The minimap and cancellation banner require both horizontal and vertical clearance. Search nearest two-axis offsets and reserve the whole animated arrow envelope; keep controls and labels unobscured. |
| QA-31 | P2 | M3 threat-report jump button receives a Continue lesson caption | Fixed; current report/action replay passed with Jump to threat caption | The cue now names Jump to threat while the report is open. Continue remains the caption only for the actual Continue control. |

The fresh campaign run completed both five-mission unlock chains and five out-of-order replays, checking disk reloads and once-only reward settlement. It preceded the final font/audio/native-storage changes and skipped comics; see `Evidence/Completion20260916/campaign_continuity.txt` for that exact scope. Current recovery runs 05/06 (English) and 07 (Farsi) cover the final font/audio/native-storage changes with active comics played fully; their individual failures and successful mission ranges are retained in the completion evidence.


Completion-pass test repairs are recorded separately from gameplay findings: stale English-only expected validation errors; Pause attempted behind a modal report; wrong-target feedback inspected through a retired text field; supplemental M4 unload still attempted the retired hidden Do It button and was changed to use the visible passenger chip. The cleanup change also required excluding retired pools from the live singleton query and using a Burst-compatible query builder; focused/live rechecks cover those follow-ups.

| ID | Priority | Finding | State | Acceptance |
|---|---|---|---|---|
| QA-32 | P0 | Android player compilation fails because the operation-map baker calls an Editor-only footprint helper | Fixed; subsequent Android script compilation passed | The helper loads a GridAuthoringConfig through AssetDatabase. Baking remains unchanged in Editor; Android consumes baked components and must not compile an unavailable asset lookup. Original build failed with CS0103 at OperationMapBuildingAuthoring.cs. |

| ID | Priority | Finding | State | Acceptance |
|---|---|---|---|---|
| QA-33 | P1 | Android packaging duplicates shared render dependencies across map bundles and fails the existing content gate | Fixed; existing address ownership and strict content layout gates passed; validated configuration synced to source | Preserve existing addresses, labels and group ownership. Add a local shared dependency owner using Addressables’ Analyze repair, then run the unchanged duplicate/package/report gates. Do not allowlist runtime duplicates. |
