# WarlineCapture Campaign Chapter 4: Air And Armor

Date: 2026-05-21

## Purpose

This document owns Campaign Chapter 4 high-level design. Internally this is `Chapter 4`; player-facing title is `Air And Armor`.

Use `../3D_SingleMap_Gameplay_Direction.md` and `../Level_And_Mission_Content_Plan.md` before turning these mission slots into detailed specs. Air/armor warning previews, landing zones, armor routes, breach lanes, and anti-air anchors must resolve to metadata on the same 3D operation map.

## Chapter Role

Air And Armor escalates battlefield pressure with aircraft, armored columns, anti-air readiness, vehicle survivability, and combined-arms defensive planning.

## Design Focus

| Area | Direction |
|---|---|
| Primary fantasy | Coordinate ground, armor, air warnings, and transport under heavy pressure. |
| New pressure | Air attacks, armored breach threats, anti-air preparation, vehicle preservation. |
| Main player verbs | Deploy, intercept, reinforce, defend, breach, extract. |
| Civilian/district hook | Heavy threats create visible risk to district security and infrastructure. |
| Economy exposure | Fuel pressure becomes more important; Materials and Credits support defense. |
| Threat families | Air Assault, Armored Column, Defensive Garrison, Mixed Force. |

## Recommended Mission Arc

| Mission Slot | Working Title | Archetype | Teaching / Mastery Goal |
|---|---|---|---|
| M01 | Air Corridor | Airlift Extraction | Manage landing zones and anti-air warning response. |
| M02 | Steel Push | Base Defense | Stop an armored column before it breaches the district line. |
| M03 | Split Front | Convoy Defense / Base Defense | Divide forces between route defense and base defense. |
| M04 | Grounded Signal | District Raid | Disable an air-support relay with combined arms. |
| M05 | Armor Break | Breach Assault | Use vehicles, infantry, and support to break fortified armor command. |

## Unlock And Reward Pacing

| Beat | Reward Direction |
|---|---|
| Early chapter | Fuel rewards, `upgrade.vehicle.apc_armor` parts, and `upgrade.air.helicopter_avionics` parts. |
| Mid chapter | `Unit_Veh_Missle_Launcher_Air` unlock and `ability.precision_strike` SupportAbilityUnlock. |
| Late chapter | `Building_NavalYard`, `Unit_Sea_Patrol_Boat`, and `upgrade.sea.patrol_hull` parts for coastal missions. |
| Chapter completion | `Unit_Sea_Coastal_Cutter` unlock and `ability.naval_fire_support` threshold reward. |

## Balance Direction

Use Standard and Mastery bands. Warning lead time, vehicle loss, anti-air readiness, and breach timing should be tracked in balance reports.

## Validation Focus

- Air warnings are readable and not color-only.
- Strategic air/armor threat previews, minimap pings, and tactical landing/route markers resolve to consistent map ids and metadata anchors.
- Aircraft and armored threats have fair warning lead time.
- Fuel costs do not block required tutorial usage.
- Vehicle survival stars evaluate independently from mission completion.
