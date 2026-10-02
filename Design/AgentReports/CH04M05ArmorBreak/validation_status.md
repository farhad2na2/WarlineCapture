# Armor Break validation status

Snapshot 2026-10-02. Resumed at the user request. Implementation is saved. English and Persian captioned normal-input journeys passed; voices remain pending; this is the captioned review checkpoint.

## Passed native gates

- `build-wrapper-08`: saved map, scenario, mission, narrative, presentation, catalogs, Support context and Addressables registration; receipt 0.
- `map-wrapper-01`: 5,327 identities, 667 building owners, 4,660 render-only identities, three roots, 1280 × 520 crop, six unchanged source hashes, independent loader, rebased footprints and qualified routes; receipt 0.
- `objectives-wrapper-01`: actual objective writer, 12 original military targets, independent authority custody and protected relief; receipt 0.
- `rules-wrapper-04`: normal helicopter Attack observer, first-shot proof, stale attempt rejection, owned finite Fuel and protected floor, and mission rule guards; receipt 0. These fixtures do not establish player readiness.

- `packed-wrapper-04`: native Entities and Addressables catalogs produced, receipt 0 and no Burst errors. Build scope is content only; old hashed outputs remain in the isolated build cache.

## Retained failed evidence

- `compile-01/02`: missing facade and ReadOnlySpan conversion; fixed.
- `build-wrapper-01/02/03`: building art pivot, material recipe suffix and merged walkable surface matching; fixed.
- `build-wrapper-04/05`: source world bounds were not rebased; fixed.
- `build-wrapper-06`: relief used an unsupported neutral role; changed to the existing protected civilian role.
- `build-wrapper-07`: new objective rules were absent from the shared validator; fixed.
- `rules-wrapper-01`: fixture lacked launch identity fields; fixed.
- `rules-wrapper-03`: observer fixture copied a stale attempt from the preceding Fuel check; fixture isolated, production guard preserved.
- `packed-wrapper-01/02/03`: content produced and receipts 0, but Burst compilation errors make these incomplete packing evidence. Guidance FixedString references were corrected; `packed-wrapper-04` is the clean initial rerun. Final packing after runtime revisions remains pending.

- `input-en-captioned-wrapper-01`: normal campaign and briefing/comics loaded, but the shared spawn restriction guard rejected Armor Break with aircraft and transport enabled. The probe was stopped as failed; a mission-specific guard is corrected and the full journey must be rerun.

- `input-en-captioned-wrapper-02`: roster spawned and normal Campaign/comics opened, but autonomous combat destroyed a required mastery target at stage 1. Retained as failed. Combat staging and opening Fuel use are being corrected; no readiness claim.

- `input-en-captioned-wrapper-03`: opening Fuel stayed at 160 usable barrels and original mastery targets survived. The hostile jet reached the parked helicopter before air-defense coverage, causing AircraftLost. Failed evidence retained. Air activation now requires actual coverage; the approach route is redirected north of the helicopter apron.

- `input-en-captioned-wrapper-04`: bootstrap correctly rejected the stale catalog content hash after the air-route edit. Catalogs refreshed through `integration-wrapper-10` (receipt 0, all integration markers Passed).

- `input-en-captioned-wrapper-05`: real coverage/interception, launcher shot and ordered helicopter shot passed; the inherited base-assault armor patrol killed parked recovery infantry before normal armor deployment. Failed evidence retained. Patrol now defends its own military command sector.

- `input-en-captioned-wrapper-06`: normal armor Move reached the approach, but remaining enemy tanks and command infantry killed the armored group before guided Attack. Actual weapon statistics showed the sequence advanced after a single helicopter shot while defending armor remained. The helicopter stage now requires clearing the three original armored defenders before the ground assault, preserving real damage/health and normal controls. Failed evidence retained.

- `input-en-captioned-wrapper-07`: helicopter was directed into hostile tank range during its first Attack. Failed evidence retained. Guidance now moves to a weapon-range-based standoff instead of 25 metres from the target, and requires being inside the real firing range before Attack.

- `input-en-captioned-wrapper-08`: firing-distance rules passed, but normal helicopter Move returned to its apron before the next Attack, repeatedly consuming Fuel. Probe stopped as failed with input restored and Editor kept open. Supplied helicopter now holds the completed Move position for the next command; explicit and Fuel-safety returns remain supported.

- `input-en-captioned-wrapper-10`: normal-input combat/recovery victory observed at 354,968 ms with all original requirements complete, relief alive and 92.197 usable Fuel. Full journey failed: shared result projection used generic base-defense facts and produced no settlement/result component, so debrief did not start. Probe stopped as failed. Own result qualification is being added; this candidate win does not establish final readiness.

## Screen review and focused rules

- English and Persian briefing, mission HUD/timer, comics and result screens reviewed separately from the normal-input journey verdict. Existing controls and approved bracket/pin markers are retained; no new gameplay command buttons.
- ARIA alternate-loss guidance now prefers surviving armed armor and uses the qualified helicopter if only an unarmed APC survives. Focused checks cover actor preference, helicopter Move/Attack cues, group selection, APC approach and dead-helicopter rejection; passed during the ARIA probe preflight. This fixture is not an alternate-loss normal-input playthrough.

## Passed normal-input journeys and final packing

- `packed-wrapper-05`: final Entities and Addressables content passed after the alternate-loss guidance fix, receipt 0, wrapper exit 0, no Burst/compile errors. Accumulated hashed build output is 277,845,389 bytes; this is content-cache size, not runtime memory.
- English ARIA normal-input journey passed in `input-en-aria-captioned-wrapper-01`: existing Play confirmation, all combat/recovery requirements, 14 panels, Chapter IV close, victory, settlement and Campaign return. Receipt 0 and wrapper exit 0. Stop ARIA remained visible; this run did not manually press Stop during combat.
- Persian normal-input journey passed in `input-fa-captioned-wrapper-01`: all 14 panels, Chapter IV close, victory, settlement and Campaign return; receipt 0 and wrapper exit 0. Native briefing, mission timer/HUD and result screen reviewed. Captioned only.
- English normal-input journey passed in `input-en-captioned-wrapper-11`: all 14 comic panels, Chapter IV close, actual victory, 3 stars, 2,500 XP / 11,000 credits settlement and return to Campaign. Receipt 0 and wrapper exit 0. This is native Editor evidence; voices and human/device acceptance remain pending.
## Pending gates

- 28 English/Persian ElevenLabs clips. Automatic approval review rejected the upload; the question naming the exact payload and provider remains pending. No external generation occurred. Captioned gameplay evidence will not be reported as voice acceptance.
- Real player review and production player/device acceptance.

## User pause

`input-en-captioned-wrapper-09` was stopped at the user request during normal launcher-stage validation. It is incomplete evidence, not a pass. Input was restored and the task Play run exited; Unity Editor and Hub remain open. Source changes remain in the primary checkout for continuation.

Final `map-wrapper-02` passed with receipt/wrapper exit 0 and six unchanged source hashes.
