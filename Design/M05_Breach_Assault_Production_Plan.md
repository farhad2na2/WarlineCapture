# M05 Breach Assault — production and implementation plan

Mission: `saga.ch01.m05.breach_assault`. Scenario: `scenario.ch01.m05.breach_assault`.

## Player experience

A Chapter 1 finale applying selection, movement, focused attacks and combined arms. The player leads two rifle squads and a heavy APC against the fortified Ash Line communications node. Breach the marked gate, destroy the transmitting core, then secure the archive area while defeating a clearly announced counterattack. Preserve the archive: it is recovered by occupying its area, never by shooting it. The APC's survival is a bonus rather than an arbitrary defeat condition. Losing all assault units or missing the operation deadline ends the attempt with a clear reason and a working retry.

Target first-play session: 3–6 minutes including the briefing, guide reading and finale, with a 12-minute gameplay failure deadline. An automated, immediate-response guided combat run completed the active objectives in 1:16; this is a lower-bound execution check, not evidence of a 6–9 minute human play session. Three stars: complete the mission, preserve the heavy APC, finish in under nine minutes. Briefing comics, the opening tour, initial ARIA plan acknowledgement and paused field-guide reading do not consume the deadline. No account Credits in the gameplay resource strip. Match resources remain Materials, Oil and Fuel; mission-supplied support has a clearly explained fuel allowance.

## Mission flow and guidance

1. Briefing comic: Dalia identifies the fortified relay; Samira explains why the archive must survive; ARIA separates the military target from the protected evidence.
2. Opening camera tour starts at the playable RTS view, focuses the staging squad, gate and core/archive, then returns to the staging squad. All positions come from map anchors and live entities. Reduced-motion skips travel and retains the same information.
3. ARIA explains the three objectives. Acknowledgement advances only that explanation.
4. Select the heavy APC, with a surviving rifle squad as the fallback. Highlight the actual unit.
5. Attack the gate. Highlight Attack, then the live gate. Observe actual destruction before advancing.
6. Advance the rifle squad through the breach. Highlight selection, Move and the destination in order; wait for arrival.
7. Keep the APC supporting the advancing rifles. Its survival is an optional star, never a required victory condition.
8. Defeat the garrison and destroy the transmitting core. Keep the camera near the action, without hijacking player pans during normal play.
9. Announce the counterattack before it reaches the player. Show a source and actionable direction; no flashing/restarting notification every frame.
10. Move a surviving assault unit into the archive zone and hold it uncontested for 20 seconds. Show remaining hold time and clearly explain when enemies interrupt progress.
11. Finale camera briefly shows the secured node and returns control to the result flow.
12. Debrief comic reveals Protocol Fragment 1, a revoked ARIA credential and the first obscured Qassem message, without revealing later chapters. Results show stars, time, APC survival, actual rewards and chapter completion. Return goes to Campaign.

Guidance runs on every M5 entry, including retry/replay. Show Me always exposes the current actionable target; while waiting it is disabled. Do It performs one UI/command action per press, never silently completes an objective. Naturally completed objectives advance their lessons. Pause, guide and narrative suspend mission timing consistently. The guide uses the existing build-popup visual language, mobile controls, left tabs, large close button and canonical 57-class inventory with accurate M5 availability.

## Presentation and languages

Use the established M3/M4 illustrated comic style and current Dalia, Samira and ARIA identities. Create mission-specific briefing/comms/debrief artwork with 16:9 and 20:9 framing. Reuse the approved HUD, target markers, command wheel, guide, warning and result components. Do not add an unrelated central debug panel. ARIA text fits above its large buttons, the selection panel fits content, and the independent minimap sits above Build with the established small margin.

All player-facing text belongs in the localization catalog, including mission selection, objectives, warnings, guidance, examples, defeat reasons, rewards, chapter completion and narrative captions. English and Farsi receive equal coverage, correct RTL shaping and readable line wrapping. Voice lines must use the matching localized script, be interruptible/replayable, and never be required for understanding an objective. New paid voice generation requires its concrete payload approval if the existing service's approval policy requires it; complete independent implementation first.

## Architecture and implementation order

1. Add validated breach scenario configuration, projected immutable blob data and per-attempt ECS state. No gameplay facts in UI views.
2. Author a dedicated logical mission map over the shared physical city using surveyed reachable anchors. Spawn actual combat targets and forces through existing runtime boundaries. Keep unrelated authored defenses dormant. Gate destruction must open the route physically; the core must be an identifiable structure, not an impostor.
3. Add breach runtime projection and pure progression rules. Track target identity, initialization, deaths, hold progress and counterattack release with session/attempt/version validation. Reuse authoritative combat, movement, damage, rewards and settlement.
4. Add tutorial projection, UI gateway contracts and current-action resolution. Reuse highlight/camera requests, never mutate mission facts from a Show Me request.
5. Add mission catalog, unlock/readiness, reward preview/settlement, chapter completion and retry/return integration. M4 victory unlocks a genuinely deployable M5. Unimplemented Chapter 2 missions stay unavailable.
6. Build mission, scenario, narrative, localization and guide assets through Editor builders. Preserve M1–M4 configuration and art.
7. Validate first clear, defeat/retry, replay and return. Iterate on real Editor playthrough findings before declaring readiness.

## Acceptance and QA

- Config identity, anchors, prefab references, mission objectives, reward IDs and localization keys validate.
- Combat objectives use actual target damage/death; duplicate requests and stale attempts cannot grant progress or rewards.
- Initializing entities cannot cause a false defeat. Destroying the gate opens traversal. Core and archive rules are distinct.
- Counterattack is announced, reachable and beatable; no inactive guards produce shooting audio.
- Normal and large text fit in English/Farsi at 16:9 and 20:9. Every enabled Show Me produces a visible target. Controls retain mobile hit areas.
- Opening/finale cameras show their targets with sufficient clearance, return correctly and honor reduced motion. Infantry animate when moving; vehicles remain assembled and grounded.
- A live first-clear Editor journey uses ordinary selection, move and attack paths without injecting objective completion facts. Verify result, rewards, campaign return and replay. Separately exercise real failure and retry.
- Run focused mission tests, localization/presentation checks, relevant M1–M4 regressions and architecture checks without weakening baselines.
- Logs/captures remain under `/private/tmp`; no evidence images in Design. Editor QA only, per user preference.

## Delivery record

Current fixes committed and pushed before M5 work: `2c84bdc53` on `codex/m03-radar-warning`.

Captioned implementation passed the complete English guided journey and Persian replay, real deadline/retry, fourteen shared regression suites, ten M5-focused suites and 139 architecture checks. See [the acceptance record](AgentReports/M05BreachAssault/Implementation_Status.md) for evidence, fixes and limits. New paid M5 voice clips remain pending approval; captioned acceptance does not claim voice acceptance.
