# M03 implementation evidence — in progress

> Current completion work and superseding results: [M3 / M4 Editor completion QA, 10 September](../M03_M04_Editor_Completion_2026-09-10.md). This older report retains its original checkpoint scope.

User scope: complete M3 implementation and QA in Unity Editor; Android/device QA excluded.

Branch: `codex/m03-radar-warning`, based on `b6b8153d2`. Existing dirty Arabic and Oxanium font assets are unrelated and excluded from task commits.

## Latest checkpoint — 2026-09-09

The sections below are chronological engineering evidence, including failures that were later fixed. Use [the current QA index](editor_qa_index.md) and [implemented architecture](implemented_architecture.md) for the current state. Editor completion is not yet claimed.

## Verified increments

- Physical source and Barracks four-member quantity: `/private/tmp/warline-m03-feasibility.log`, marker `[M03RadarWarningFeasibilityValidation] result=Passed physicalSource=unchanged barracksQuantity=4`. See `feasibility_probe.md`.
- Outcome/star rules: `/private/tmp/warline-m03-rules-02.log`, marker `[M03RadarWarningRuleValidation] result=Passed tests=11`. Includes all-enemy completion, failure priority, missing-member integrity failure, squad-loss recovery, camera/producer readiness and independent civilian/post-damage stars.
- Map and canonical assets: `/private/tmp/warline-m03-data-01.log`, marker `[M03RadarWarningConfigBuilder] result=Passed missions=3 maps=3 hostiles=7 civilians=4 rifles=8 sensor=1`. Logical map has 34 anchors and 18 route points, five-cell-square surface clearance, source hashes preserved. See `map_geometry.md`.
- Provisional narrative installation: `/private/tmp/warline-m03-narrative-01.log` produced seven dialogue panels across three sequences, English captions and Persian locale entries. M02 art is temporarily reused, voices absent. This is integration scaffolding, not final media acceptance. This run also exposed a Burst string-construction error in the new narrative policy; corrected to static fixed strings before the launch probe.

## Current QA findings

- Launch probe `/private/tmp/warline-m03-launch-04.log` passed the canonical 20-member roster, exact physical-post binding, completed initial Barracks, real brief/comms Skip+Confirm controls, and the completed camera return before the mission clock starts. The first launch's four-reward buffer overflow and ignored campaign-comic Skip request are fixed. Shared regressions remain pending.
- Starting resources were applied before the map's normal resource initializer. The mission initializer now waits for that setup and the spawned mission force. `/private/tmp/warline-m03-convoy-02.log` and subsequent runs confirm actual ECS totals of 50,000 Credits and 100/100 Materials.
- `/private/tmp/warline-m03-convoy-01.log` failed when an Editor asset refresh unloaded the map during the attempt. The probe now suspends automatic asset refresh during Play Mode, restores it on exit, and still fails on runtime exceptions or missing required members.
- Convoy run 02 exposed a shared vehicle-wreck buffer invalidation. `VehicleDestroyedVisualSystem` now snapshots linked entities before adding components to children; the existing adornment regression includes two linked visuals. Subsequent convoy runs passed through vehicle deaths without that exception.
- Convoy run 03 stalled with six defeated enemies and an APC still alive. Mission vehicles now have explicit `vehiclesSelfSupplied` authoring, disabling external Fuel consumption on these scenario instances. This avoids a hidden logistics requirement on the loaned sensor and convoy. This does not change their reusable prefab definitions or other missions.
- Convoy run 04 ended in an **idle, untouched-post Victory at 154.644 seconds**. This is a failed balance outcome despite the harness's terminal-state pass marker. Damage observations identify existing map guard towers at (674.44,349.63) and (726.41,365.17) as the attackers. M3 now marks authored map defenses dormant via an attempt policy; player-built defense structures remain eligible, and the tag is removed when leaving the mission scope. Physical assets are unchanged.
- Convoy run 05 confirms that removing those automatic defenders exposes route failures: all seven enemies survive but stall near early route points. **Terrain-only route clearance is insufficient.** The next inspection captures the actual GridWalkable, DynamicBlocker and WheeledVehicle surface intersection and inspects the accepted movement state. No route or balance acceptance is claimed.
- A persistent warning ledger, scout/sensor source distinction, unknown vehicle count, contact estimate, stale state, deterministic focus and separate presentation acknowledgement are implemented. The live Persian HUD shows the scout warning and contact estimate. Detector pause now preserves warning state, suppressed enemies are excluded, and M3 minimap markers use recorded sightings. Unit tests, Ping, real popup/Jump binding and complete guidance integration remain pending.
- M3 briefing copy and an incremental central `en`/`fa-IR` catalog import are implemented. The legacy ARIA surface still displays M1 lesson assumptions; replacing those is required, not accepted.
- Editor shutdown logs report a missing-script save error for a temporary HUD prefab preview and leaked preview scenes. A live read-only asset audit found no missing MonoBehaviours in the saved HUD prefab. Investigate preview lifecycle/exit timing; do not delete components from the saved asset without identifying the cause.
- The initial map attempt correctly failed because a 105m planning camera exceeded the physical map camera-height bounds. Corrected to 95m; canonical data build passed.
- Earlier proposed staging coordinates (580,300)/(700,300) are blocked in the actual map; current staging uses the validated western road.
- Final narrative art/voice, all-class guide, twelve-step guidance, Ping, rewards, balance, lifecycle, both-language visual QA and affected regressions remain in progress. No gate is being counted complete from source edits alone.

## Validation invocation

All runs use `Tools/CI/invoke_unity_macos.sh` with GUI licensing, explicit log and timeout, with Hub kept open. Rules/builders use `-quit -executeMethod`; the asynchronous Play Mode probe uses `-executeMethod` and exits only its wrapper-owned Editor after returning to Edit Mode. No device build, licensing reset, IPC removal or unrelated process termination.

## 2026-09-09 checkpoint — supersedes the pending-state notes above

- The actual runtime-grid survey now validates **21 route points**, with **37 logical anchors**, and the inner core at cell `(942,342)`. Logical hash: `536dd882db4f6a3a8eec9493fa0a3065b191f5d932ad5fb1eb06e348e3226762`. Physical map assets remain unchanged. Mission convoy entities are excluded from ambient squad AI, and the existing patrol owner retries interrupted normal movement requests.
- **Idle defeat:** `/private/tmp/warline-m03-convoy-07.log`, core breach at 98.522 simulated seconds, zero of seven hostiles defeated. This corrected the earlier unintended passive victory and route stalls.
- **Rifle strategy victory:** `/private/tmp/warline-m03-rifle-defense-02.log`, 193.459 simulated seconds, seven of seven hostiles defeated, no core breach or post destruction. Eight rifles received normal player move/attack requests; the probe did not edit health, positions, or outcome. A real Ping at 55 seconds consumed one charge, returned a valid empty scan, and rejected a duplicate during cooldown. Time scale 3 accelerated simulation; this is not a real-time performance measurement.
- **Ping/AI/wreck regression:** `/private/tmp/warline-m03-ping-01.log`, `[M03RadarPingValidation] result=Passed pingTests=4 aiTests=4 linkedWreckRegression=1`. Tests include the existing detector job, moving-away vehicles, future/infantry/air/neutral filtering, duplicate/cooldown/pause/exhaustion, dead and stale sensors, and sensor death after reservation.
- **Guide data and prefab build:** `/private/tmp/warline-m03-ui-build-04.log` and later UI probes verify `[M03GuideBuilder] result=Passed topics=12 identities=57 runtimeConfigs=51`. Six naval entries explicitly have no runtime config. Cards load one canonical config at a time through Addressables; no copied balance table. Build 04's subsequent shell-owner error was fixed by binding the actual scene-owned `UIShellContentView`.
- **ARIA:** real Play Mode HUD in `/private/tmp/warline-m03-ui-proof-02.log` displayed the correct M3 first lesson and twelve-step count. Optional reinforcement completion now reads actual produced-unit facts because M3 has no mandatory training objective. Full bilingual presentation and all-step interaction acceptance remain pending.
- **UI findings:** proof 01 caught optional serialized-object fake-null access; fixed. Proof 02 exercised warning/Jump but stalled opening the guide after the warning close. The shell consumed the first popup request and cleared the rest. It now preserves FIFO requests, with a regression for close-then-guide during animation. Guide return context also considers pending close/show intents. Live acceptance is being rerun.
- The minimap's secondary relay MonoBehaviours were moved to their own matching script filenames. The previous missing-Viewport-script save error did not recur in proof 02; two leaked Editor preview scenes were still reported and remain under investigation.
- UI proof 03 hit a compiler dialog after a missing namespace in the harness; fixed. Its wrapper timed out at 480 seconds and terminated only its own Editor. Proof 04 found a managed string in the new radio `ISystem`; changed to a fixed string. Neither run is acceptance.
- M3's outage clue now uses the existing assistant message/speech arbitration path during combat; M3 no longer selects the blocking comms comic automatically. M2's original comms policy is preserved. Final media and live radio acceptance remain pending.
- Construction strategy QA now uses actual Tower/Barrier placement transactions, the live confirmation button, cancellation, real four-rifle production and budget assertions. It has not yet run.

Still required: passing guide/pause/locales/captures, two-strategy construction and recovery checks, final comic art and voice, camera path and restoration evidence, results/replay/rewards/lifecycle stress, architecture checks, measured Editor performance, and final documentation reconciliation. No Android QA is required.


## 2026-09-09 camera, construction, media and results checkpoint

This section supersedes earlier pending-state notes where a later pass is listed. M3 is still in implementation; source-only changes below are not accepted Editor evidence.

### Completed Editor observations

| Lane | Evidence | Observed result |
|---|---|---|
| Guide / warning / pause | `/private/tmp/warline-m03-ui-proof-07.log` | Passed and wrapper exit 0. Real warning and Jump preserve selection. Guide freezes mission time, actor positions and health; 57 classes in both locales; at most one class handle; exact return to Pause; 16:9 and 20:9 captures. Later digit, minimap and large-text fixes require the expanded rerun. |
| Construction defense | `/private/tmp/warline-m03-construction-03.log` | Passed and exit 0. Four forward rifles plus reserve, actual Tower at (953,388), Barrier at (951,395), four paid rifle reinforcements. Cancellation spends nothing; Tower has an active weapon; 12,000 Credits remain. Victory at 208.505 simulated seconds, 7/7 defeated, no post destruction/core breach. No claim that the Tower contributed damage. |
| Full opening 16:9 | `/private/tmp/warline-m03-camera-01.log` | Passed and exit 0. Actual RTS pose captured; simultaneous pan/zoom; both key-area holds; exact return within focus .15 / pose .12 tolerances. Mission clock remains zero; duration 14.190 seconds. |
| Full opening 20:9 | `/private/tmp/warline-m03-camera-wide-01.log` | Passed and exit 0. Same checks, duration 14.136 seconds. |
| Skip opening | `/private/tmp/warline-m03-camera-skip-02.log` | Passed and exit 0. Actual live button interrupts first move, restores captured RTS pose and preserves zero mission clock; duration 2.457 seconds. First skip run exposed the generic cinematic lock disabling its own skip control; fixed through the serialized control binding. |

### Implemented, awaiting the next live checks

- Added a three-second optional post finale through `CampaignMissionPatrolOrderSystem`, using existing camera requests. `SecureCorridor` freezes the mission clock until presentation completes; failure still takes priority. No actor is repaired, moved or removed for the shot. Reduced motion and skip reuse the camera-tour state.
- Result projection now identifies M3, gates Victory until its debrief finishes, uses actual post/civilian facts, localizes failure reasons and rewards, and refreshes on locale changes. Result view no longer inherits M1's hard-coded zero civilian count or command-squad-loss reason. Added a result guide entry.
- Guide has twelve topics, all 57 class identities, search/filter coverage and large-text layout. Persian numeral shaping accounts for RTLTMPro's native-digit behavior. Expanded harness checks actual glyph order, every topic, every class, all filters, empty-search handle release, and both text sizes.
- Minimap resolves the actual installed Header section instead of a cross-prefab Footer reference. M3 filtering excludes static map props and preactivation/unobserved enemies, retains real produced units and runtime buildings, and includes protected civilians through the existing neutral-marker presentation.
- Radio archive is a caption/art page within the guide; it becomes readable after the clue is delivered (or after an M3 first clear). It does not execute gameplay or settlement. Campaign archive and result guide use the same popup owner.
- Seven final text-free comic PNGs have been generated and visually inspected. C01 was revised to remove an unintended aircraft. Exact prompts, provenance and crop policy are in `comic_art_manifest.json`. The Editor importer creates fourteen authored 16:9/20:9 sprite crops; final installation/capture remains pending.
- Voice generation is **pending explicit user approval**. Automatic approval review rejected sending the 23 script pairs to `api.elevenlabs.io` without payload/destination approval. `voice_payload_review.json` contains the exact text. No request was sent and no voice clip generated. Captioned-art QA remains independent.
- New settlement tests cover first-clear XP/Credits/Tower/Ping, whitespace normalization, duplicate tokens and duplicate first clears, replay Credits, preowned conversions, foreign/replay unlock rejection, and M04 availability. New narrative tests cover four truthful debrief variants.
- Existing source ceilings were not changed. Same-owner partial files group AI membership checks, popup request consumption and Pause/Settings lifecycle. Scan target construction now uses the cached query builder and remains within its strict byte ceiling. The consolidated architecture test is authoritative.

### Failed/incomplete validation runs

- Reduced-motion run 01 stopped at a missing test namespace, then wrapper timeout 540 (exit 124). The source error is fixed; this is not a licensing failure and no reduced-motion acceptance is claimed.
- Consolidated regression run 01 caught test-harness `SettingsService` ambiguity and a `ReadOnlySpan` LINQ assumption, then wrapper timeout 600 (exit 124). Both source errors are fixed; offline diagnostics subsequently compiled all ten affected assemblies. Offline diagnostics are not Editor acceptance.
- Production inspection 01 proved real produced entities but timed out during Editor exit; it does not count as a passed lane. Later construction 03 is the accepted transaction/play observation.
- Two leaked Editor preview scenes remain under investigation. No unrelated Editor or preview content has been terminated or deleted.

Current next checks: consolidated regression 02; final art importer and narrative installation; reduced motion; expanded normal/large guide UI; complete real victory -> finale -> three debrief panels -> localized result -> guide-return journey. Then remaining ARIA command parity, recovery, retry/isolation, lifecycle and measured Editor performance. Human cohort/native-language review must be distinguished from agent Editor QA. Android is excluded by user direction.


## 2026-09-09 expanded UI / regression checkpoint

- `/private/tmp/warline-m03-ui-proof-08.log` passed the expanded normal-text UI lane: all 12 topics and 57 classes in both locales, actual Persian count glyph order, all availability filters, empty search releasing its class handle, at most one resident class, warning Jump selection preservation, frozen clock/positions/health, and return to Pause. The Persian 20:9 guide capture was inspected and is readable. Large-text and radio archive checks are still pending.
- `/private/tmp/warline-m03-regression-05.log` passed ten suites: M3 rules (12), Ping/detector plus AI/wreck checks, warning interaction (5), shell audio/popup ordering (9), M3 narrative flow (3), M3 settlement (2), M1 rules (14), M2 rules (11), M2 resources (9), and M2 narrative (25). The overall regression failed at architecture. Run 04 used an incorrect namespace in the executeMethod argument and ran no tests; its exit 0 is not acceptance.
- `/private/tmp/warline-m03-architecture-audit-01.log` ran all 17 existing checks independently: 11 passed and 6 failed. It identified pre-existing first-launch/helper/UI/Resource Exchange guard violations and two M3 source responsibilities to correct. M3 popup lifecycle now remains in the approved existing `UIShellContentView` owner; its partial files contain serialized state, while HUD bindings are grouped separately. Popup enums and ARIA command handling were extracted as coherent contracts/behaviors. No baseline hash, size ceiling, approved exception or test assertion was changed. A new audit is required; this is not an architecture pass.
- `/private/tmp/warline-m03-camera-reduced-02.log` passed with wrapper exit 0: mode 2, 20:9, actual captured RTS restored within focus .15 / pose .12 tolerance, no cinematic pan/zoom, opening clock zero, 641 ms presentation. Full and skip paths have earlier passes. A 30-second optional-camera watchdog has now been implemented, but the fault path is not yet accepted.
- Final M3 comic art is installed: `/private/tmp/warline-m03-art-import-01.log` passed seven sources, fourteen crops and stable sprite IDs. `/private/tmp/warline-m03-result-01.log` completed the real rifle victory, three-second finale with frozen clock, three M3 debrief panels and result-guide return. That functional pass did **not** imply visual acceptance: inspected captures exposed mixed-script font selection, inherited M1 art, overlapping labels and truncated rewards.
- `/private/tmp/warline-m03-result-02.log` confirmed the mixed-script fix and M3 backdrop but **failed** the stronger Persian reward overflow assertion. Source now gives rewards a bounded 245-high panel / 166-high text body and reapplies M3 objective columns after responsive layout. All four locale/aspect combinations, ellipsis detection and full revealed debrief captions are part of the rerun. No result visual acceptance yet.
- M3 campaign and briefing now bind the final B01 preview, and both campaign archive buttons plus the warning guide button use the existing guide popup/return contract. These new entry points await live QA.
- Voice remains pending the explicit ElevenLabs payload approval already requested. No voice payload was sent. Android remains excluded.


## 2026-09-09 result and interaction checkpoint

- `/private/tmp/warline-m03-result-04.log` passed with wrapper exit 0: real rifle victory, three-second finale with frozen mission clock, three fully revealed M3 debrief panels, and both locales at both aspects. Inspected result captures show all four rewards and separated objective/status columns. The radio archive capture exposed the result being recreated over the guide; the result binder now defers to the shell's active guide modal. That visibility fix still needs a live result rerun. Run 03 failed in the campaign-art prefab builder (root versus child component), then timed out; the builder now resolves the child view, and probe entrypoints exit fail-fast on startup exceptions.
- `/private/tmp/warline-m03-ui-large-01.log` failed on a Persian bottom navigation label (36-high text, 48.58 preferred). Guide Previous/Next buttons now have 56-high text space. Large run 02 is in progress.
- Interaction run 02 found ARIA tutorial body overflow in the inherited compact M1 card. M3 now reallocates portrait space to its longer lessons; large text has additional body height. M1 geometry restores when leaving M3. The M1-specific numbering/command substeps no longer alter M3 steps 3 and 4.
- `/private/tmp/warline-m03-interaction-04.log` passed warning interaction (7 plus ten World teardowns), missing-camera fallback (3), ARIA presentation (48 language/size combinations), fourteen final comic crops, assistant gateway (7), assistant intent system (16), and focused unit command (4) checks. The aggregate failed because the legacy focused-command runner reports by exception instead of setting ValidationExit. The aggregate now invokes that runner directly; the final ARIA prefab suite and strengthened M3-to-M1 layout/progress assertions await rerun.
- Added a ten-cycle live menu -> M3 -> class guide -> restart -> Pause Exit -> menu probe, checking fresh roster/budget/Ping, duplicate restart rejection and previous unit disposal. Not yet run. Added a recovery probe with explicitly labeled sensor-death fault injection, unused Ping charges, initial rearward positioning and correction at 35 seconds. Not yet run.
- The M3 HUD read model now caches unchanged presentation strings and rejects stale mission state when PlayRequested is zero. No source-growth guard or performance threshold was relaxed.


## Final Editor QA checkpoint — 2026-09-09

This M3 QA snapshot records a playable implementation with open acceptance gates; it is not an all-green or Editor-complete declaration. Git delivery of the combined M3/M4 work was subsequently authorized by the user. The reconciled M3 QA record is [editor_qa_index.md](editor_qa_index.md), with full-log hashes/compact markers in editor_validation_ledger.json and final working-source hashes in source_snapshot.json. Android is excluded by user direction.

- Behavior-audit-07: 30 suites pass / one missing-voice failure; wrapper exit 0 does not override failed aggregate. M1 HUD restrictions pass all eight tests, including the cleared-session Skirmish regression.
- Architecture-audit-04: 11 pass / six fail; unchanged guards, thresholds and exceptions. Fifteen reported paths: fourteen unchanged from base; UIShellContentView already fails its base identity guard and is also modified by M3.
- Lifecycle-04: all ten real cycles pass, including paid/produced ownership removal, fresh restart state, cleared exit buffers and restored handles/clock. Cross-mission-04 passes actual M1/M2/M3/Skirmish routing.
- Defeat-result-03 found an idle win at 217.112 seconds, invalidating the probe assumption that idle always loses. Earlier idle-09 lost at 221.119 seconds. Idle balance remains unaccepted; no production balance was changed to force QA.
- Defeat-result-04 passes after an Editor-only probe correction: eight rifles actually retreat using ordinary Move (28.803 seconds), followed by a real core breach (117.369 seconds), no victory comic/rewards, localized result/guide return, and real Retry into a fresh 20-member attempt. Persian 20:9 result visually inspected. No health, transform, path or outcome writes.
- Frame-time budgets pass at normal speed; inclusive frame allocations remain nonzero. Raw attribution validates 0 B in ten sampled ECS owners, but changing HUD cooldown text still allocates. No unsupported current-thread counter is treated as zero-allocation evidence.
- Forty-six English/Persian voice clips await the existing explicit ElevenLabs payload approval. No scripts sent. Independent fluent Persian editing/listening and participant learning/fun evidence are absent. The tracker remains 32/47 individually verified, with combined dependencies open.
