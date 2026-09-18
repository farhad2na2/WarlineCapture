# Skirmish recruitment delivery — 2026-09-18

## Problem and fix

The restricted Skirmish recruitment registry contains the rifleman and logistics trucks, but not the transport helicopter. The rifleman relied on finding its delivery carrier in that registry, so recruitment silently skipped the transport presentation.

- Assign the existing transport helicopter explicitly in the rifleman configuration and authoring prefab. The carrier remains excluded from the recruitable Skirmish roster.
- Focus friendly managed helicopter deliveries as their approach starts. Mark enemy deliveries as handled without requesting camera focus, including the later arrival callback.
- Keep the accepted recruitment drawer closure and provide English/Farsi confirmation that a helicopter will deliver four soldiers.
- Add an asset-backed regression test using the actual restricted Skirmish registry, verifying a helicopter resolves while remaining unavailable for direct recruitment.

## Validation

Connected normal Editor, isolated test save, actual visible Build / Soldiers / rifle card / Recruit controls and EventSystem hit testing. No resources, units, transforms, health or time scale were altered to create a successful result. The original scene, save-root environment and Edit mode were restored afterward.

English and Farsi live runs both verified:

1. Successful recruitment closes the build drawer (confirmed closed on the following sample).
2. Camera moves to the drop site; transport helicopter arrives and performs the existing rope-delivery presentation.
3. Player infantry increases from 8 to 9, 10, 11 and 12, one unit per drop.
4. All four drops complete and the helicopter departs.
5. The third squad card is selectable and the selection panel reports four soldiers with 500/500 health. Recruits proceed to the existing base rally point; they do not remain under the departing helicopter.

Focused tests passed:

- RifleRecruitmentRetainsHelicopterDeliveryWithTheRestrictedSkirmishRoster
- CanonicalTentTransportPresentation_ArrivesRopesAndDefersSpawnUntilDropCompletes
- ManagedBarracksTransport_LocksHoverAndPublishesDepartureReadiness
- ManagedBarracksTransport_ReservesOccupiedStaticHelipad

Screenshots and observation logs are adjacent. Enemy camera protection was verified in the code path; a separate live enemy-delivery camera test was not run. This was an Editor check, not an Android device run.

The initial observer used case-sensitive soldier IDs and missed the lowercase IDs of produced units. Another observer accessed the drawer after its expected destruction. Those QA observer errors were corrected before the successful recorded runs; neither was a game failure.

## Follow-up: allow manual camera control during delivery

Manual pan moved the camera while the delivery smooth-focus target remained active, allowing subsequent camera updates to pull it back. Nonzero manual pan/zoom now clears automatic smooth focus and perspective targets. The delivery zoom owner also releases its transition after that focus is cleared; a zero-motion pointer does not cancel the approach animation. The delivery itself continues.

Six focused camera tests passed (camera-interruption-tests.txt), covering pan/release without pullback, zoom interruption and idle input, normal smooth delivery focus, map boundaries, tactical-follow locks, and HUD zoom levels.

Live Skirmish validation used the real recruitment UI followed by mouse input through Unity Input System during delivery phase 1. A 240-pixel drag moved the camera about 12 world units, immediately cleared its smooth-focus target, and showed zero drift back two seconds after release. All four soldiers then arrived (8 → 12) and the helicopter departed normally. See camera-interruption-live.txt and camera-interruption.png. The normal Editor/save context was restored.
