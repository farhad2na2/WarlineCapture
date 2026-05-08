# WarlineCapture Saga Chapter 2: Broken Grid

Date: 2026-05-05

## Purpose

This document owns Saga Chapter 2 high-level design. Internally this is `Chapter 2`; player-facing title is `Broken Grid`.

Use `../WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md` before turning these mission slots into detailed specs. Each mission needs strategic preview/minimap ids plus a tactical `IsoMapId` and metadata-backed level package.

## Chapter Role

Broken Grid expands from squad survival into infrastructure pressure. The player learns that roads, repair capacity, resource routes, convoy timing, and district infrastructure matter as much as winning a fight.

## Design Focus

| Area | Direction |
|---|---|
| Primary fantasy | Reconnect a fractured district logistics grid under hostile pressure. |
| New pressure | Infrastructure damage, convoy routes, repair windows, resource strain. |
| Main player verbs | Repair, escort, defend, prioritize, build routes, protect workers. |
| Civilian/district hook | Infrastructure recovery improves district readiness and income; collateral damage increases repair cost. |
| Economy exposure | Credits, Materials, Fuel, and limited Intel. |
| Threat families | Armored Column, Hidden Cell, Swarm Militia. |

## Recommended Mission Arc

| Mission Slot | Working Title | Archetype | Teaching / Mastery Goal |
|---|---|---|---|
| M01 | Gridlock | Infrastructure Repair | Repair a blocked route while defending crews. |
| M02 | Supply Line | Convoy Defense | Escort supplies through contested roads. |
| M03 | Sabotage Trace | Patrol Intercept | Find and stop saboteurs before infrastructure damage spreads. |
| M04 | Power Relay | Base Defense | Hold a repair site through timed waves. |
| M05 | Route Reopened | Convoy Defense / Breach Assault | Break the hostile hold on the district logistics node. |

## Unlock And Reward Pacing

| Beat | Reward Direction |
|---|---|
| Early chapter | Materials and `ability.field_repair` parts. |
| Mid chapter | Fuel, `Building_FieldWorkshop`, and `upgrade.vehicle.logistics_trucks` parts. |
| Late chapter | `Building_MedicalStation` unlock and `ability.repair_convoy` OperationSupply path. |
| Chapter completion | `upgrade.building.infrastructure` parts and OperationInfrastructure-facing reward pattern. |

## Balance Direction

Use Standard bands from `../WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md` for most missions. Infrastructure Repair missions can start near Tutorial timing if they introduce new repair rules, but combat pressure should still appear within the first 60-90 seconds.

## Validation Focus

- Repair and convoy objectives are visible in briefing, HUD, and result.
- Strategic route previews, minimap route pings, and tactical convoy/repair routes resolve to the same authored metadata anchors.
- Infrastructure consequences are shown in Mission Result and Operation-facing summaries.
- Materials are spent through authored actions and rewards use canonical types.
- Convoy and repair targets do not become unreadable under mobile HUD scale.
