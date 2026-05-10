# WarlineCapture Large-Scale Grid Movement Design

Date: 2026-05-08

## Purpose

This document defines what `large-scale grid-based movement` means for WarlineCapture as a AAA-style mobile RTS, how it fits the current design direction, and when it becomes a player-facing product promise rather than only a foundation simulation capability.

Use this document when updating:

- tactical map metadata
- ScenarioSetup and mission specs
- unit selection, move, attack, transport, and build-placement commands
- tactical HUD command feedback
- minimap, objective jump, and threat jump behavior
- Chapter 1 validation scenes
- balance probes that measure path clarity, travel time, congestion, and loss rate

## Evaluation

`Large-scale grid-based movement` was an initial goal and remains valid, but the current design direction changes how it should be presented.

The project already has the foundation:

- grid movement and pathing
- road speed behavior
- blockers, walls, gates, buildings, and breach paths
- infantry, vehicle, ground, and air movement
- player selection and move/attack commands
- AI squad movement and attack orders
- threat warnings tied to moving targets

The missing design layer is player-facing clarity. On mobile, `large-scale` cannot mean showing maximum units or maximum grid area at all times. It must mean:

```text
The player can command meaningful groups across a large tactical space while the game keeps paths, threats, objectives, and consequences readable at mobile landscape scale.
```

If movement is technically large but unreadable, it fails the AAA mobile design bar.

## Design Position

Large-scale grid movement is one of WarlineCapture's core differentiators, but it must serve the north star:

```text
Read the city.
Identify the hostile faction's position.
Prepare the right force.
Strike with tactical control.
Protect civilians and infrastructure.
Live with the district consequences.
```

Movement is not only locomotion. It is how the player expresses command under pressure:

- reposition squads before contact
- intercept hostile patrols before civilian panic
- hold roads, sidewalks, plazas, gates, and choke points
- avoid collateral-heavy routes
- reinforce or extract through APC/helicopter transport
- respond to radar/threat warnings
- choose between fast road movement and safer covered movement
- exploit breached walls, gates, and approach cells

## Stage Ownership

| Stage | What Movement Means | Acceptance |
|---|---|---|
| Foundation | Grid/pathfinding systems exist and units can move, attack, breach, and use roads/transports. | Current simulation reference can document it. |
| M01 Playable Vertical Slice | Player can select a small squad, issue move/attack commands, read markers, and complete First Contact on a tactical map. | M01 passes select/move/attack/objective/result validation at mobile landscape scale. |
| Chapter 1 Tactical Expansion | M01-M05 each teach one movement pressure: patrol intercept, base movement/building approach, threat response, transport extraction, breach assault. | Every Chapter 1 mission has metadata-backed routes, blockers, anchors, camera bounds, UI feedback, and validation scenes. |
| Persistent Operation Integration | Tactical movement affects district consequence: patrol routes, raid confidence, civilian safety, infrastructure protection, and ignored threat drift. | Operation-launched missions preview route/consequence risk and results update district state. |
| Production Scale | Larger tactical spaces, more squads, more enemy paths, more simultaneous pressures, and broader AI movement remain readable and performant. | Device/performance gates, readability captures, and balance probes pass across representative mission archetypes. |

## AAA Mobile Design Principles

### Readability First

The player should understand movement intent before and after issuing a command.

Required feedback:

- selected entity/squad outline
- valid ground target affordance
- move destination marker
- short path preview or direction pips where practical
- accepted command pulse
- rejected command marker and reason text
- current order state in HUD
- minimap viewport and destination feedback

Do not add large-unit scenarios until this feedback is reliable.

### Scale Through Squads, Not Micromanagement

WarlineCapture should feel large-scale through grouped command decisions, not through tiny individual units that require precision tapping.

Rules:

- early missions use small squads with generous selection targets
- later missions support multiple squads and vehicles
- group movement should preserve formation readability
- individual units may exist in simulation, but UI should expose squad-level intent first
- transport commands should move groups with clear boarding, loaded, landing, and extraction states

### Metadata Is Gameplay Truth

Terrain art can show roads, sidewalks, plazas, pads, curbs, damage, rubble, and dressing. Movement must use tactical metadata.

Required metadata:

- walkable zones
- road graph / preferred road cells
- sidewalk or infantry-safe zones
- blockers and invalid zones
- building footprints and approach cells
- spawn anchors
- patrol/route anchors
- objective anchors
- civilian zones
- camera bounds
- minimap mapping

No tactical mission should claim large-scale movement readiness without metadata validation.

### Large Tactical Space, Focused Camera

AAA mobile scale should come from the map and mission structure, not from zooming out until units become unreadable.

Camera rules:

- default camera starts on the current decision area
- threat, objective, minimap, and ARIA jumps move to named anchors inside tactical bounds
- wide strategic context belongs to Saga/Operation/Briefing/minimap surfaces
- tactical camera stays close enough for unit role, direction, selection, and VFX readability

### Movement Must Create Decisions

Every major mission should ask at least one movement question:

- take the road for speed or side route for safety
- intercept early or hold defensive ground
- split squads or move as one group
- risk vehicle exposure or use infantry cover
- rush civilians out or stabilize the route first
- breach a wall or approach through a defended gate

If a mission only asks the player to tap the enemy, it is not using large-scale movement as design.

## Mission Design Patterns

| Pattern | Movement Job | Best Modes |
|---|---|---|
| Patrol Intercept | Read route, move to cut-off point, attack before escape or civilian panic. | Saga M01, Operation Patrol, Quick Custom probe |
| Road Control | Use roads for faster reinforcement while defending chokepoints. | Base Defense, Convoy Defense |
| Civilian Corridor | Clear and hold movement lanes for civilians or transports. | Civilian Evacuation, Airlift Extraction |
| Breach Approach | Move to approach cells, destroy gate/wall/core, avoid blocked footprints. | Breach Assault, District Raid |
| Threat Response | Jump from warning/minimap to target route, redirect squads quickly. | Radar Warning, Operation escalation |
| Multi-Vector Pressure | Manage two or more routes without losing readable command state. | Chapter finale, Production Scale |

## Chapter 1 Movement Arc

| Mission | Movement Lesson | Scale Target |
|---|---|---|
| M01 First Contact | Select, move, attack, read destination/attack markers. | One friendly squad, one enemy patrol, one clear route. |
| M02 Establish The Base | Move builders/defenders around sockets, pads, and resource paths. | One base area plus short defense routes. |
| M03 Radar Warning | Respond to an incoming route warning before district damage. | One to two threat routes, minimap/threat jump required. |
| M04 Airlift | Move/extract under landing-zone pressure. | Ground squad plus transport path and landing zone. |
| M05 Breach Assault | Approach a fortified node through gates/walls and attack target footprints. | Multiple routes, blockers, breach target, enemy core. |

Chapter 1 should prove movement clarity before the game scales to larger fights.

## UI And Feedback Contract

Required UI surfaces:

- `SCN-08 Battle HUD`
- `PREFAB-01 ObjectiveTracker`
- `PREFAB-02 SquadTray`
- `SCN-10 UnitCommandWheel` or direct command controls
- `POP-01 ThreatAlert`
- `POP-03 BuildPlacement`
- minimap viewport and jump feedback

Required movement states:

- no selection
- selected squad idle
- selected squad moving
- command mode: move
- command mode: attack
- target accepted
- target rejected
- target unreachable
- target out of bounds
- target blocked
- target not attackable
- transport boarding / loaded / extracting where relevant

## Validation Gates

### M01 Gate

Pass only when:

- selected squad can move to a walkable metadata anchor
- selected squad rejects blocked/out-of-bounds targets with visible feedback
- selected squad can attack the enemy patrol
- unit path avoids blockers
- current order is visible in HUD
- move and attack markers remain readable at 16:9 and 20:9
- objective progress and result flow work after movement/combat

### Chapter 1 Gate

Pass only when each Chapter 1 tactical map has:

- ground art
- metadata overlay
- pathfinding validation
- selection validation
- movement validation
- attack/approach-cell validation
- minimap mapping
- mobile landscape capture
- designer/QA readability review

### Production Scale Gate

Pass only when representative missions prove:

- multiple squads can be selected or commanded without UI ambiguity
- enemy route pressure is readable before contact
- road/path preference changes actual movement behavior
- congestion does not break commands or readability
- offscreen threats are communicated through minimap/threat feedback
- performance remains stable on target mobile profiles
- balance probes classify travel time, time-to-contact, loss rate, and objective completion as acceptable

## Metrics

Track these in probes or reports:

- time to first command
- time to first contact
- travel time from spawn to objective
- path failure count
- invalid command count by reason code
- route completion rate
- unit congestion / blocked duration
- own losses during movement
- civilian loss tied to delayed movement
- camera jump usage
- minimap jump usage
- objective completion after movement-heavy sequence

## Implementation Notes

- Do not create a second pathfinding model for 2D isometric maps.
- Tactical metadata should populate existing grid/pathfinding buffers.
- `ScenarioSetup` should reference stable map IDs: `LevelId`, `IsoMapId`, `MapPreviewArtId`, and `MinimapArtId`.
- UI should consume typed command outcomes and reason codes, not infer movement validity from screen position alone.
- ARIA should use typed command intents and metadata anchors for Show Me / Do It behavior.
- Balance probes should run with fixed seeds first, then expand to seed sweeps after deterministic behavior is stable.

## Design Decision

Keep `large-scale grid-based movement` in the README, but treat it as a staged promise:

- already valid as a foundation capability
- product-visible in M01 when select/move/attack is readable and validated
- design-complete for Chapter 1 when five tactical missions prove different movement pressures
- production-scale only after multi-squad, multi-route, mobile readability, and performance gates pass
