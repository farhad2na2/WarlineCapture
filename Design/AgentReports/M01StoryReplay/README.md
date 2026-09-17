# M1 story replay and setup simplification — 2026-09-17

M1 campaign starts and retries now play the existing first-launch opening comic in the current saved language, regardless of completed campaign progress. The replay omits language and commander setup and uses the saved commander identity. Initial onboarding still chooses a language and commander, then hands off to M1 without playing the opening twice.

The unused ARIA guidance-level choice has been removed from the authored sequence, shipping prefab and both prefab builders. Commander setup now displays step 2/2. Legacy serialized guidance values remain compatible; normal default guidance remains available in gameplay. Migration preserves existing story art, voice clips and timings.

M1 stays in its briefing phase until the story completes or its existing Skip confirmation is accepted. It then releases the narrative pause and starts the interactive lesson once. M2–M5 narrative and result policies are unchanged. Fresh campaign comic players now restore the saved commander before the first line is presented; first-launch playback also restores the identity before starting and captures any new portrait chosen during setup. Portrait and commander voice selection use the same resolved option, eliminating the faceless fallback on replays.

## Validation

- Focused replay/onboarding/state-graph tests passed (4 cases).
- 45 focused Editor regression tests passed, including real commander portrait-button/Continue commits, legacy saved-avatar clamping, startup gating and adjacent mission narrative policy.
- Full completed-profile English and Farsi M1 story runs passed: all 17 recordings per language completed, the chosen `commander_01` portrait appeared with the matching localized voice, no setup screens reopened, and the first gameplay lesson advanced once.
- Visual inspection includes the commander dialogue in both languages, the Farsi comic with localized controls and subtitles, and both gameplay handoffs. See [English portrait](Evidence/english-commander.png), [Farsi portrait](Evidence/farsi-commander.png) and [regression results](Evidence/regression.xml).
- Tests use an isolated Unity project and campaign progress store. The saved commander is read without changing the player’s identity; player progress is not reset.

This report covers source and Editor validation. The earlier ReadinessCandidate APK predates this change.
