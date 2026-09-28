# CH04-M02 Steel Push — Editor readiness evidence

Status: **ready for Unity Editor player acceptance**. Latest three-tank candidate
passed complete English and Persian normal-input automated journeys on `main`.
Representative native screens have been visually reviewed. Human acceptance,
packaged-player and physical-device gates are not claimed.

## Scope

Story authority: Campaign high-level mission catalog, CH04 detailed design, and
narrative sequence/comic catalog. Three tanks, an APC and four infantry defend a
finite physical Fuel reserve against two Vanguard armor groups. Military movement
uses 120 usable barrels; 40 remain protected for emergency civilian services.
Normal selection, Move, Attack, Hold, barracks production and ARIA Play/Stop are
reused. Show Me moves the camera without issuing troop orders.

Existing briefing, comms and debrief PNG pixels are unchanged. Seven dialogue
states have distinct authored 16:9 and 20:9 crops. English and Persian story/UI
copy is written. On 2026-09-28 the user approved generation and validation.
Fourteen ElevenLabs clips (seven English and seven Persian) were generated with
the established cast, saved locally and bound to the authored dialogue states.
All fourteen clips played fully in the two in-mission runs; there is no runtime
network TTS.

## Evidence collected

- `/private/tmp/warline-steel-push-input-fa-01.log`: wrapper exit 0;
  `[SteelPushInput] result=Passed locale=fa-IR dialogue=7 armorCombat=live fuel=spent
  ARIA-actions=5 victory=result=settlement=return`. Actual fight stopped 5/5 enemies
  at 92.895 seconds; no friendly losses and the Fuel site stayed intact (3/3 stars).
  All seven local Persian clips played fully. This is an automated normal-input
  Editor journey at 2400×1080, not a human/device acceptance run.
- `/private/tmp/warline-steel-push-input-en-13.log`: wrapper exit 0;
  `[SteelPushInput] result=Passed locale=en dialogue=7 armorCombat=live fuel=spent
  ARIA-actions=5 victory=result=settlement=return`. Actual fight stopped 5/5 enemies
  at 93.887 seconds; no friendly losses and the Fuel site stayed intact (3/3 stars).
  All seven local English clips played fully; no injected health, position or outcome.
  This run rebuilt and passed the latest checkpoint before play and captured the
  wide Persian briefing as a separate visual check.
- `/private/tmp/warline-steel-push-build-04.log`: wrapper exit 0 and required
  `[SteelPushCheckpoint] result=Passed` marker. Latest native depot registration,
  ground-only guidance, previous radar-contract regression and finite Fuel rules pass.
  `[SteelPushMedia] result=Passed comics=3 dialogueCrops=7 voices=14 mode=voiced`.
- `/private/tmp/warline-steel-push-build-05.log`: wrapper exit 0 and required
  checkpoint/rule/media markers after finite Fuel scoping and counterforce spacing.
  Rule checks explicitly add a 5,000-barrel ambient store: only the mission reserve
  is spent, and its 40-barrel protected floor survives an oversized drain request.
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
  art contains no baked duplicate button. Representative final native comics,
  HUD, result and Campaign return were also reviewed; see `VisualReview/review.md`.
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
  force-quit, licensing reset or IPC cleanup was performed. The user subsequently
  closed the Editor before wrapper validation resumed.
- Input en-08: test asserted mission availability before the replacement save's
  native projection refreshed. A bounded wait was added without injecting availability.
- Input en-09: real HUD ARIA Play was hidden because the new mission was missing
  from the capability allow-list; added to the existing guided Campaign capability.
- Input en-10: real combat and comms played, but inherited demo Fuel storage paid
  movement instead of the mission reserve. Spending, move affordability, Fuel hold
  and HUD now share the authored mission-reserve scope. The supporting tank starts
  nearer the blocking position, and loss of the front tank no longer clears valid
  hold guidance for the surviving counterforce. This run failed and is not readiness.
- Input en-11 was attempted before the focused Editor fully finished shutdown;
  it produced no input pass marker and is not accepted. En-12 starts only after
  the focused wrapper reports exit 0.
- Input en-12 spent the real finite reserve and played comms but lost the second
  armor fight with two starting tanks. Starting counterforce was tuned to three
  canonical tanks without changing unit stats, enemies, Fuel or failure rules.
  Briefing/tutorial copy matches the actual force; story/voice payload is unchanged.

## Completed Editor gates and remaining acceptance

Both complete normal-input automated journeys passed actual armor combat and Fuel
drain, all seven comic states, ARIA, victory, settlement and Campaign return.
Fourteen bilingual voice clips passed full playback without early cutoff.
Representative native-screen review is recorded separately from those checks.
One Persian intro capture caught early typewriter reveal, so that image is not
evidence of complete caption fit; completed Persian ARIA and Samira debrief captions
were inspected. No all-frame or every-line screenshot acceptance is claimed.

User visual acceptance, real-player acceptance, packaged-player build and physical
device testing are separate gates and have not been performed. Compilation and
isolated fixture passes do not establish readiness. Unrelated concurrent SupportSystem
report files are preserved and are not part of this mission's evidence.
