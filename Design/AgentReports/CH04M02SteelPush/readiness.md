# CH04-M02 Steel Push — implementation and pending readiness

Status: **not player-ready yet**. Source implementation is in progress on `main`;
the latest depot variant and voiced scenario have been generated; complete
normal-input playthroughs and final native visual review are still pending.

## Scope

Story authority: Campaign high-level mission catalog, CH04 detailed design, and
narrative sequence/comic catalog. Two tanks, an APC and four infantry defend a
finite physical Fuel reserve against two Vanguard armor groups. Military movement
uses 120 usable barrels; 40 remain protected for emergency civilian services.
Normal selection, Move, Attack, Hold, barracks production and ARIA Play/Stop are
reused. Show Me moves the camera without issuing troop orders.

Existing briefing, comms and debrief PNG pixels are unchanged. Seven dialogue
states have distinct authored 16:9 and 20:9 crops. English and Persian story/UI
copy is written. On 2026-09-28 the user approved generation and validation.
Fourteen ElevenLabs clips (seven English and seven Persian) were generated with
the established cast, saved locally and bound to the authored dialogue states.
Full in-mission playback is still pending; there is no runtime network TTS.

## Evidence collected

- `/private/tmp/warline-steel-push-build-04.log`: wrapper exit 0 and required
  `[SteelPushCheckpoint] result=Passed` marker. Latest native depot registration,
  ground-only guidance, previous radar-contract regression and finite Fuel rules pass.
  `[SteelPushMedia] result=Passed comics=3 dialogueCrops=7 voices=14 mode=voiced`.
- Voice generation completed with
  `[SteelPushBilingualVoiceGeneration] result=Passed clips=14 narrative=7 locales=2 runtimeNetworkTts=0`;
  payload hashes and local file metadata are recorded in `steel_push_voice_manifest.json`.
- `/private/tmp/warline-steel-push-build-03.log`: wrapper exit 0;
  `[SteelPushCheckpoint] result=Passed dialogue=7 locales=en,fa-IR
  fuel=physical-finite controls=existing-only`.
- Isolated Fuel binding/spending/floor, building damage/destruction, deadline and
  mission progression checks passed in that candidate. These are rule fixtures,
  not a successful normal-input mission.
- Campaign comic-coverage validation passed: 37 sequences, 87 dialogue states,
  both supported aspects. Seven new dialogue states have valid comic crops.
- Native English briefing and Persian wide briefing were captured and inspected
  in `/private/tmp/warline-steel-push-input/`. One native Deploy control is present;
  art contains no baked duplicate button. Final whole-mission visual review is pending.
- An old Air Corridor clear unlocked the new mission during the isolated normal
  launch. Campaign availability/completion masks were expanded from 16 to 32 bits.

## Failed evidence and source corrections

Full logs remain under `/private/tmp/warline-steel-push-*`.

- Build 01: planning camera exceeded permitted map height; corrected.
- Build 02: inherited radar-required contract did not support a ground-only defense;
  explicit zero-charge/no-sensor support and four-step guidance were added.
- Input en-01: probe inspected the defense-member buffer before launch created it;
  a startup guard was added.
- Input en-02 through en-05: preparation failed before combat. Diagnostics showed
  the depot was rejected by native placement policy. The initial producer also
  relocated close to the reserve footprint, so its bay was separated and the fixed
  reserve is requested first. Allowing relocation alone did not solve the rejection:
  the depot was absent from the mission construction catalogue.
- Input en-06: adding the existing `SupplyLine_Reserve_Depot` name exposed the
  generic `Building_` ID requirement. Source now generates a mission-specific
  `Building_SteelPush_ReserveDepot` variant using the same qualified geometry,
  registers it and grants its single native catalogue slot. Build 04 validates the
  generated registration; live placement remains part of the input playthrough.
- Input en-07 could not establish a fresh run while the failed en-06 Editor remained
  open. It is not passing evidence.
- The safety review rejected remotely requesting that Editor's exit. No retry,
  force-quit, licensing reset or IPC cleanup was performed. User closure is requested.

## Pending gates

1. Complete normal-input English and Persian journeys: actual armor combat and Fuel
   drain, all seven comic states, ARIA, victory, settlement and Campaign return.
2. Review final native Campaign, briefing, comics, HUD and result/return captures.
3. Verify full bilingual voice playback without early cutoff.

User visual acceptance, real-player acceptance, packaged-player build and physical
device testing are separate gates and have not been performed. Compilation and
isolated fixture passes do not establish readiness. Unrelated concurrent SupportSystem
report files are preserved and are not part of this mission's evidence.
