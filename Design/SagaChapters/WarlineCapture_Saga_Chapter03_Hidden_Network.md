# WarlineCapture Saga Chapter 3: Hidden Network

Date: 2026-05-05

## Purpose

This document owns Saga Chapter 3 high-level design. Internally this is `Chapter 3`; player-facing title is `Hidden Network`.

Use `../WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md` before turning these mission slots into detailed specs. Intel/raid previews are strategic context; close-up raid, ambush, extraction, and breach play must use tactical map metadata.

## Chapter Role

Hidden Network shifts the campaign toward Intel, evidence, trust risk, ambushes, and raids. The player should feel that better information creates safer tactical outcomes.

## Design Focus

| Area | Direction |
|---|---|
| Primary fantasy | Uncover and dismantle a hidden hostile network inside the city. |
| New pressure | Intel confidence, collateral risk, false leads, ambush timing. |
| Main player verbs | Scan, patrol, confirm, raid, extract evidence, protect trust. |
| Civilian/district hook | Poor Intel or reckless raids can reduce trust even after tactical success. |
| Economy exposure | Intel becomes central; Credits, Materials, and Fuel remain supporting resources. |
| Threat families | Hidden Cell, Defensive Garrison, Mixed Force. |

## Recommended Mission Arc

| Mission Slot | Working Title | Archetype | Teaching / Mastery Goal |
|---|---|---|---|
| M01 | Signal Trace | Patrol Intercept | Follow evidence and stop a moving cell. |
| M02 | Safehouse Sweep | District Raid | Raid a suspected node with visible Intel confidence. |
| M03 | False Front | Civilian Evacuation | Protect civilians during a misdirection attack. |
| M04 | Evidence Chain | District Raid / Airlift Extraction | Capture evidence and extract safely. |
| M05 | Network Break | Breach Assault | Strike a fortified command cell using gathered Intel. |

## Unlock And Reward Pacing

| Beat | Reward Direction |
|---|---|
| Early chapter | Intel and CommanderXP. |
| Mid chapter | `ability.drone_scan` SupportAbilityUnlock and `upgrade.air.drone_sensor` BlueprintParts. |
| Late chapter | `ability.breach_charge` support parts and `upgrade.unit.marksman_recon` parts. |
| Chapter completion | Evidence-themed chapter reward and unlock path into `OperationIntel` actions. |

## Balance Direction

Use Standard bands for most missions. District Raid missions should include explicit Intel confidence and collateral-risk targets so a balancer can compare success with high-confidence and low-confidence variants.

## Validation Focus

- Mission Briefing shows enemy intel and confidence clearly.
- Strategic intel previews and tactical objective/ambush anchors resolve through map ids and metadata instead of baked preview art.
- Raid confirmation surfaces explain collateral risk and cost.
- Intel rewards and OperationIntel deltas are not confused with wallet Intel.
- Hidden Cell ambushes do not spawn unfairly on top of the player.
