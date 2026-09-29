# CH04-M03 Split Front — production plan

Status: owner approved gameplay HUD v03 with “ok fix it then”. Native center panel and separate confirmation removed; normal Attack/Hold implemented. Revised native/rules build, English Smoke/Hold normal-input journey and Persian no-Smoke widescreen journey passed on identical player source/assets. Voiced playback and human/device acceptance pending.
Mission: `saga.ch04.m03.split_front`, after Steel Push.

## Current owner direction — supersedes confirmation design below

On 2026-09-29 the owner questioned separate launcher confirmation/cancellation and described ordinary unit Attack behavior. Proposed revision v03 removes the center panel and separate Confirm/Cancel controls, reuses existing Attack for targeting/fire and Hold for stopping before launch, and retains launcher timing, range and civilian-safety validation. This supersedes the earlier design's confirmation/cancellation requirements in the historical plan below. The owner approved v03 for implementation with “ok fix it then”. ARIA, bilingual narrative, native HUD and launcher rules were updated together. Revised-candidate normal-input evidence is recorded separately; old confirmation-based wins do not certify the revised candidate.

## Authority and operation

Follow the Campaign high-level mission catalog, Chapter 4 design, narrative sequence catalog, mission product contract dated 2026-09-28, and approved Support plan.

Neutralize the verified Vanguard battery while Ash Line's diversion attacks the forward base. Preserve protected civilian structures. Scenario-owned forces and Materials fund the required operation; no account-wallet spending or paid targeting solution. Use the existing manual ground missile launcher, ordinary selection/move/attack/hold commands and base defense. Leave enough defenders at the base while preparing the firing asset.

Smoke is the optional first Support lesson, earned after Steel Push. The mission must remain winnable without it. Expose it only after its native/runtime/normal-input gates pass. Earn Precision Strike once on successful settlement; do not promise an unavailable ability. Respect the Campaign Edition ownership and prior-mission progression policy.

## Presentation proposed for review

Reuse actual Campaign briefing, HUD and result typography, portraits, colorful controls and mobile spacing. Keep existing Split Front briefing/comms/debrief PNG pixels. Use distinct authored dialogue crops for 16:9 and 20:9.

Briefing: show the verified battery, forward-base diversion, protected civilian structures, minimum range and deliberate confirmation/cancellation. HUD: show battery/base/civilian status, launcher readiness and range, protected area and deliberate target confirmation/cancellation. Preserve visible ARIA Play/Stop. Show Me changes the camera only. Reuse native selection/action surfaces; no invented team buttons, developer controls or prototype guidance.

The ImageGen board is illustrative. Exact map geometry, UI bindings and numerical budgets require native implementation and validation. It does not establish readiness.

## Story and voices

Preserve stable `seq.ch04.m03.brief`, `.comms`, `.debrief` identities.
Brief: ARIA explains verification, minimum range, confirmation and cancellation; Dalia assigns split defense; civilian protection is explicit.
Comms: Qassem offers ARIA complete memory and the Commander unrestricted Relay control. ARIA refuses autonomous authority before the Commander answers.
Debrief: the battery and diversion fail without an unsafe strike; the targeting package expects ARIA authorization; imported Relay-compatible hardware leads to the air-support site.

Cast: Qassem, ARIA, Dalia, Samira. Author English and conversational Persian copy using the existing cast/audio pipeline; The user pre-approved voice sending on 2026-09-29, but automatic approval review rejected the concrete ElevenLabs upload. An exact-payload/destination approval question is pending. Do not retry or bypass it. Until approved, the eight dialogue states are captioned; bilingual voiced playback remains a pending gate.

## Implementation and acceptance gates

1. Register mission configuration, sequence/unlock progression, localized briefing/HUD/result, scenario forces, resources, map and native assets.
2. Certify launcher range/minimum range, preparation, flight/impact timing, explicit confirmation, cancellation before launch and protected civilian targeting/collateral behavior. Existing launcher source is not proof of these UX gates. The catalog permits a ground raid only if launcher gates fail; do not silently substitute a fake strike.
3. Prove base diversion, firing-asset protection, battery destruction, civilian failure, optional Smoke, legitimate win/loss, replay cleanup and idempotent settlement.
4. Run focused wrapper validations with explicit timeout/log and required pass markers. Keep Hub open and signed in; use the checked macOS GUI wrapper without batchmode; retain failed logs.
5. Inspect implemented English 16:9 and Persian 20:9 native briefing/comics/HUD/targeting/result/return. Review full captions, range/confirmation/cancellation and ARIA Play/Stop.
6. Complete both normal-input mission journeys: actual combat and launcher operation, optional Support use and a no-Support win, mandatory voices/comics, ARIA, result, reward settlement and Campaign return. Injected outcomes and older candidate wins cannot pass this gate.
7. Report owner visual review, automated checks, normal-input journeys and human/packaged-player/device acceptance separately.

## Environment inspection

Unity 6000.5.2f1 is running with Pipeline 0.6.0-exp.1 at port 7800. CLI discovery works outside the process/network sandbox. Native builders and all executeMethod validation use the checked macOS GUI wrapper with Hub open. No Editor termination, licensing recovery, batchmode or raw scene/prefab/asset editing was used.

Readiness remains pending until the latest implemented candidate passes native and normal-input gates. External voice sending awaits the exact-payload approval requested after automatic review rejection.


Final evidence: [readiness.md](readiness.md). Current normal Attack candidate: build02, English Smoke EN01 and Persian FA01 exited 0 with all 18 required markers. Historical confirmation-based build08/Smoke03/FA02 evidence is retained separately. Visual direction approval, native screen review, automated checks, normal-input first clears, voice playback and human/device acceptance are separately recorded.
