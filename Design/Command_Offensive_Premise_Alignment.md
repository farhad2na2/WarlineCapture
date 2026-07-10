# WarlineCapture Command Offensive Premise Alignment

Date: 2026-05-21

2026-07-10 narrative amendment: the active Campaign uses the local Daryat JRC, the Ash Line terrorist network, and later Vanguard Brigade military escalation defined in `Campaign_Narrative_Bible.md`. Civilian appearance is never a hostility rule.

## Purpose

This document aligns WarlineCapture's player fantasy around an active field commander preparing and executing targeted operations against fictional hostile terrorist and insurgent cells embedded in civilian towns and districts.

The 2026-05-21 direction change updates the product target to full 3D single-map gameplay and command-base menu art. It does not change existing runtime implementation by itself, Chapter 1 mission order, ARIA design, or Operation systems. It changes the design framing so existing systems read as proactive command operations rather than only city stabilization.

## Updated Player Fantasy

WarlineCapture is a mobile 3D command RTS about preparing and executing precision operations in a fictional Middle Eastern city where armed hostile cells exploit infrastructure, routes, compounds, compromised logistics, and populated districts.

The player fantasy is:

```text
Read the city.
Identify the hostile faction's position.
Prepare the right force.
Deploy into the same 3D operation map.
Strike with tactical control.
Protect civilians and infrastructure.
Live with the district consequences.
```

The player is not only reacting to attacks. The player is a field commander who chooses when to patrol, raid, breach, intercept, reinforce, evacuate, or hold based on intel and district risk.

## What Changes

The product language should shift from passive stabilization toward proactive operations:

| Previous Emphasis | Updated Emphasis |
|---|---|
| Stabilize a city under attack. | Prepare and execute targeted operations in a contested city. |
| Keep the city alive while fighting. | Strike hostile factions while protecting civilians and infrastructure. |
| District recovery. | District consequence from tactical command choices. |
| Respond to threats. | Use intel, routes, and timing to choose the right operation. |

Civilian safety remains central. The difference is that civilians are the constraint and consequence layer around offensive command, not the only fantasy.

## What Does Not Change

Do not change these unless a separate implementation task explicitly requires it:

- Existing gameplay systems.
- Existing UI visual targets until a UI-specific task updates them.
- M01 First Contact teaching goal.
- Chapter 1 mission sequence.
- The existing code-level tactical simulation foundation.
- Large-scale grid movement design, now interpreted through the 3D single-map direction.
- ARIA as assistant/advisor, not commander.
- District metrics: trust, security, infrastructure, enemy influence, intel, heat, civilian risk.
- Monetization guardrails and reward economy.

## Design Pillar Adjustment

The pillars should be interpreted through an offensive-command lens:

| Pillar | Updated Meaning |
|---|---|
| Tactical Command | The player prepares and executes squad, move, attack, build, transport, support, and breach decisions on one large 3D operation map. |
| Hostile Factions In Civilian Space | Fictional terrorist and insurgent cells use districts, routes, infrastructure, compounds, and compromised civilian systems to create tactical and ethical pressure. Civilian models remain civilians; hostility requires confirmed conduct, weapons, Intel, and objective context. |
| Precision Under Constraint | Winning is not enough; sloppy attacks damage civilians, trust, infrastructure, intel quality, or future operation safety. |
| District Consequence | Raids, patrols, defenses, evacuations, and breaches change district state after the mission. |
| Readable 3D Mobile RTS | The player can understand hostile position, route risk, civilian risk, objective state, camera state, and next command at mobile landscape scale. |
| Fair Progression | Unlocks and rewards support preparation and identity, not pay-to-win tactical outcomes. |

## Mode Framing

| Mode | Updated Role |
|---|---|
| Campaign | Teaches the commander's operation toolkit through authored 3D missions: first contact, forward post, radar response, airlift, breach. |
| Operations | Lets the player decide where and how to pressure hostile cells across districts while managing public trust, intel, heat, and civilian risk. |
| Skirmish | Lets the player rehearse, stress-test, and replay 3D operations with configurable AI and economy knobs. |

## Mission Framing Rules

Every authored mission should answer:

- Which hostile cell, node, convoy, compound, or network element is the target?
- Why is this district tactically important?
- What intel does the commander have before launch?
- What is the intended operation type: patrol, raid, intercept, defense, evacuation, breach, airlift, repair, or convoy?
- What civilian/infrastructure constraint shapes the attack?
- What happens to district state after success, partial success, or reckless execution?

## Terminology Guidance

Prefer:

- hostile faction
- hostile cell
- terrorist cell
- insurgent network
- hidden network
- embedded hostile force
- suspected node
- fortified node
- district raid
- patrol intercept
- breach assault
- command operation
- collateral risk
- civilian risk
- intel confidence

Avoid naming real-world armed groups, governments, or conflicts in general docs unless a specific narrative doc intentionally defines them. The design works better with fictional hostile factions and cells because it keeps the tone premium, flexible, and less politically brittle.

## README Language

The root README should describe WarlineCapture as:

```text
a mobile-first 3D command RTS about preparing and executing operations against fictional hostile cells embedded in civilian towns, while protecting civilians, infrastructure, and long-term district stability.
```

This language preserves existing systems:

- base building
- tactical combat
- district consequences
- Operation actions
- hidden network/intel
- civilian risk
- Campaign/Operations/Skirmish modes

## FTUE Language

M01 should still teach selection, move, attack, objectives, and result. The player-facing framing should become:

```text
First hostile contact has been confirmed near a civilian corridor. Select your response squad, move to cover, and neutralize the patrol before it escalates.
```

This is a framing update only. It does not add new FTUE steps.

## Acceptance

This premise alignment is accepted when:

- README and `Design/README.md` point to this document.
- The north-star doc frames the player as a proactive field commander.
- FTUE premise text supports targeted operations against hostile factions embedded in civilian districts.
- Existing gameplay stays intact until implementation tasks update it, while UI visual targets can move to the command-base style through dedicated UI work.
- Civilian safety and district consequence remain explicit constraints on offensive action.
