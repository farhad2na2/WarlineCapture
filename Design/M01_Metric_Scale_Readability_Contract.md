# WarlineCapture M01 Metric Scale And Readability Contract

Date: 2026-05-08

## Purpose

This document locks the M01 First Contact tactical scale/readability rules after repeated Gate 4 visual review rejections for tiny or oversized unit/building scale, oversized marker treatment, unclear marker language, unrealistic movement speed, missing or wrong animation states, selection hit-target problems, and public unit presentation that still exposed renderer-wrapper paths instead of the accepted ECS/atlas direction.

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

M01 visual scale must be derived from real-world anchors, not hand-tuned tiny or huge sprite values.

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

## Scale Derivation Rules

Art/Atlas and Gameplay must explain scale from anchors before asking for review.

Required:

- Start from the source frame's intended upright soldier height and target about `1.8m` in tactical context.
- Compare the soldier against visible doors, road width, sidewalk/shoulder width, and building footprint.
- Adjust atlas/world scale until the soldier reads as a plausible adult human beside a `2.3m` door.
- Preserve the sprite aspect ratio. Do not squash the soldier to fit a marker, card, or test target.
- If a tested numeric scale makes the soldier too large or squashed, reduce it. The user's latest review noted that the current soldier visual read better near `0.15` than `0.2`; treat that as evidence that numeric scale must be validated visually against the metric anchors, not forced upward blindly.
- Record the chosen runtime scale and the capture evidence used to validate it.

Rejected:

- arbitrary tiny values that make humans/buildings read like icons or toy props
- arbitrary large values that make soldiers dominate roads/doors or look squashed
- scale acceptance without a same-capture comparison to soldier, door, road, and building footprint context

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
- Soldier body and formation footprint should be usable for selection. The player must not need to click exact foot pixels.

Hostile patrol:

- `unit.enemy.patrol_01` must read as hostile at tactical scale.
- Enemy readability may use faction tint/material, silhouette, marker, or stance, but cannot rely only on a tiny color accent that disappears at camera scale.
- Hostile patrol must remain visually distinct from the player squad while preserving the same metric human-scale rules.
- Red flash, alert state, enemy marker, or damage state must communicate an enemy/patrol state. It must not read as an unexplained sitting object, artifact, or random flashing prop.

## Selection Treatment

Selection should be tactical, grounded, and proportional.

Required:

- Selected state is visible in world and HUD.
- World selection should sit under each soldier, or use an equivalent subtle grounded treatment around the formation.
- The selected treatment must be small enough to support infantry scale and large enough to read at public camera scale.
- Selection must not cover the screen, hide map context, obscure target feedback, or read as a huge unrelated green/blue overlay.
- Selection hit targeting should use the soldier body/formation footprint or equivalent interaction bounds. It is not acceptable if a player can only select the soldier by tapping exact foot pixels.
- Placeholder squares or raw debug plates must not be visible in public review captures.

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
- yellow placeholder square or debug-looking plate visible as the selected state
- selection only triggered by exact foot-pixel clicks

## Move / Attack / Target Markers

Command markers should be readable but physically restrained.

Required:

- Move, attack, invalid, and objective markers are separate runtime feedback layers and must not be baked into terrain art.
- The target/move/attack marker footprint should read at roughly two soldier footsteps wide, unless a specific command requires a larger area marker.
- Marker scale is measured against the current accepted soldier footprint, not screen percentage.
- Markers must sit on the ground plane and must not hide the soldier body, enemy body, road context, or objective.
- Marker color and shape must make command intent clear without becoming a screen-covering blob.

Rejected:

- huge green target marker covering the screen or dominating the scene
- marker so small that the player cannot identify the move/attack target
- marker that is visually ambiguous with selection, enemy flash, impact, or objective state
- marker that hides the exact unit or point being commanded

## Movement And Animation

Movement must look like realistic infantry movement, not teleporting or sliding.

Required:

- Idle units must have a visible idle animation or alive/ready state. Static frozen soldiers should not be accepted as final M01 presentation.
- Units must visibly animate while moving.
- Movement state must use the atlas-backed move/run state, not a static idle pose sliding across the map.
- Movement speed must be calibrated to plausible infantry run/jog motion against the `1.8m` soldier and road/building context.
- The first M01 move to `tutorial.move_target.cover_01` must be slow enough for the player to see command response, direction, and arrival.
- Units return to idle or hold state after arrival.
- Moving soldiers must use standing run/move frames. Do not use crouched, sitting, kneeling, death, hit, or artifact frames unless the unit is intentionally in that state.
- Source frames must be checked for artifacts, including stray foot pixels at the top or around the silhouette.

Designer acceptance test:

```text
When the player issues the first move order, a new viewer should see soldiers begin moving, understand their direction, and perceive a run/jog animation before arrival.
```

Rejected:

- instant jump / teleport feeling
- sliding idle sprite
- speed so high that the move target is reached before the player reads the action
- no run animation during movement
- crouched/sitting movement frames for normal running infantry
- missing idle animation or frozen idle state
- obvious source-frame artifacts visible in public captures

## Runtime Presentation Rule

Public M01 unit and calibration-building visuals must be ECS entity / atlas-backed, or explicitly accepted as a non-gameplay terrain/decor exception by PM before review.

Required:

- Public M01 infantry presentation must be owned by ECS runtime entities with atlas-backed visual states.
- Public review reports and captures must not present a SpriteRenderer unit path as the accepted final direction.
- Public review reports and captures must not present a GameObject renderer-wrapper path using `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` as the accepted visible unit/building presentation for M01 gameplay entities.
- SpriteRenderer-era implementation names, proxies, or capture references may appear only as historical evidence and must not be used to justify Gate 4 visual acceptance.
- Idle, move/run, attack, damaged, and destroyed/death states must resolve through the atlas visual-state contract.
- If a temporary renderer wrapper still exists internally, the handoff must state why it is not the public accepted presentation path and how ECS owns identity/state. QA/HCI must not pass a review by checking only that `SpriteRenderer` is absent while `MeshRenderer`/`MeshFilter` wrappers still represent the public unit/building visuals.

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
- whether public visible unit/building paths avoid accepted `SpriteRenderer`, `MeshRenderer`, and `MeshFilter` wrapper presentation
- whether target/move/attack markers are about two soldier footsteps wide
- whether selection can be triggered from the body/formation footprint instead of exact foot pixels
- whether idle and run frames are correct and free of crouch/sit/artifact errors

## User Feedback Regression Checks

QA/HCI must include this matrix in the next rejection-aware validation report.

| Check | Reject If |
|---|---|
| ECS visual path | Public M01 units/buildings are accepted through `SpriteRenderer`, `MeshRenderer`, `MeshFilter`, old child `Model`, or unclassified GameObject renderer-wrapper presentation. |
| Soldier scale/aspect | Soldiers read too large, too small, or squashed against `1.8m` soldier, `2.3m` door, road, and building footprint context. |
| Building scale | Building doors/footprints read as toy/decor scale or cannot calibrate human scale. |
| Selection marker | Marker is huge, screen-covering, unclear blue/green blob, placeholder square, or hides the unit/ground context. |
| Selection input | Soldier can only be selected by exact foot pixels instead of body/formation footprint. |
| Target/move/attack marker | Marker is not roughly two soldier footsteps wide for a point command, or it dominates/hides the scene. |
| Idle animation | Idle soldiers are frozen or use wrong state frames. |
| Move animation | Moving soldiers slide, teleport, or use crouched/sitting/kneeling/hit/death/artifact frames. |
| Enemy clarity | Enemy state reads as a red flashing sitting object or unexplained artifact instead of a hostile patrol/readable state. |
| Repeated feedback | Any issue previously rejected by the user is missing from the validation matrix, marked as polish, or passed without evidence. |

## Acceptance Summary

M01 scale/readability is acceptable only when:

- soldiers read near `1.8m` human scale
- building doors read near `2.3m`
- buildings read from door/footprint/context, not tiny decor values
- roads and sidewalks make infantry movement believable
- player squad reads as four soldiers under one squad entity
- selection is small, grounded, and readable
- move/attack/target markers are about two soldier footsteps wide for point commands
- selection works from soldier body/formation footprint, not exact foot pixels
- idle animation is visible and correct
- movement uses visible run/move animation and plausible speed
- moving soldiers do not use crouched/sitting frames unless intentionally crouching
- public unit/building presentation is ECS entity / atlas-backed, not `SpriteRenderer`, `MeshRenderer`, or `MeshFilter` public gameplay presentation
