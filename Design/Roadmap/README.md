# Warline development roadmap

Owner: project owner with Codex implementation and validation. Updated: 2026-09-18.
Status: milestone 1 implementation/internal QA complete and committed as `b9d17a700`; milestone 2A Base Assault is ready for internal Editor play in both languages. On 2026-09-18 the owner requested a larger Skirmish plan, then a visible, touch-only ARIA match player before entering expansion implementation. Milestone 2A.1 now documents that feature across Campaign, Skirmish and Operations tactical matches; milestone 2B remains the planned wider roster/hundreds-of-units expansion before progression/rewards. Device/player acceptance and pacing evaluation remain separate. The existing Android candidate predates these changes. This roadmap orders future work; it does not authorize starting every milestone now.

## Product direction

Build a readable, responsive mobile strategy game where players can understand an objective, make a tactical decision, issue it confidently, and see the result. Preserve the existing visual language and conversational English/Farsi campaign presentation. Expand content after the shared gameplay earns player trust.

| Order | Milestone | Deliverable | Exit decision |
|---|---|---|---|
| 1 — internally complete | Reliable M1–M5 and essential mobile UI/UX | Five internally verified missions in English and Farsi, with story/replay and recovery fixes | Internal checks passed; latest APK rebuild, device and independent-player review remain separate release gates |
| 2A — internally playable | Small skirmish prototype | One Base Assault preset, existing map/roster, one AI opponent and clear win/loss rules | Functional Editor checks complete; preserve as a regression baseline and carry open acceptance forward |
| 2A.1 — planned, next | Watch ARIA play | Visible holographic gestures, confirmation and instant Stop; shared touch-only play across every current playable Campaign, Skirmish and Operations tactical match | Complete route inventory, physical/synthetic input parity, full matches, EN/FA clarity, player takeover and separate device/learning evidence |
| 2B — planned | Combined-arms Skirmish expansion | Useful specialist/vehicle/air roster, mobile army groups, scalable economy/AI and roughly 224-unit War target; larger preset after testing | Staged ground/air/large-battle gameplay, player comprehension, campaign regressions and per-device certification; ARIA support for each new mechanic |
| 3 | Basic progression and rewards | Clear results, stars, unlocks, replay value and reliable persistence | Rewards are understandable, earned once, saved correctly and encourage a meaningful next choice |
| 4 | More missions and chapters | Missions that extend proven mechanics through varied tactical decisions | Each mission passes the same readability, gameplay, localization and device acceptance process |
| 5 | Additional modes | A narrowly scoped prototype of the most valuable distinct mode | Its player purpose and production cost justify keeping it; avoid modes that duplicate existing play |
| 6 | Decorative presentation polish | Refined transitions, reward presentation and optional visual flourishes | Improves perceived quality without hiding information, slowing frequent actions or reducing device performance |

Functional feedback belongs in milestone 1: selection acknowledgment, accepted/rejected commands, progress/wait states, damage/destruction, placement confirmation and restrained guidance animation. Decorative panel motion can wait.

## Next milestone

Follow the [Watch ARIA play implementation plan](Aria_Demonstration/PLAN.md), [mockup and interaction rules](Aria_Demonstration/UX.md), [all-mode coverage](Aria_Demonstration/COVERAGE.md), [delivery checklist](Aria_Demonstration/DELIVERY.md) and [acceptance gates](Aria_Demonstration/ACCEPTANCE.md). First inventory every playable route and prove real touch/cancellation. M1 is the first validation slice; completion includes M2–M5, current Skirmish and all playable Operations tactical matches. The owner requested documentation/planning; runtime implementation has not started. Operations UI actions are not proof of a playable tactical mode, and missing gameplay remains an explicit dependency.

Then follow the [Skirmish expansion plan](Skirmish_Expansion/PLAN.md), [source baseline](Skirmish_Expansion/BASELINE.md), [delivery packages](Skirmish_Expansion/DELIVERY.md) and [acceptance gates](Skirmish_Expansion/ACCEPTANCE.md). Start with the current defect/roster/performance baseline, then a roughly 100-unit ground battle with useful unit roles and manageable groups. Scale toward 200-plus units with air/recon and stronger multi-front play; expose larger settings only after device validation. New groups, upgrades, air and objective mechanics require matching ARIA skills and certification. The expansion is planned, not implemented.

The accepted [unit unlock and upgrade direction](Skirmish_Expansion/UNLOCKS_AND_UPGRADES.md) uses three match readiness stages, facility requirements and capped shared upgrades. Permanent blueprint progression belongs to Campaign/Operations later. This remains documented direction; expansion implementation has not started.

Retain the [small prototype completion report](Skirmish_Prototype/COMPLETION.md), [original plan](Skirmish_Prototype/PLAN.md) and [acceptance matrix](Skirmish_Prototype/ACCEPTANCE.md) as historical scope and evidence. Its original 8–12-minute pacing target was not demonstrated. Unfamiliar-player and device testing are included in the expansion gates rather than marked complete. Progression/rewards and full Operations development follow a proven combined-arms loop.

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
