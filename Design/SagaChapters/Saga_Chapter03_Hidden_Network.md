# WarlineCapture Campaign Chapter 3: Hidden Network

Date: 2026-07-10

Status: Active detailed high-level chapter design. No step-by-step implementation content.

## Purpose And Authority

This document owns Campaign Chapter 3 high-level story, character, feature, mission, consequence, reward, and presentation direction. `../Campaign_Narrative_Bible.md` owns canonical story facts. `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` owns feature-readiness truth.

`../Campaign_Mission_High_Level_Design_Catalog.md` owns the complete per-mission gameplay/story contracts and fallbacks. `../Campaign_Narrative_Sequence_And_Comic_Catalog.md` owns the exact briefing, in-mission communication, debrief, chapter-opening, and Protocol Fragment sequence beats.

Read after the v0.2 GDD, narrative bible, North Star, feature matrix, level/mission plan, narrative presentation design, mission high-level catalog, and narrative sequence catalog.

## Chapter Promise

The Commander now knows that the Ash Line is feeding dormant Civic Relay nodes, but the network is hidden inside ordinary city systems. Chapter 3 asks the player to find confirmed threats without treating the population as the enemy.

```text
Better information should make force more precise, not merely more powerful.
```

## Story Contract

| Element | Chapter 3 authority |
|---|---|
| Opening state | Public fear and misinformation rise as the Ash Line plants false reports and moves through compromised logistics contacts. |
| Chapter question | Which evidence is real, and why is ARIA unable to read part of her own archive? |
| Story faction | Ash Line hidden cells, weapons nodes, couriers, false fronts, and bunker command. |
| Commander arc | Replace rapid reaction with confirmed, explainable decisions under uncertainty. |
| ARIA arc | Make an incomplete interpretation, disclose uncertainty, and reveal that her missing archive is self-sealed. |
| Dalia arc | Confront the cost of waiting for confirmation, then see the cost of striking the wrong target. |
| Samira arc | Become a trusted information source without being reduced to an infallible civilian oracle. |
| Chapter climax | Preserve the original audit while destroying the network bunker trying to erase it. |
| Protocol Fragment 3 | ARIA partitioned herself to stop Nadir Qassem's illegal Civic Relay override. |
| Exit hook | Qassem's hidden cells were only the access layer; a proxy military force is preparing open escalation. |

## Threat Identification Rule

This chapter cannot rely on visual profiling as a mechanic.

- Civilian character configs remain civilian.
- Insurgent character configs remain armed hostile actors.
- Hostility is confirmed through weapons, hostile behavior, trusted Intel, restricted-zone context, and objective state.
- A person near a suspicious building is not automatically hostile.
- A civilian transmitter, vehicle, market route, or home may be innocent even when an Ash Line node is nearby.
- False leads change location confidence or objective priority; they do not transform civilian models into enemies.
- Universal minimap knowledge must be removed before hidden-threat missions become required.

## Feature Contract

| Feature | Chapter role | Readiness rule |
|---|---|---|
| Scan/Intel | Reveal, confirm, compare, and expire information. | Confidence, reveal state, false leads, HUD, minimap, and objective behavior must be coherent before M01. |
| Civilian-risk raids | Reward verified targets and controlled force. | Civilian identity and collateral boundaries must be readable before penalties become severe. |
| Evidence | Carry the mystery and distinguish tactical destruction from investigative success. | Evidence objectives/results require explicit authored state; story-critical chapter evidence remains guaranteed. |
| APC/helicopter boarding | Move witness/archive assets through contested space. | Capacity, ownership, board/unboard, loss, and extraction tracking must be reliable. |
| ARIA uncertainty | Make incomplete data legible without making the assistant useless. | Recommendations must state confidence and cancel when stale. |
| Tactical follow cinematic | Optional hero presentation for one confirmed high-value action. | It cannot hide urgent state or turn ordinary attacks into repetitive spectacle. |

## Principal Character Beats

| Character | Chapter movement |
|---|---|
| Commander | Learns that a delayed correct strike may be better command than an immediate uncertain one. |
| ARIA | Moves from concealed absence to an honest explanation of self-partition and its cost. |
| Dalia | Challenges caution in M02, then becomes the strongest advocate for confirmed targets after M03. |
| Samira | Corrects compromised data in M03 but acknowledges the limits of local reports. |
| Salma Idris | Authorized security lead whose knowledge of access control helps distinguish legitimate guards from Ash Line infiltration. |
| Nadir Qassem | Manipulates feeds and frames ARIA's missing memory as proof she cannot be trusted. |

## Mission Arc

| Mission | Story and objective | Dominant learning/mastery | Character beat | Evidence beat |
|---|---|---|---|---|
| M01 Signal Trace | Follow competing signals to stop a moving armed cell without disrupting an innocent civilian transmitter. | Scan, Intel confidence, patrol intercept. | ARIA explains what she knows and what remains inference. | One signal contains an old Relay routing signature. |
| M02 Safehouse Sweep | Confirm and raid a weapons node beside occupied homes while preserving adjacent structures. | Intel-gated raid and collateral boundary. | Dalia argues for speed, then sees that the unconfirmed neighboring site was a family home. | Captured records identify an evidence courier. |
| M03 False Front | A planted report draws JRC away while the Ash Line attacks an evacuation route. | Civilian evacuation, deception, divided attention. | Samira's report corrects ARIA's feed; ARIA marks its uncertainty rather than silently switching truth. | The false report came through a compromised Relay-era authority channel. |
| M04 Evidence Chain | Move a recovered archive and protected witness through an ambush to an extraction point. | APC/helicopter boarding, escort, evidence preservation. | Salma and Laila support the extraction; Dalia prioritizes evidence over chasing retreating fighters. | The archive contains ARIA's signature on a self-sealing command. |
| M05 Network Break | Assault the bunker holding the original audit while the Ash Line attempts to erase it and endanger nearby civilians. | Scan, precision breach, extraction, civilian-risk mastery. | ARIA tells the Commander the partition was her deliberate act. | Protocol Fragment 3 identifies Qassem as the original override architect. |

## Mission High-Level Design References

These references complete the one-per-mission high-level layer. Detailed implementation specifications must later use the shared template in `../Level_And_Mission_Content_Plan.md` without changing these contracts.

| Mission | Gameplay/story contract | Sequence/comic coverage |
|---|---|---|
| M01 Signal Trace | [CH03-M01 Signal Trace](../Campaign_Mission_High_Level_Design_Catalog.md#ch03-m01-signal-trace) | [Chapter 3 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-3-hidden-network): `seq.ch03.m01.brief`, `.comms`, `.debrief` |
| M02 Safehouse Sweep | [CH03-M02 Safehouse Sweep](../Campaign_Mission_High_Level_Design_Catalog.md#ch03-m02-safehouse-sweep) | [Chapter 3 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-3-hidden-network): `seq.ch03.m02.brief`, `.comms`, `.debrief` |
| M03 False Front | [CH03-M03 False Front](../Campaign_Mission_High_Level_Design_Catalog.md#ch03-m03-false-front) | [Chapter 3 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-3-hidden-network): `seq.ch03.m03.brief`, `.comms`, `.debrief` |
| M04 Evidence Chain | [CH03-M04 Evidence Chain](../Campaign_Mission_High_Level_Design_Catalog.md#ch03-m04-evidence-chain) | [Chapter 3 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-3-hidden-network): `seq.ch03.m04.brief`, `.comms`, `.debrief` |
| M05 Network Break | [CH03-M05 Network Break](../Campaign_Mission_High_Level_Design_Catalog.md#ch03-m05-network-break) | [Chapter 3 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-3-hidden-network): `seq.ch03.m05.brief`, `.comms`, `.debrief`, then `seq.ch03.close.protocol_fragment_03` |

## Consequence Direction

| Outcome family | Story consequence |
|---|---|
| Confirmed target and low collateral | Trust and Evidence improve; later briefings begin with better local cooperation. |
| Correct objective but lost evidence | Tactical success remains, but optional context and epilogue Evidence are reduced. |
| Reckless raid | Trust and Infrastructure decline even if the hostile node is destroyed. |
| Witness/archive protected | Qassem's propaganda loses credibility and the Story Archive gains supporting evidence. |
| False lead avoided | ARIA's uncertainty model and the Commander's legitimacy are reinforced. |

## Reward And Progression Direction

- Early: Intel and CommanderXP tied to verified discovery, not indiscriminate destruction.
- Middle: scan/recon support and fixed specialist progression only when supported by the combat catalog.
- Late: breach/evidence-extraction support and chapter recognition.
- Completion: Protocol Fragment 3, Story Archive audit entry, and Chapter 4 access.

No optional evidence, star, purchase, or Operations action may gate the identity of Qassem or ARIA's self-partition reveal.

## Presentation Direction

- Chapter opening uses contradictory reports and repeated locations to make uncertainty visual without confusing the objective.
- Intel states use clear labels such as `Unconfirmed`, `Corroborated`, and `Confirmed`, not only color.
- M02 art must visually distinguish the verified weapons node from adjacent civilian homes.
- M03 avoids portraying community reporting as automatically reliable or corrupt; information gains value through corroboration.
- M05 uses a Tier B internal-memory sequence showing ARIA sealing the audit in abstract command imagery, not a misleading "evil AI" reveal.

## Balance Direction

Most missions use Standard bands. Pressure comes from time, incomplete information, route risk, and evidence preservation, not invisible unfair spawns.

- Waiting for confirmation must create pressure but remain viable.
- Low-confidence action must communicate risk before commitment.
- Scan must reveal actionable information, not decorative icons.
- Civilian penalties must match visible player agency.
- Extraction assets must not fail through opaque boarding or path behavior.

## High-Level Validation Questions

- Can the player explain why each hostile target is confirmed?
- Are civilians never used as surprise hostile skins?
- Does minimap and world visibility support uncertainty honestly?
- Do ARIA recommendations display confidence and invalidate stale conclusions?
- Can evidence be distinguished from ordinary loot or rewards?
- Is Protocol Fragment 3 guaranteed on completion?
- Does the chapter preserve the Middle Eastern fictional setting without mapping the enemy to a real community or religion?
