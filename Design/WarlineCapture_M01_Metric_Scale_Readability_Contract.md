# WarlineCapture M01 Metric Scale And Readability Contract

Date: 2026-05-08

## Purpose

This document locks the M01 First Contact tactical scale/readability rules after the temporary Gate 4 art was rejected for tiny unit/building scale, oversized selection treatment, unclear marker language, unrealistic movement speed, missing run animation, and SpriteRenderer-era public unit presentation.

Use this contract before Art/Atlas recommends assets, Gameplay tunes runtime scale/motion, and QA/HCI reruns Gate 4.

## Scope

Applies to:

- `saga.ch01.m01.first_contact`
- `unit.player.rifle_squad_01`
- `unit.enemy.patrol_01`
- visible M01 civilian buildings/decor that calibrate scale
- selection, movement, attack, and destroyed/readability feedback

This contract does not expand M01 scope. M01 remains one player rifle squad, one hostile patrol, select/move/attack/objective/result, no player vehicles, no build/base/transport mechanics.

## Metric Anchors

M01 visual scale must be derived from real-world anchors, not hand-tuned tiny sprite values.

| Anchor | Contract |
|---|---|
| Standing soldier height | About `1.8m`. Player and hostile infantry must read as human-scale soldiers, not icon dots. |
| Building door height | About `2.3m`. Door height is the primary building scale calibration point. |
| Road/context width | Roads, shoulders, doors, and building footprints must make soldiers feel physically present in a city block. |
| Building footprint | Buildings scale from door height plus footprint readability, not from arbitrary decor scale. |

Designer rule:

```text
If a soldier, door, road, and building footprint are visible in the same M01 capture, their proportions must feel plausible before any art is accepted.
```

Do not accept a building that reads like a tiny decoration beside a soldier. Do not accept soldiers scaled so small that the squad reads as UI noise instead of infantry on tactical ground.

## Building Scale Rules

Buildings and civilian structures in M01 are calibration objects, not background icons.

- Scale buildings from visible door height first.
- Then confirm the building footprint reads as an occupied civilian structure at tactical-map scale.
- Roads and sidewalks must support believable infantry movement beside those buildings.
- Small props may be decorative, but primary civilian buildings must not be scaled like table-top miniatures.
- A building scale near a tiny decor value is not acceptable if the door/footprint no longer reads against a `1.8m` soldier.

QA/HCI should reject captures where a soldier appears larger than a normal door, where a building reads as toy-scale, or where road/building context cannot calibrate human scale.

## Infantry Readability Rules

Player squad:

- `unit.player.rifle_squad_01` must read as four distinct soldiers under one controllable squad entity.
- Each soldier should preserve readable stance, facing, grounding, and motion state at the public M01 camera.
- The squad must not become a single flat group icon or unreadable clump.
- Friendly identity should be readable through formation, HUD selection, and subtle faction treatment, not only a large marker.

Hostile patrol:

- `unit.enemy.patrol_01` must read as hostile at tactical scale.
- Enemy readability may use faction tint/material, silhouette, marker, or stance, but cannot rely only on a tiny color accent that disappears at camera scale.
- Hostile patrol must remain visually distinct from the player squad while preserving the same metric human-scale rules.

## Selection Treatment

Selection should be tactical, grounded, and proportional.

Required:

- Selected state is visible in world and HUD.
- World selection should sit under each soldier, or use an equivalent subtle grounded treatment around the formation.
- The selected treatment must be small enough to support infantry scale and large enough to read at public camera scale.
- Selection must not cover the screen, hide map context, obscure target feedback, or read as a huge unrelated green/blue overlay.

Preferred treatment:

```text
small per-soldier ground plates / rings / shadows + selected squad HUD state
```

Acceptable alternative:

```text
one restrained formation-level ground treatment, only if it remains under the squad footprint and does not dominate the screen
```

Rejected:

- giant selection marker covering the screen
- ugly unclear solid-color blob
- marker that floats above soldiers without grounding
- selection that hides movement, attack, objective, or enemy readability

## Movement And Animation

Movement must look like realistic infantry movement, not teleporting or sliding.

Required:

- Units must visibly animate while moving.
- Movement state must use the atlas-backed move/run state, not a static idle pose sliding across the map.
- Movement speed must be calibrated to plausible infantry run/jog motion against the `1.8m` soldier and road/building context.
- The first M01 move to `tutorial.move_target.cover_01` must be slow enough for the player to see command response, direction, and arrival.
- Units return to idle or hold state after arrival.

Designer acceptance test:

```text
When the player issues the first move order, a new viewer should see soldiers begin moving, understand their direction, and perceive a run/jog animation before arrival.
```

Rejected:

- instant jump / teleport feeling
- sliding idle sprite
- speed so high that the move target is reached before the player reads the action
- no run animation during movement

## Runtime Presentation Rule

Public M01 unit visuals must be ECS entity / atlas-backed.

Required:

- Public M01 infantry presentation must be owned by ECS runtime entities with atlas-backed visual states.
- Public review reports and captures must not present a SpriteRenderer unit path as the accepted final direction.
- SpriteRenderer-era implementation names, proxies, or capture references may appear only as historical evidence and must not be used to justify Gate 4 visual acceptance.
- Idle, move/run, attack, damaged, and destroyed/death states must resolve through the atlas visual-state contract.

M01 must not expose old child `Model` presentation or separate child `Destroyed` visuals as the accepted public infantry presentation.

## Gate 4 Capture Acceptance

The next Gate 4 visual/readability evidence should include:

- public M01 start state
- selected squad state
- first move in progress with run animation visible
- arrival/idle after movement
- attack feedback
- hostile destroyed/neutralized state
- result popup
- 16:9 and 20:9 captures when practical

Each capture set should state:

- soldier scale anchor used
- door/building scale anchor used
- whether road/context scale reads plausibly
- selection treatment used
- whether movement animation is visible during movement
- whether public unit presentation is ECS/atlas-backed

## Acceptance Summary

M01 scale/readability is acceptable only when:

- soldiers read near `1.8m` human scale
- building doors read near `2.3m`
- buildings read from door/footprint/context, not tiny decor values
- roads and sidewalks make infantry movement believable
- player squad reads as four soldiers under one squad entity
- selection is small, grounded, and readable
- movement uses visible run/move animation and plausible speed
- public unit presentation is ECS entity / atlas-backed, not SpriteRenderer public unit presentation
