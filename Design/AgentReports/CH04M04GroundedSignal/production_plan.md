# CH04-M04 — Grounded Signal

Status: preparation started; visual direction awaiting review. Not registered or playable yet.

## Authoritative contract

- Mission `saga.ch04.m04.grounded_signal`, immediately after Split Front.
- Source: [high-level design](../../Campaign_Mission_High_Level_Design_Catalog.md#ch04-m04-grounded-signal), [chapter design](../../SagaChapters/Saga_Chapter04_Air_And_Armor.md), [narrative catalog](../../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-4-air-and-armor), [future map plan](../../MapVariants/FUTURE_CONTENT_MAP_PLAN.md).
- Insert Karim and Yusuf's specialist team, disable the military air-support relay, recover its control hardware, and extract survivors. Keep the civilian airfield intact.
- Campaign Edition access and ordinary previous-mission progression. Required transport supplied; support optional. Award Paratroopers after success, for later use in Armor Break.

## Visual review

[Gameplay direction v01](Mockups/gameplay-direction-v01.png) uses the actual Air Corridor HUD and prepared CityEdgeAirfield overview as references. It retains the existing command bar, portraits, typography, Field Guide and ARIA Play/Stop controls. No new command buttons. Ground markers use corner brackets, small objective pins and extraction chevrons without large circles or painted text.

The mockup illustrates a clear runway, sheltered unloading apron, separate fenced military relay and protected civilian terminal. It is a proposed composition, not evidence of implemented map geometry or native readiness. AGENTS.md requires visual-direction approval before implementing the mission screen; approval is pending.

## Initial gameplay scope

Implement the documented fallback first: **runway unload → relay recovery → APC extraction**. Plane, parachute and cargo code exists, but its complete mission path is not qualified. Do not present two insertion choices until both pass normal-input validation. Helicopter extraction remains conditional.

Normal player flow:

1. Select the supplied transport plane and use its existing passenger drawer **Exit All** command at the apron.
2. Escort the dismounted specialists through the open service gate.
3. Use the existing Attack command against the military relay equipment, away from the civilian terminal.
4. Move both living original specialists to the recovery point. A visible interaction hold confirms hardware custody; leaving, boarding or dying cancels the hold.
5. Board the supplied APC with existing transport controls.
6. Drive the APC to the guarded exit and complete an uncontested secure hold with both specialists and recovered hardware aboard.
7. Show ordinary victory/debrief/return and advance to Armor Break when that mission exists.

ARIA explains the current action and moves the camera only when requested. It must not issue troop orders. Camera-tour or tutorial completion must never count as insertion, recovery or extraction.

## Runtime integration

The current generic extraction path expects an APC-to-aircraft transfer. Its armored route is enabled only for Evidence Chain. Breach and Extraction cannot simply be enabled together: `CampaignMissionRuntimeSystem` dispatches to the first specialized handler and returns.

Add a dedicated Grounded Signal specialization with per-attempt state and explicit mission identity. Reuse real transport roster/health/occupancy tracking and checkpoint secure-hold logic, with APC extraction opted in for this mission. Require actual insertion, relay disable and hardware recovery before extraction can settle victory. Preserve source version, session token, retry cleanup and result idempotency.

Reusable code/assets:

| Need | Existing foundation |
|---|---|
| Transport plane | `Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab` |
| Ground ramp unload | `TransportBoardingCommandDisembarkApplication.TryDisembarkTransport`; disembark planning validates complete passenger footprints |
| Plane doors | `UnitTransportPlaneDoorSystem` |
| Personnel / cargo drops | `UnitTransportAirdropSystem`, `TransportBoardingCommandAirdropPlanning`, `UnitAirMovementSystem` |
| Ordinary boarding | `TransportBoardingCommandBoardPassenger`, `TransportBoardingCommandBoardTransport`, `UnitTransportBoardingSystem` |
| Existing unload UI | `MatchHudTransportPassengerDrawerView` |
| APC | `Unit_Veh_APC_Fast.prefab` / `Unit_Veh_APC_Heavy.prefab`; capacity qualification required |
| Protected roster / exit hold | `CampaignMissionRuntimeSystem.Extraction.cs` |
| Scenario / narrative patterns | `M04AirliftConfigBuilder`, `CH03M04EvidenceChainConfigBuilder` |
| Prepared map route audits | `CH04M01AirCorridorConfigBuilder.Airfield.cs` |

Plane configuration currently supports 24 soldiers / 2 vehicles, runway landing, personnel parachute and cargo-drop references. Vehicle cargo is limited by actual footprint to 3×3 / 9 cells. Verify the chosen APC footprint before promising cargo insertion.

## Physical map preparation

Create an independent CityEdgeAirfield derivative; preserve the sources already used by Airlift and Air Corridor.

The source authoring places the runway at `z=560`, approximately `x=491..909`, with a static transport at `(860,560)` and barriers near the ends. Relocate those obstructions in the derivative and qualify continuous runway and aircraft approach clearance. The review mockup does not establish that clearance.

Author separate deployment/unload, military relay, hardware, civilian terminal, extraction apron and ground exit anchors. Sample actual surface heights. Audit connected paths with specialist and APC footprint margins, open fence gate clearance, uncluttered unloading cells, firing safety and departure clearance. No duplicated fences or prop/fence intersections.

## Narrative preparation

Sequences: `seq.ch04.m04.brief`, `.comms`, `.debrief`. Cast: Karim Daher, Yusuf Darzi, Laila, ARIA.

- Brief: show actual insertion conditions; Karim explains the available transport route; Yusuf identifies the control hardware to recover. Describe only the validated insertion path.
- Comms after insertion: Yusuf confirms physical Civic Relay compatibility; Laila reports the extraction window. No unsupported bomb-disposal mechanic.
- Debrief: specialists return with hardware; compatibility confirms deliberate preparation; evidence identifies Vanguard's command group and link schedule, leading to Armor Break.

English and conversational Persian copy, portrait/art references and voices must match the final implemented path. The separate 34-clip voice refresh covers existing Airlift, Supply Line and Split Front dialogue; it does not cover this new mission.

## Acceptance gates

| Gate | Current status |
|---|---|
| Visual direction | Mockup generated; user review pending |
| Native implemented screens | Not implemented |
| Compilation / focused rules checks | Pending implementation |
| Physical runway and route qualification | Pending derivative authoring |
| Full normal-input mission | Pending; include unloading, recovery, boarding, secure exit, ARIA, result and return |
| English / Persian comics and voice binding | Pending final mission copy |
| Airborne personnel / vehicle cargo option | Unqualified; excluded from initial selectable path |
| Real player / device acceptance | Pending user review |

Keep failed evidence. Automated pointer dispatch, injected victory or Scenario Lab success cannot substitute for a complete normal-input mission.
