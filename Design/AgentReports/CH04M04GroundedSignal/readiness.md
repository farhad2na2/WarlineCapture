# Grounded Signal readiness

Mission: `saga.ch04.m04.grounded_signal` — Campaign, Chapter IV, mission 4.

Status: ready for user review in the primary project.

## Implemented

Independent airfield map, actual transport unloading, military relay attack, two-specialist hardware recovery, APC boarding and secure ground exit. Existing command controls and approved ground-marker style retained. Three comic sequences contain eight English/Persian lines with sixteen installed voice clips. Mission rewards unlock Paratroopers.

## Evidence gates

- Visual direction: previously approved map/UI direction; user explicitly waived another mockup approval.
- English normal-input manual playthrough: passed in `Evidence/20261001-224822-en-manual/result.txt`; eight full English voice clips, ordinary victory, debrief, rewards and campaign return.
- Focused rules/objective/map reuse checks: passed in `Evidence/english-aria-wrapper-02.log`; isolated automated checks are separate from native input acceptance.
- Final native ARIA / Persian review: passed in `Evidence/20261001-232658-fa-IR-aria/result.txt` and `Evidence/persian-aria-wrapper-02.log`; eight full Persian voice clips, ordinary victory, debrief, rewards and Campaign return. Wrapper exit and receipt both zero. Native Persian briefing, comics, HUD, corrected two-star result and return inspected.
- Packed content: passed final repository wrapper confirmation in `Evidence/packed-wrapper.log`, with explicit pass marker and zero exit/receipt; independent entity scene and fourteen-prefab fixture exported. Ordinary production player/device loading remains separate.
- Human listening, physical device acceptance and a full ordinary production player build: pending. Editor evidence does not establish these gates.

## Retained failed evidence

Earlier input runs exposed map identity/reuse, duplicate asynchronous surface loading, duplicate fence identities, objective publication, probe timing, camera retries, relay approach and a mobile/armed relay. The fixes are recorded in source and failed attempts remain in `Evidence/`.

`english-aria-wrapper.log` failed because its new entry point had not yet compiled. `english-aria-wrapper-02.log` failed after disabling the real relay because the two-specialist stage exposed a single-unit selection cue. The production selection now exposes actual pair drag bounds. `english-aria-wrapper-03.log` exposed the relay approach and attack sharing a single objective watchdog; actual arrival in attack range now advances the public sub-objective. `persian-aria-wrapper.log` exposed a two-second camera retry in pair selection; the Grounded drag cue now uses the same 15-second pan allowance as other Grounded world targets. A subsequent result-screen compile error was corrected before final validation.

No gameplay outcome or troop command was injected to qualify the normal-input mission. Validation uses an isolated campaign progress store and actual InputSystem touch controls; ARIA uses the same visible controls and world gestures.
