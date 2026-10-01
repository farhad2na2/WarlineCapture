# Route Reopened: AshLinePort migration

Date: 2026-10-01
Mission: CH02-M05 / saga.ch02.m05.route_reopened
Branch: codex/airlift-airfield-review
Status: implementation checkpoint; final native validation paused for requested recovery authorization. Human and target-device acceptance pending.

## Implementation

- Logical map: `opmap.ch02.route_reopened_port_review`.
- Logical content hash: `cb4ec7727e459e719325d1bfdc7d681ef6b5a856f127892eb38565f4c6f0da63`.
- Physical source: `opmap.skirmish.ashlineport_prepared`.
- Exact physical hash: `3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5`.
- Relief travels west over the south bridge at z385 to the clinic lane. Fuel travels east over the north bridge at z505 to the water-service endpoint.
- Both engineers must remain at the disruption for seven seconds; interrupted repair and records holds reset. The hub/garrison and ten-second records hold remain mandatory alongside both deliveries.
- The Port Records Office is a real, independently destructible building in the hub. Destruction fails the attempt. Idle target acquisition skips protected records; an explicit Attack remains possible.
- Correct civilian roster totals include both engineers and both convoy vehicles, allowing real convoy-loss results to project.
- Existing command controls and approved ground/selection markers are reused. Route-specific camera glides settle before ARIA touches the world; manual camera framing uses explicit Show Me, so map navigation is preserved. The records camera frames the office and entrance together.
- Compact prepared surfaces now feed the native minimap; legacy layered surfaces retain the same feature selection.
- The authored relay comms now trigger after the first delivery. Seven original story panels and native voices remain in English and Persian.
- Campaign District Atlas UI from merged main is preserved. No Campaign prefab rebuild or new gameplay button.

## Evidence categories

### Automated checks

Preparation validates two 5x5 bridge corridors and 7x7 infantry/engineer formations, office footprint clearance, 810 surface samples with infantry/wheeled/tracked masks, deck elevation and blocked canal cells. Route rule checks cover every win prerequisite and uninterrupted holds. Shared minimap encoding/projection tests and the native traffic fixture are implemented but not yet executed. Their results must be recorded separately from mission journeys. Source/document/JSON staged whitespace checks pass; Unity-authored asset/meta formatting and byte-preserved logs retain their original whitespace.

### Native ordinary-input journeys

Full journeys enter through the current Campaign Continue card, Chapter Two, mission node five, Launch and Deploy. Tactical orders use the real touch input device and existing Select/Move/Attack controls; narrative/UI navigation uses EventSystem pointer dispatch. Fixtures use isolated temporary saves, without touching the user's profile. No health, progress or outcome injection establishes a mission win.

English ARIA run 10 passed both real bridge deliveries, engineering repair, hub/garrison, the complete records hold, all seven native panels/voices, 6000 Credits + 1200 Commander XP, debrief and Campaign return. Later final runs are required for subsequent source changes.

Persian manual run 02 completed gameplay, seven voices, settlement and return, but its final media gate failed because the collector expected a wrong sprite-name prefix. Its failure is preserved; it is not represented as a passed run.

Real records destruction occurred in records-loss run 01, followed by a failed probe health read after destruction cleanup removed the component. Real convoy loss occurred in lifeline-loss run 01, revealing inconsistent civilian totals and a missing defeat result. Both failures and full logs are retained; reruns are required. Lifeline runs 02–04 exposed probe/camera navigation failures. Repeated and initial implicit Route destination focus could override manual map navigation; Route now uses explicit Show Me during manual play and its existing ARIA focus path during Watch.

### Visual review

Reuses the user's approved sparse marker direction and existing Campaign typography, portraits, colorful command controls and mobile spacing. Native English/Persian screenshots are reviewed by the agent; user review of this mission remains pending. Mockup approval does not establish native acceptance.

### Human/device acceptance

Player review, actual target-device performance and real-device acceptance remain pending. Desktop journeys and traffic fixtures do not qualify the underlying map foundation for devices. No Android performance claim.

## Current validation interruption

Lifeline run 05 stalled before project loading/compilation after successful licensing. It has no mission pass marker. Recovery permission for only this task-owned Editor was requested under AGENTS.md; it is not classified as a licensing blocker. Final positive and negative reruns remain pending. The wrapper timeout monitor was stopped without terminating Unity while recovery authorization is pending.

## Reproduction

Keep Hub signed in. Run each entry through `Tools/CI/invoke_unity_macos.sh` with an explicit timeout and log; never use macOS batchmode.

- `Game.Editor.CH02M05RouteReopenedInputProbe.RunEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunManualPersian`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunRecordsLossEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunLifelineLossEnglish`
- `Game.Editor.CH02M05RouteReopenedInputProbe.RunTrafficEnglish`

For human review, `OpenEnglishReview` stops automation at the fresh interactive briefing, restores normal input and leaves the task Editor open with an isolated temporary review profile. This is a review handoff, not a validation pass.

Full logs and hashes: [Logs/manifest.json](Logs/manifest.json). All unsuccessful runs are retained alongside subsequent successful evidence.
