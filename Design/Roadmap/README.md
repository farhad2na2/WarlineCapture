# Warline development roadmap

Owner: project owner with Codex implementation and validation. Updated: 2026-09-17.
Status: milestone 1 internal gameplay/recovery checks complete; Android candidate built and verified; device/player review remains. This roadmap orders future work; it does not authorize starting every milestone now.

## Product direction

Build a readable, responsive mobile strategy game where players can understand an objective, make a tactical decision, issue it confidently, and see the result. Preserve the existing visual language and conversational English/Farsi campaign presentation. Expand content after the shared gameplay earns player trust.

| Order | Milestone | Deliverable | Exit decision |
|---|---|---|---|
| 1 — active | Reliable M1–M5 and essential mobile UI/UX | Candidate build with five understandable, completable missions in English and Farsi | No known progression blockers or misleading required actions; normal player journeys and recovery checks pass; device and independent-player review remain explicit gates |
| 2 | Small skirmish prototype | One map, existing roster, simple enemy strategy and clear win/loss rules | Players can enjoy selection, movement, building and combat without a scripted tutorial; use feedback to decide further investment |
| 3 | Basic progression and rewards | Clear results, stars, unlocks, replay value and reliable persistence | Rewards are understandable, earned once, saved correctly and encourage a meaningful next choice |
| 4 | More missions and chapters | Missions that extend proven mechanics through varied tactical decisions | Each mission passes the same readability, gameplay, localization and device acceptance process |
| 5 | Additional modes | A narrowly scoped prototype of the most valuable distinct mode | Its player purpose and production cost justify keeping it; avoid modes that duplicate existing play |
| 6 | Decorative presentation polish | Refined transitions, reward presentation and optional visual flourishes | Improves perceived quality without hiding information, slowing frequent actions or reducing device performance |

Functional feedback belongs in milestone 1: selection acknowledgment, accepted/rejected commands, progress/wait states, damage/destruction, placement confirmation and restrained guidance animation. Decorative panel motion can wait.

## Active milestone

See [M1–M5 detailed plan](M01_M05_Readiness/PLAN.md), [verification matrix](M01_M05_Readiness/VERIFICATION.md) and [findings / fixes](M01_M05_Readiness/FINDINGS.md).

Sequence: establish evidence and shared interaction contracts → play through M1 and M2 → M3 defense → M4 rescue → M5 breach → repeat both-language journeys and recovery checks → target-device review → unfamiliar-player review. Fix shared problems in their owning system and rerun affected missions. Avoid replacing established mechanics merely to simplify a test.

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
