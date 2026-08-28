# M02EB-031 Bilingual Copy Acceptance

Date: 2026-08-29
Status: Accepted

## Scope

`M02EstablishBaseLocalizedText` is the single source for:

- 9 English narrative lines and 9 one-to-one Persian lines across brief, comms, and debrief.
- 7 English tutorial instructions and 7 one-to-one Persian instructions for steps 2 through 8.
- Exact shared text used by narrative generation, Persian locale generation, tutorial presentation, and voice-manifest validation.

The copy uses short, direct sentences and concrete player language. It explains why the Barracks is required: it restores the abandoned forward post, trains the rifle squad that makes the post operational, and protects the clinic road. The comms beat introduces the stolen municipal-access list without jargon, and the debrief names Dalia Rahim without an artificial pause.

## Validation

- `PersianLocaleMatchesEveryFinalNarrativeLine` passed.
- `BriefEstablishesPostDirectionAndCivicPurpose`, `CommsRecoverPreAttackMunicipalAccessList`, and `DebriefClosesPostDaliaAndM03WarningSectorBeats` passed.
- `TutorialVoiceAssetsMatchEveryDisplayedInstruction` passed.
- All M2 guidance and narrative copy contracts passed in the 246/246 all-M2 run.
