# Watch ARIA play — implementation plan

Updated: 2026-09-18. Owner: project owner; implementation and evidence maintained by the implementing agent/team.
Status: **implementation in progress; guided campaign prototype under validation**. This package answers the owner's request for a documented feature usable across game modes and matches. See [current implementation status](IMPLEMENTATION_STATUS.md) for actual coverage and open gates.

## Read this package

| Document | Purpose |
|---|---|
| [Concept](CONCEPT.md) | Accepted product direction and original recommendation |
| [Presentation and interaction](UX.md) | Start/Stop, hand, teaching, localization and mockup interpretation |
| [Architecture](ARCHITECTURE.md) | Observation boundary, ECS ownership, planner, input and lifecycle |
| [Mode and match coverage](COVERAGE.md) | Current inventory, reusable skills, Operations discovery and future content contract |
| [Delivery checklist](DELIVERY.md) | Ordered implementation packages and concrete exit evidence |
| [Acceptance](ACCEPTANCE.md) | Input integrity, full matches, interruptions, languages and performance gates |
| [Mockup](Mockups/aria-watch-play-concept-v01.png) / [prompts](Mockups/PROMPTS.md) | Preserved visual proposal and generation provenance |

## Product outcome

After confirming **Watch ARIA play**, a player can watch ARIA complete the current match using the same touch controls available to them. A cyan holographic finger shows each real gesture, while concise coaching explains the decision or reason for waiting. A persistent **Stop ARIA** returns control immediately. The feature works from the beginning and from normal recoverable mid-match states, in English and conversational Farsi.

Completion means all currently playable Campaign missions, Skirmish configurations and Operations tactical match types are covered by a recorded inventory and validated. **M1 is the first proof, not the release scope.** A player-facing feature advertised as available in all modes cannot be closed with only M1 or one successful Skirmish run.

This is adaptive play with reusable interaction and tactical skills, not a coordinate recording or a script that sets objectives complete. No guaranteed win is promised from an irrecoverable position. ARIA must handle loss and uncertainty honestly and return control with a useful explanation when she cannot proceed.

## Decisions to implement

1. One confirmation authorizes only the current match. No automatic replay, next mission, account purchases or permanent Operations decisions.
2. All game actions pass through simulated touchscreen input. The hologram and input share one gesture timeline; there is no direct-command fallback.
3. A read-only mirror of player-visible information supplies perception. It includes displayed instructions and public goal semantics, not hidden targets or mission facts. Text and semantics must agree in both languages.
4. A local, bounded planner observes, chooses a skill, gestures, verifies and replans. The first release works offline; it needs no model account or cloud calls. Arbitrary natural-language missions require supported goal semantics and skills.
5. A genuine player touch cancels ARIA and is consumed for handover; the next touch operates the game. Stop cancels pending input, not orders already accepted by the game.
6. Existing mission simulation, difficulty, resource costs, clocks, tutorials and victory rules remain authoritative. Ordinary orders continue to execute autonomously as they do for a human player.
7. Player and developer QA use the same gesture executor. Diagnostic access to ground truth never feeds player-mode decisions.
8. The current selection/command interface remains the interface ARIA demonstrates. Do not restore the removed tactical Stop button, the hidden Do It path, or a special radar button to simplify automation.

## Scope and coverage policy

| Scope | Required outcome |
|---|---|
| Campaign | M1–M5, first play/replay, tutorial on/off where supported, mid-match start, success/loss and story transitions |
| Current Skirmish | Base Assault, every supported setup variant, economy/recruitment/construction, defense/attack and all terminal outcomes |
| Operations | Enumerate real tactical launch routes and integrate every playable match type. Dashboard actions alone are not tactical matches. Missing gameplay is a dependency, not an ARIA pass. |
| Future missions and expanded Skirmish | Same public goal and capability contract; new content requires ARIA coverage in its own release gate |
| Developer QA | Normal-speed touch traces, screenshots/video and independent outcome evidence; clearly separate scenario setup from match play |

The feature starts inside a running match. Briefing and post-match screens retain player control; ARIA never starts another match. A script-controlled cutscene may finish normally while ARIA waits without input. A blocking story choice returns control. District management, End Day, permanent spending and unattended multi-match progression are separate future products.

Availability uses a capability manifest for the exact mode/ruleset/content version. During development, incomplete rows remain visibly unsupported in QA and unavailable to players. At release, every playable row must pass or remain a named release blocker; hiding a row is not completion. Future not-yet-playable content can remain planned, with an explicit dependency and no support claim.

## Delivery order

| Gate | Outcome |
|---|---|
| A0 | Complete match/route inventory, input-path audit and source baseline |
| A1 | Real touch parity, cancellation and exclusive input ownership proven |
| A2 | Player-visible observations, public goals, capability/version checks |
| A3 | Start/Stop UI, hologram, teaching and bounded reusable skills |
| A4 | Full M1 proof, then M2–M5 completion and recovery |
| A5 | Current Skirmish and every discovered playable Operations match |
| A6 | Cross-mode certification, device evidence and player comprehension |
| A7 | Player rollout, future-content gates and maintainable QA tooling |

See [DELIVERY.md](DELIVERY.md) for dependencies and checklists. A0–A4 foundation and guided campaign validation are underway; see the current status for evidence. The existing [Skirmish expansion](../Skirmish_Expansion/PLAN.md) remains planned; its new mechanics add ARIA skills and certification alongside their implementation rather than waiting for another autoplay rewrite.

## Success and limits

Release evidence must demonstrate real world outcomes, not only accepted taps. It must include changed camera/layout conditions, wrong/rejected actions, realistic waits, overlapping units, resource shortages, player takeover and complete result flow. Stop correctness is mandatory even when the planner fails.

Coverage completeness and victory rate are different measurements. All advertised matches need complete behavioral coverage; stochastic Skirmish additionally needs reported success rates across a pinned sample of seeds. Human pacing, unchanged difficulty and fair information access take precedence over inflating the win rate.

Remaining uncertainties are implementation work: physical/synthetic pointer arbitration; existing UI cancellation behavior; visible world-object observation and occlusion; Operations launch readiness; coach voice inventory; phone performance. A0 records findings and owners. Unknowns cannot become silent shortcuts.

## Progression, monetization and persistence

Record a non-punitive assisted-run marker in result metadata. Preserve existing rewards and result settlement; no duplicate rewards, automatic replay or retroactive penalty. Permanent profile actions remain outside session permission. Persist coaching preferences, not live ownership, pending touches or the planner's command queue. Loading a match returns in manual control and requires a fresh confirmation.

Keep the initial teaching experience accessible. Extended coaching, strategy lessons and replay analysis are future monetization candidates. Entitlements, pricing and cloud services are not dependencies of the first implementation. Stopping and taking control are always available, with no payment or extra confirmation.

## Evidence and change discipline

Code/asset changes use the repository's ECS, assembly, naming, input and performance contracts. Unity assets are edited through the supported Editor workflow. Unity validation follows [AGENTS.md](../../../AGENTS.md), including the macOS wrapper/GUI-licensing rules. Preserve user saves and unrelated work; use isolated QA profiles.

Document a stable build's code/config hashes, platform, language, route, ruleset, seed, input origin, start state, expected/actual outcome and evidence. Mark each package implemented, Editor-verified, device-verified and player-reviewed separately. No runtime tests were performed for this documentation task, and no gameplay implementation is claimed by this plan.
