# WarlineCapture Saga Chapter 5: Citywide Command

Date: 2026-05-21

## Purpose

This document owns Campaign Chapter 5 high-level design. Internally this is `Chapter 5`; player-facing title is `Citywide Command`.

Use `../WarlineCapture_3D_SingleMap_Gameplay_Direction.md` before turning these mission slots into detailed specs. Multi-objective planning views must connect to the same 3D operation map with metadata-backed routes, objective anchors, camera bounds, and minimap markers.

## Chapter Role

Citywide Command is the Saga mastery chapter. It combines prior mission grammar into multi-objective operations where tactical success, civilian safety, district consequence, and enemy escalation all matter.

## Design Focus

| Area | Direction |
|---|---|
| Primary fantasy | Command the full city response against coordinated hostile pressure. |
| New pressure | Mixed threats, simultaneous objectives, final hostile node, strategic consequence. |
| Main player verbs | Prioritize, coordinate, defend, raid, extract, breach, recover. |
| Civilian/district hook | The final chapter should clearly show how command choices affect city recovery. |
| Economy exposure | Full non-store gameplay economy can be visible; premium resources remain outside objective completion. |
| Threat families | Mixed Force, Air Assault, Armored Column, Hidden Cell, Defensive Garrison. |

## Recommended Mission Arc

| Mission Slot | Working Title | Archetype | Teaching / Mastery Goal |
|---|---|---|---|
| M01 | Citywide Alert | Base Defense / Convoy Defense | Manage two threat lanes with clear priorities. |
| M02 | Trust Under Fire | Civilian Evacuation | Protect civilians while maintaining tactical pressure. |
| M03 | Network Collapse | District Raid | Resolve a high-confidence raid with collateral risk. |
| M04 | Last Corridor | Airlift Extraction / Convoy Defense | Move critical assets through a contested route. |
| M05 | Command Node | Breach Assault / Mixed Force | Final hostile node with combined objective pressure. |

## Unlock And Reward Pacing

| Beat | Reward Direction |
|---|---|
| Early chapter | High-value CommanderXP, Credits, Materials, Fuel, and Intel. |
| Mid chapter | `Building_CommandPost`, `ability.rally_order`, and `upgrade.building.command_post` completion path. |
| Late chapter | `pc.cosmetic.base_banner.iron_guard` and `pc.cosmetic.hud_accent.amber_command` identity rewards. |
| Chapter completion | Major Campaign completion reward, `Unit_Sea_Missile_Craft` unlock for late Skirmish/coastal Operations, and durable account recognition. |

## Balance Direction

Use Mastery bands for finale missions and Standard bands for setup missions. Reports should compare objective completion, civilian losses, district consequence, resource float, and star distribution.

## Validation Focus

- Multi-objective HUD remains readable on mobile landscape.
- Planning previews do not replace combat-scale validation; each objective lane must be playable and readable on the 3D operation map.
- The final mission does not rely on hidden objectives or hidden score.
- Rewards use fixed `RewardConfig` entries.
- Campaign completion does not grant store-only power or bypass Operations consequences.
