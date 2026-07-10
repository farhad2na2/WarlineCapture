# WarlineCapture Campaign Chapter 5: Citywide Command

Date: 2026-07-10

Status: Active detailed high-level chapter and ending design. No step-by-step implementation content.

## Purpose And Authority

This document owns Campaign Chapter 5 high-level story, character, feature, mission, consequence, reward, presentation, and ending direction. The narrative bible owns canonical facts and epilogue values; the feature matrix owns readiness truth.

`../Campaign_Mission_High_Level_Design_Catalog.md` owns the complete per-mission gameplay/story contracts and fallbacks. `../Campaign_Narrative_Sequence_And_Comic_Catalog.md` owns the exact briefing, in-mission communication, debrief, chapter-opening, Protocol Fragment, and epilogue sequence beats.

Read after the v0.2 GDD, narrative bible, North Star, feature matrix, level/mission plan, narrative presentation design, mission high-level catalog, narrative sequence catalog, and Chapters 1-4.

## Chapter Promise

Qassem launches a coordinated citywide attack to force ARIA, the Commander's emergency authority, and the Civic Relay into the same place. The final chapter asks the player to command every mastered system while protecting the legitimacy that separates the Commander from Qassem.

```text
The final battle is not only for control of the city.
It is over what control is allowed to mean after the battle.
```

## Story Contract

| Element | Chapter 5 authority |
|---|---|
| Opening state | Ash Line cells and Vanguard units attack multiple districts, evacuation corridors, and Relay nodes at once. |
| Chapter question | Can the Commander save Sahrin without completing Qassem's model of permanent emergency rule? |
| Story factions | Ash Line remnants and Vanguard Brigade operating as one coordinated mixed force. |
| Commander arc | Become a legitimate citywide commander who can use power, explain it, limit it, and return it. |
| ARIA arc | Support complex operations with explicit consent, release the complete audit, and accept bounded shared governance. |
| Dalia arc | Defend disciplined military command and reject Qassem's logic of indiscriminate necessity. |
| Samira arc | Make civilian authority operationally credible during the crisis, not only after it. |
| Chapter climax | Assault the Civic Relay command complex while preserving the city systems tied to its defenses. |
| Protocol Fragment 5 | Qassem manufactured both the original shutdown and current crisis to install unilateral command; ARIA preserved the proof. |
| Canonical resolution | Qassem is defeated, his network is exposed, Sahrin survives, ARIA remains bounded, and recovery begins. |

## Finale Design Rule

Chapter 5 introduces no major new mechanic. Difficulty comes from combining previously taught systems, prioritizing simultaneous objectives, and managing consequence under pressure.

Any feature still Partial or Scaffolded at detailed-design time must be simplified, replaced, or omitted. The finale cannot depend on an unreliable system merely because the high-level campaign once listed it.

## Feature Mastery Contract

| Feature family | Finale expression |
|---|---|
| Core command and production | Rebuild losses, split forces, issue clear priorities, and preserve command responsiveness. |
| Roads and routing | Keep evacuation, convoy, and reinforcement corridors connected. |
| Oil/Fuel and automated hauling | Sustain aircraft and vehicles while protecting civilian services. |
| Import/export | Optional only if fully campaign-ready; provide an emergency supply decision, never a premium gate. |
| Boarding, aircraft, parachute, cargo | Move people and assets between isolated fronts through known risk. |
| Radar, G2A, G2G | Control air and long-range threats with confirmation and civilian-risk discipline. |
| Scan/Intel and evidence | Identify true command nodes and preserve the proof against Qassem. |
| Civilians/refugees | Make route, timing, and collateral decisions visible in lives and district outcomes. |
| ARIA bounded control | Offer consent-based help on simultaneous objectives while preserving immediate player override. |

## Principal Character Resolution

| Character | Resolution |
|---|---|
| Commander | Wins by coordinating military and civilian capability, then refuses permanent unilateral authority. |
| ARIA | Restores the missing audit, confirms her self-partition was protective, and accepts a transparent bounded role. |
| Major Dalia Rahim | Becomes the military guarantor that emergency force will remain disciplined and accountable. |
| Engineer Samira Haddad | Represents civilian co-ownership of recovery and Relay oversight; her confidence reflects earned Trust. |
| Captain Laila Nasser | Keeps the last air corridor open and connects the multi-front battlefield. |
| Nadir Qassem | Is defeated and exposed as the architect of the manufactured crisis. The story does not validate his terrorism as necessary. |

## Mission Arc

| Mission | Story and objective | Dominant mastery | Character beat | Evidence beat |
|---|---|---|---|---|
| M01 Citywide Alert | Defend two critical districts while Ash Line sabotage and Vanguard pressure attempt to overload command. | Multi-front command, production, radar, mixed threats. | ARIA asks permission before one bounded support action, proving that urgency no longer erases consent. | Attack timing maps the final active Relay nodes. |
| M02 Trust Under Fire | Keep evacuation and shelter routes open while Qassem broadcasts that JRC has abandoned the population. | Civilians, refugees, boarding, roads, defense, information pressure. | Samira's response reflects earned Trust but remains honest about avoidable losses. | Civilian witnesses identify the broadcast relay's real source. |
| M03 Network Collapse | Strike verified command nodes, neutralize long-range threats, and preserve the complete evidence chain. | Scan/Intel, precision raid, G2G restraint, evidence extraction. | Dalia chooses proof and civilian protection over a faster destructive solution. | The complete audit proves Qassem caused the original shutdown and present attacks. |
| M04 Last Corridor | Move Fuel, medical supplies, engineers, and reinforcements into the city center through a collapsing route network. | Roads, Oil/Fuel, automated logistics, convoys, airlift, parachute/cargo as authored. | Dalia and Samira jointly set route priority; Laila and Karim keep the air option alive. | The convoy carries the physical keys needed to restore bounded Relay access. |
| M05 Command Node | Assault the Relay complex, defeat Qassem's mixed force, and protect the city systems wired into his defenses. | Full combined-arms and multi-objective mastery with ARIA support. | All principal arcs resolve through action before the final governance choice. | Protocol Fragment 5 and the canonical revelation are released to the city. |

## Mission High-Level Design References

These references complete the one-per-mission high-level layer. Detailed implementation specifications must later use the shared template in `../Level_And_Mission_Content_Plan.md` without changing these contracts.

| Mission | Gameplay/story contract | Sequence/comic coverage |
|---|---|---|
| M01 Citywide Alert | [CH05-M01 Citywide Alert](../Campaign_Mission_High_Level_Design_Catalog.md#ch05-m01-citywide-alert) | [Chapter 5 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-5-citywide-command): `seq.ch05.m01.brief`, `.comms`, `.debrief` |
| M02 Trust Under Fire | [CH05-M02 Trust Under Fire](../Campaign_Mission_High_Level_Design_Catalog.md#ch05-m02-trust-under-fire) | [Chapter 5 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-5-citywide-command): `seq.ch05.m02.brief`, `.comms`, `.debrief` |
| M03 Network Collapse | [CH05-M03 Network Collapse](../Campaign_Mission_High_Level_Design_Catalog.md#ch05-m03-network-collapse) | [Chapter 5 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-5-citywide-command): `seq.ch05.m03.brief`, `.comms`, `.debrief` |
| M04 Last Corridor | [CH05-M04 Last Corridor](../Campaign_Mission_High_Level_Design_Catalog.md#ch05-m04-last-corridor) | [Chapter 5 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-5-citywide-command): `seq.ch05.m04.brief`, `.comms`, `.debrief` |
| M05 Command Node | [CH05-M05 Command Node](../Campaign_Mission_High_Level_Design_Catalog.md#ch05-m05-command-node) | [Chapter 5 sequence catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-5-citywide-command): `seq.ch05.m05.brief`, `.comms`, `.debrief`, `seq.ch05.close.protocol_fragment_05`, and epilogue family |

## Final Mission High-Level Phases

| Phase | Dramatic purpose | Gameplay purpose |
|---|---|---|
| 1. Approach | Reach the complex while the city remains under simultaneous pressure. | Route, logistics, air defense, and force composition. |
| 2. Separate The Network | Disable external weapons and infrastructure locks without collapsing civilian systems. | Confirmed objectives, G2A/G2G restraint, engineers, evidence. |
| 3. Breach | Enter the command perimeter and defeat the Vanguard core. | Combined arms, breach, production/reinforcement, vehicle preservation. |
| 4. Command Choice | Qassem attempts the override; ARIA exposes the audit and waits for explicit authority. | Bounded ARIA interaction and visible final priorities, not a surprise quick-time event. |
| 5. Recovery | Show the city consequence and governance outcome. | Result, rewards, Trust/Evidence/Infrastructure epilogue. |

## Canonical Ending

The full ending is guaranteed by Campaign completion:

- JRC and the Civil Authority stop the override.
- Qassem and the Ash Line command structure are defeated.
- Vanguard Brigade loses access to the Relay and its remaining forces withdraw or surrender according to later detailed content.
- ARIA releases the complete audit.
- The Commander rejects permanent unilateral control.
- The Civic Relay is restored only with bounded, transparent, shared authority.
- Sahrin begins visible recovery.

No star total, purchase, ad, Operations grind, or optional evidence is required to see these facts.

## Epilogue Variants

The ending uses one canonical resolution with three consequence emphases.

| Value | High-state epilogue | Low-state recovery cost |
|---|---|---|
| Trust | District councils and citizens accept shared command; Samira helps establish oversight. | Recovery begins under skepticism, protest, and a longer legitimacy repair process. |
| Evidence | Qassem's network, financiers, and proxy links are publicly documented. | Core guilt is proven, but parts of the support network remain contested or hidden. |
| Infrastructure | Power, roads, Fuel, clinics, shelters, and trade recover quickly. | Victory is followed by shortages, displacement, and a longer emergency transition. |

Low values are not hidden "bad endings" and do not remove the central truth. They make the player's operational style visible.

## Consequence And Reward Direction

- Every mission result previews how Trust, Evidence, and Infrastructure changed.
- Chapter completion grants durable Campaign recognition, Commander identity rewards, and the full Story Archive finale.
- Rewards follow canonical economy types and cannot bypass Operations consequence systems.
- Cosmetics may recognize different strengths but must not imply one paid identity is the canonical Commander.
- Future Operations content can begin from the player's epilogue emphasis while keeping the same canonical victory.

## Presentation Direction

- Tier B opening interlude rapidly revisits the five districts and their current condition.
- M01-M04 communications favor urgency without taking camera control from multi-front gameplay.
- M03 reveals the complete audit in readable stages rather than one exposition dump.
- M05 receives Tier A pre-assault and ending sequences, with gameplay carrying the central confrontation between them.
- Final images reflect actual Trust, Evidence, and Infrastructure outcomes using locations and characters already established.
- End credits or post-credits beat may tease future conventional/coastal conflict, but not at the expense of closure.

## Balance Direction

M01-M02 use upper Standard bands. M03-M05 use Mastery bands.

- Multi-objective pressure requires clear priority and travel information.
- No offscreen objective may fail without warning and recovery time.
- ARIA assistance reduces cognitive overload but never runs an unrestricted mission autopilot.
- The final mission should test decisions, not endurance through excessive duration or enemy count.
- Civilian and infrastructure consequences must follow visible agency.
- A required feature failure caused by pathing, boarding, automation, or UI ambiguity is an invalid run, not difficulty.

## High-Level Validation Questions

- Does the finale combine mastered systems rather than introduce new ones?
- Can the player understand every simultaneous objective and warning on mobile?
- Are Qassem's responsibility, ARIA's self-partition, and the Civic Relay purpose fully explained on the critical path?
- Does the Commander remain the decision-maker during ARIA support?
- Do civilians and infrastructure affect the ending visibly without gating the central truth?
- Is emergency authority returned to a bounded shared structure?
- Can future campaigns continue from this ending without invalidating player outcomes?
