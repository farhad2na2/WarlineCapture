# Warline development roadmap

Owner: project owner with Codex implementation and validation. Updated: 2026-09-21.
Status: M1–M5 internal baseline and the two small Skirmish Base Assaults have EN/FA Editor evidence. ARIA has guided campaign and small-Skirmish baseline wins, with broader release gates open. The owner clarified the expanded Skirmish target: **120 distinct selectable battles across five maps, expandable to 200**, alongside full unit roles and larger armies. Detailed scenario, map, difficulty, starting-package and AI/ARIA plans are documented; they are not implemented content. The separate [Operations plan](Operations/PLAN.md) now proposes **60 authored tactical missions across six district arcs**, with per-mission implementation briefs and mandatory ARIA wins. Operations remains planned, not implemented. Device/player acceptance remains separate.

## Product direction

Build a readable, responsive mobile strategy game where players can understand an objective, make a tactical decision, issue it confidently, and see the result. Preserve the existing visual language and conversational English/Farsi campaign presentation. Expand content after the shared gameplay earns player trust.

| Order | Milestone | Deliverable | Exit decision |
|---|---|---|---|
| 1 — internally complete | Reliable M1–M5 and essential mobile UI/UX | Five internally verified missions in English and Farsi, with story/replay and recovery fixes | Internal checks passed; latest APK rebuild, device and independent-player review remain separate release gates |
| 2A — internally playable | Small skirmish baseline | Two selectable Base Assaults, restricted roster and one AI opponent | EN/FA autonomous Editor wins recorded; known CC floating-terrain defect and wider device/visual acceptance remain open |
| 2A.1 — baseline implemented | Watch ARIA play | Visible touch input and handback; guided M1–M5 and both small Skirmish baseline wins | Keep broader seed/device/recovery gates explicit; extend reusable skills with every expanded Skirmish mechanic |
| 2B — detailed plan | Full Skirmish battle library | 120 distinct scenarios on five maps; four objectives, three army profiles, two starts; four difficulty levels; 116/224/340 combat-unit size targets; shared enemy strategy and touch-only ARIA | Per-entry gameplay/AI/ARIA evidence, complete roster roles, EN/FA/mobile clarity, campaign protection and device-certified sizes; expand to 200 only after new objective/profile validation |
| Separate mode — detailed proposal | Operations city stabilization | 60 authored missions, six district arcs, 12 shared mission families, persistent district/day state and six abstract actions | Every released mission has a real ARIA win; complete city-run ARIA win, manual review, consequence/recovery correctness, EN/FA and device evidence |
| 3 | Basic progression and rewards | Clear results, stars, unlocks, replay value and reliable persistence | Rewards are understandable, earned once, saved correctly and encourage a meaningful next choice |
| 4 | More missions and chapters | Missions that extend proven mechanics through varied tactical decisions | Each mission passes the same readability, gameplay, localization and device acceptance process |
| 5 | Additional modes | A narrowly scoped prototype of the most valuable distinct mode | Its player purpose and production cost justify keeping it; avoid modes that duplicate existing play |
| 6 | Decorative presentation polish | Refined transitions, reward presentation and optional visual flourishes | Improves perceived quality without hiding information, slowing frequent actions or reducing device performance |

Functional feedback belongs in milestone 1: selection acknowledgment, accepted/rejected commands, progress/wait states, damage/destruction, placement confirmation and restrained guidance animation. Decorative panel motion can wait.

## Next milestone

Implement the expanded Skirmish in reviewable slices under [PLAN.md](Skirmish_Expansion/PLAN.md) and [DELIVERY.md](Skirmish_Expansion/DELIVERY.md). The requested catalog is specified in [120 battles and path to 200](Skirmish_Expansion/BATTLE_CATALOG.md), with [all 120 planned IDs](Skirmish_Expansion/SCENARIO_CATALOG.csv), [five map briefs](Skirmish_Expansion/MAPS.md), [starting packages and difficulty](Skirmish_Expansion/MATCH_SETUP.md), and [enemy AI/ARIA requirements](Skirmish_Expansion/AI_AND_ARIA.md).

The September 21 [Skirmish 4–120 programming handoff](Skirmish_Expansion/IMPLEMENTATION_HANDOFF.md) adds exact shared classes/asset contracts, fourteen implementation packages, [twenty packets with all 120 individual briefs](Skirmish_Expansion/Scenarios/README.md), a [117-entry remaining work list](Skirmish_Expansion/WORK_QUEUE_004_120.csv) and 360 resolved starting packages. Existing prototypes map to S001/S025/S073, so work ordinals are explicitly separate from stable catalog IDs. Every expanded entry requires normal manual and ARIA victories before publication; this is planned work, not newly accepted gameplay.

First correct known defects, especially the floating terrain near Skirmish 2's base; audit every roster role/producer; establish scale and economy measurements. Prove an expanded ground battle before completing air/intel, larger armies and new objectives. Then validate batches of 24 scenarios per map toward 120. Difficulty, seeds and battle sizes do not inflate the scenario count. A completed game-winning script does not certify visual quality, mobile usability or a whole catalog.

Preserve [current ARIA evidence](../AgentReports/AriaWatchPlay/implementation-status.md) and [coverage](Aria_Demonstration/COVERAGE.md). ARIA expansion must use shared objective/route/role reasoning and visible input, not 120 scripted solutions. Its [architecture and ownership rules](Aria_Demonstration/PLAN.md) remain in force. Current mission-specific guidance and S1/S2 tactical exceptions are migration inputs, not proof of a universal planner.

The accepted [unlock and upgrade direction](Skirmish_Expansion/UNLOCKS_AND_UPGRADES.md) is refined by exact proposed packages in MATCH_SETUP.md: no campaign grind for Skirmish catalog access, match readiness/facilities determine recruitment, category upgrades reset on a fresh match, and difficulty uses fair resources/stats. Numbers are initial balance inputs requiring validation. Full Arsenal Sandbox is separate from the fixed catalog.

**Operations planning is documented in [Operations/PLAN.md](Operations/PLAN.md).** The recommendation is 60 authored missions in six ten-mission district arcs, implemented with 12 shared families. The package includes [all mission briefs](Operations/Missions/README.md), [technical architecture](Operations/ARCHITECTURE.md), [strategic rules](Operations/STRATEGIC_RULES.md), [coding packages](Operations/DELIVERY.md), and [acceptance requiring ARIA to play and win every mission and a complete city run](Operations/ACCEPTANCE.md). Deliver a three-mission loop, then 12, 30 and 60 published entries. This is a documentation handoff, not implementation or proof that dashboard actions already launch complete matches. Operations completion is not a dependency of the 120 Skirmish battles; their implementations remain separate workstreams.

Retain the [small prototype completion report](Skirmish_Prototype/COMPLETION.md) and [two-scenario evidence](../AgentReports/AriaWatchPlay/skirmish-two-scenario-delivery.md) as historical scoped results. No large-battle or 120-scenario readiness is claimed from them. Acceptance is defined in [the expanded matrix](Skirmish_Expansion/ACCEPTANCE.md).

**The next Campaign mission is planned as [Chapter 2, Mission 1: Gridlock](../CH02_M01_Gridlock_Production_Plan.md).** Restore the hospital relief route while defending Fadi's road crews, using the catalog's authored route-clearing variant. The plan includes the M5 → Chapter 2 transition, real vehicle traversal, EN/FA story and guidance, and six mandatory normal-speed ARIA wins covering guided play, guidance-disabled play and recovery. It is planned content; no Gridlock implementation or gameplay acceptance is claimed.

The closed internal campaign milestone is documented in [M1–M5 completion](M01_M05_Readiness/COMPLETION.md), [verification](M01_M05_Readiness/VERIFICATION.md), [findings](M01_M05_Readiness/FINDINGS.md) and the [M1 story replay follow-up](../AgentReports/M01StoryReplay/README.md). Reopen it for concrete defects or player feedback. Fix shared problems in their owning system and rerun affected missions; preserve the accepted experience while building skirmish.

## Definition of a readiness decision

A test that clicks a button is not enough. Verify the command's world result and the visible state afterward. Separate source inspection, gameplay command simulation, visible UI interaction, rendered review, audio review and real-device evidence. An Editor pass cannot close a device or human-enjoyment gate.

Keep a single issue register with reproduction, expected behavior, narrow fix, impacted missions, regression evidence and remaining limitations. Historical reports are useful context, not proof of the current build. Do not promise dates until the initial mission pass establishes the remaining scope.

## Scope boundaries

Preserve completed work and user saves. Use isolated QA profiles. No automatic changes to economy, mission outcomes, enemy health, player positions or tutorial facts to manufacture success. Test setup may select a mission in an isolated profile; disclose shortcuts such as skipped comics or accelerated simulation. Story/voice revisions must remain synchronized in localization assets; new paid audio generation is handled under the applicable explicit authorization.

## Design references

These references inform the roadmap; project-specific acceptance comes from the user's requirements and observed gameplay.

- [Apple: onboarding for games](https://developer.apple.com/app-store/onboarding-for-games/) — teach actions needed for early objectives and defer unnecessary concepts.
- [Android: user experience quality](https://developer.android.com/quality/user-experience) — audience-appropriate onboarding and usable layouts across devices.
- [Android: technical quality](https://developer.android.com/quality/technical) — performance and reliability are part of the player experience.
