# CH04-M02 Steel Push — production plan

Authority: `Campaign_Mission_High_Level_Design_Catalog.md`, Chapter 4 detailed design,
and `Campaign_Narrative_Sequence_And_Comic_Catalog.md`. Continues Air Corridor;
does not repeat its automatic air-defense encounter.

## Contract

Stop a regular Vanguard armored column before it captures the emergency Fuel
reserve or reaches the dormant Relay node. Dalia names the open military assault;
Samira explains the hospital and water-pump dependency. A disabled command vehicle
yields an order naming the final authority key. Debrief points to Split Front's battery.

## Gameplay direction

Reuse the industrial relief yard and qualified emergency-depot geometry, with a
distinct armored approach, a defended Fuel building and a separate Relay breach
zone. Supply an appropriate starting counterforce of battle tanks, APC and infantry;
retain ordinary production and Select/Move/Attack/Hold controls for reinforcement
and target priority. No custom gameplay button or hidden troop order from Show Me.

Use the canonical fixed-reserve fallback: physically stored, finite usable Fuel and
a protected civilian allocation. Normal armor movement consumes usable Fuel;
infantry production uses canonical barracks costs. The civilian floor is not available
for military spending. No account-wallet
Fuel, recurring top-ups or invented refinery output. The starting state must remain
winnable and display the reserve through normal HUD/panels.

Two warned approach groups, conventional silhouettes, readable routes and sensible
camera pacing. No match movement or combat before the opening comic finishes.
Loss of the Fuel building or a Relay breach fails the mission. All hostile command
vehicles must be stopped. Comics interrupt safely and precede result settlement.

## Presentation

Reuse approved Campaign/briefing/HUD/result layouts, mobile spacing, typography,
portrait catalog and ARIA Play/Stop. Existing Steel Push art pixels are preserved;
author distinct dialogue framing for 16:9 and 20:9 without regenerating faces.
Seven dialogue lines (three briefing, one comms, three debrief), localized English
and natural Persian. The user approved all fourteen local ElevenLabs voice clips
on 2026-09-28; generation and full playback checks are part of validation.

## Acceptance evidence to collect

- Focused config, Fuel floor/spending, protected-building, breach/deadline and comic checks.
- Native English 16:9 and Persian 20:9 visual review, including clean preview and result.
- Both complete normal-input ARIA journeys: actual armor combat, Fuel consumption,
  mandatory comics/voices, result, reward settlement and settled Campaign return.
- Separate user visual review, real-player acceptance and device testing; do not infer
  them from compilation or injected test outcomes. Retain failed candidate logs.

Status: implemented and validated for Unity Editor player acceptance on 2026-09-28.
English 16:9 and Persian 20:9 automated normal-input journeys passed, including
all fourteen local voice clips, live armor combat/Fuel use, three-star result,
reward settlement and Campaign return. See `readiness.md` for exact markers,
failed evidence, native visual review and remaining human/device gates.

## Support unlock dependency — approved design 2026-09-28

See [Support design and build plan](../SupportSystem/support_design_and_build_plan.md).
The approved Support rollout preserves this mission's combat and adds no custom
in-match button. After Smoke Screen passes its
runtime/native UI/normal-input gates, successful reward settlement grants
`ability.smoke_screen` once and exposes Support from CH04-M03 onward. Replays must
not duplicate grants, and late profiles need the documented milestone migration.

Proposed debrief authorization copy: “Smoke support is cleared for the next
operation. You decide when to deploy it.” Integrate into the existing debrief beat
and localize through the normal English/Persian copy pipeline; do not regenerate
approved art or add a new comic/voice scope implicitly. Show the Smoke Screen unlock
in the settled reward result. Until runtime readiness, do not ship a reward that
promises unavailable gameplay. The design is approved; runtime readiness remains a dependency,
not evidence that the unlock or Support runtime has been implemented.
