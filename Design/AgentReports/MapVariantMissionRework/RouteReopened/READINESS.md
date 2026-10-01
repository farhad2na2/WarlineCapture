# Route Reopened: AshLinePort migration

Date: 2026-10-01
Mission: CH02-M05 / saga.ch02.m05.route_reopened
Branch: codex/airlift-airfield-review
Status: desktop native validation passed. Ready for player review; human and target-device acceptance pending.

## Implementation

- Logical map: `opmap.ch02.route_reopened_port_review`.
- Logical content hash: `8a6dfb27294e25b30e45421bf0151aef3a6f3bb43f485e622ac8e11901966864`.
- Physical source: `opmap.skirmish.ashlineport_prepared`.
- Exact physical hash: `3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5`.
- Relief travels west over the south bridge at z385 to the clinic lane. Fuel travels east over the north bridge at z505 to the water-service endpoint.
- Both engineers must remain at the disruption for seven seconds; interrupted repair and records holds reset. The hub/garrison and ten-second records hold remain mandatory alongside both deliveries.
- The Port Records Office is a real, independently destructible building in the hub, spawned through the normal building request boundary for each attempt. Attempt cleanup removes it and Retry creates a fresh office. Destruction fails the attempt. Idle target acquisition skips protected records; an explicit Attack remains possible.
- Correct civilian roster totals include both engineers and both convoy vehicles, allowing real convoy-loss results to project.
- Existing command controls and approved ground/selection markers are reused. Route-specific camera glides settle before ARIA touches the world; manual camera framing uses explicit Show Me, so map navigation is preserved. The records camera frames the office and entrance together.
- Compact prepared surfaces now feed the native minimap; legacy layered surfaces retain the same feature selection.
- The authored relay comms now trigger after the first delivery. Seven original story panels and native voices remain in English and Persian.
- Campaign District Atlas UI from merged main is preserved. No Campaign prefab rebuild or new gameplay button.

## Evidence categories

### Automated checks

Preparation run 05 validates two 5x5 bridge corridors and 7x7 infantry/engineer formations, office footprint clearance, 810 surface samples with infantry/wheeled/tracked masks, deck elevation and blocked canal cells. Route rule checks cover every win prerequisite and uninterrupted holds. Seven shared minimap encoding/projection tests passed in traffic run 01. Its live armor/hauler fixture passed both bridges, blocked destination recovery, turns and staging with 1465/2599 deck samples; no mission outcome was injected. These are distinct from ordinary mission journeys. Unity-authored asset/meta formatting and byte-preserved logs retain their original whitespace.

### Native ordinary-input journeys

Full journeys enter through the current Campaign Continue card, Chapter Two, mission node five, Launch and Deploy. Tactical orders use the real touch input device and existing Select/Move/Attack controls; narrative/UI navigation uses EventSystem pointer dispatch. Fixtures use isolated temporary saves, without touching the user's profile. No health, progress or outcome injection establishes a mission win.

English ARIA run 11 passed on the attempt-owned office and held-garrison implementation: both real bridge deliveries (401/263 deck samples), engineering repair, eight-rifle hub/garrison assault, complete records hold, seven native panels/voices, 6000 Credits + 1200 Commander XP, debrief and Campaign return. The win took 186691 active milliseconds, with 18 ARIA actions.

Persian manual run 03 passed the same complete mission through normal touch gestures: both bridge deliveries (341/281 deck samples), two-engineer drag and repair, eight-rifle drag and assault, records hold, seven native panels/voices, exact first-clear rewards, debrief and Campaign return. The win took 174560 active milliseconds, with nine manual gestures. Earlier run 02 completed gameplay but failed the final media collector's wrong sprite prefix; that failure remains preserved.

Real records destruction occurred in records-loss run 01, followed by a failed probe health read after destruction cleanup removed the component. Real convoy loss occurred in lifeline-loss run 01, revealing inconsistent civilian totals and a missing defeat result. Both failures and full logs are retained; reruns are required. Lifeline runs 02–04 exposed probe/camera navigation failures. Repeated and initial implicit Route destination focus could override manual map navigation; Route now uses explicit Show Me during manual play and its existing ARIA focus path during Watch.

### Visual review

Reuses the user's approved sparse marker direction and existing Campaign typography, portraits, colorful command controls and mobile spacing. Agent review of run 11 English and run 03 Persian records-hold screenshots confirms a visible office outside the road, both squads at the entrance, existing guidance and command controls, and ARIA Play/Stop. Both victory screens show localized Route star goals and the expected XP/Credits. Native story screenshots capture panel/typewriter transitions; their seven full voice playbacks are verified separately in the journey logs. The early victory screenshots precede the last star's reveal animation. User review remains pending; mockup approval does not establish native acceptance.

### Human/device acceptance

Player review, actual target-device performance and real-device acceptance remain pending. Desktop journeys and traffic fixtures do not qualify the underlying map foundation for devices. No Android performance claim.

## Validation recovery and fixes

Lifeline run 05 stalled before project loading/compilation after successful licensing. It has no mission pass marker. The wrapper timeout monitor was stopped without terminating Unity while recovery authorization was pending. The user subsequently requested validation to continue; that Editor was already closed. Run 06 loaded successfully. This was not classified as a licensing blocker.

Run 06 exposed the actual camera cause: the Watch observation path focused Route destinations while ARIA was inactive. Route camera mutation in that path is now gated on active ARIA. Manual guidance uses Show Me. Run 07 exposed a garrison that left its defenses and died before the interrupted convoy arrived. Garrison and waiting rifles now use native Hold at spawn, with idle wandering disabled; normal player orders release Hold. The assault guidance uses a native drag over both rifle squads rather than sending one rifle at a time.

Lifeline run 08 passed real convoy combat loss, Defeat projection, no completion settlement, and native Retry with fresh actors and cleared objectives. Its screenshot missed the actual popup. Run 09 passed the actual popup/Retry gate and its labels fit, but visual review exposed a generic zero civilian-loss statistic. The result model now carries Route's recorded civilian losses. Focused run 10 passed the exact native statistic against mission facts, actual convoy combat loss, Defeat/no reward, and Retry with a fresh live office, fresh units and cleared progress. Its screenshot shows one civilian loss and no clipped result labels.

Records run 02 exposed incorrect probe projection of selection bounds; the manual driver now projects all eight corners before dragging. Run 04 completed real office destruction but exposed a production Retry bug: the destroyed map-owned office persisted. The office now uses normal per-attempt spawn and cleanup requests. Records run 06 passed office destruction, native Defeat, unchanged rewards, and Retry with a fresh live office, fresh units and cleared objectives. No live probe intervention was required in run 06. The result shows Route's own star goals and failure text; clipped status labels were reduced and verified in lifeline run 10. All required desktop gates are passed; real player/device acceptance remains pending.

## Reproduction

Keep Hub signed in. Run each entry through `Tools/CI/invoke_unity_macos.sh` with an explicit timeout and log; never use macOS batchmode.

- `Game.Editor.CH02M05RouteReopenedInputProbe.RunEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunManualPersian`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunRecordsLossEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunLifelineLossEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunTrafficEnglish`

For human review, `OpenEnglishReview` stops automation at the fresh interactive briefing, restores normal input and leaves the task Editor open with an isolated temporary review profile. This is a review handoff, not a validation pass.

Full logs and hashes: [Logs/manifest.json](Logs/manifest.json). All unsuccessful runs are retained alongside subsequent successful evidence.
