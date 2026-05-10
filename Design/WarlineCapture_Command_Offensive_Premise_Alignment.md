# WarlineCapture Command Offensive Premise Alignment

Date: 2026-05-08

## Purpose

This document aligns WarlineCapture's player fantasy around an active field commander preparing and executing targeted operations against hostile factions embedded in civilian districts.

It does not change existing gameplay implementation, UI visual targets, Chapter 1 mission order, tactical-map direction, ARIA design, or Operation systems. It changes the design framing so existing systems read as proactive command operations rather than only city stabilization.

## Updated Player Fantasy

WarlineCapture is a mobile tactical RTS about preparing and executing precision operations in a civilian city where hostile factions hide among infrastructure, crowds, routes, and district systems.

The player fantasy is:

```text
Read the city.
Identify the hostile faction's position.
Prepare the right force.
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
- Existing UI visual targets.
- M01 First Contact teaching goal.
- Chapter 1 mission sequence.
- Strategic/tactical map split.
- Large-scale grid movement design.
- ARIA as assistant/advisor, not commander.
- District metrics: trust, security, infrastructure, enemy influence, intel, heat, civilian risk.
- Monetization guardrails and reward economy.

## Design Pillar Adjustment

The pillars should be interpreted through an offensive-command lens:

| Pillar | Updated Meaning |
|---|---|
| Tactical Command | The player prepares and executes squad, move, attack, build, transport, support, and breach decisions. |
| Hostile Factions In Civilian Space | Enemies use districts, routes, infrastructure, and civilian cover to create tactical and ethical pressure. |
| Precision Under Constraint | Winning is not enough; sloppy attacks damage civilians, trust, infrastructure, intel quality, or future operation safety. |
| District Consequence | Raids, patrols, defenses, evacuations, and breaches change district state after the mission. |
| Readable Mobile RTS | The player can understand hostile position, route risk, objective state, and next command at mobile landscape scale. |
| Fair Progression | Unlocks and rewards support preparation and identity, not pay-to-win tactical outcomes. |

## Mode Framing

| Mode | Updated Role |
|---|---|
| Saga Campaign | Teaches the commander's operation toolkit through authored missions: first contact, forward post, radar response, airlift, breach. |
| Persistent Operation | Lets the player decide where and how to pressure hostile factions across districts while managing public trust, intel, heat, and civilian risk. |
| Quick Custom Game | Lets the player rehearse, stress-test, and replay tactical operations with configurable AI and economy knobs. |

## Mission Framing Rules

Every authored mission should answer:

- Which hostile faction or cell is the target?
- Why is this district tactically important?
- What intel does the commander have before launch?
- What is the intended operation type: patrol, raid, intercept, defense, evacuation, breach, airlift, repair, or convoy?
- What civilian/infrastructure constraint shapes the attack?
- What happens to district state after success, partial success, or reckless execution?

## Terminology Guidance

Prefer:

- hostile faction
- hostile cell
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

Avoid broad, real-world loaded labels in general docs unless a specific narrative doc intentionally defines them. The design works better with fictional hostile factions and cells because it keeps the tone premium, flexible, and less politically brittle.

## README Language

The root README should describe WarlineCapture as:

```text
a mobile-first tactical RTS about preparing and executing command operations against hostile factions embedded in civilian districts, while protecting civilians, infrastructure, and long-term district stability.
```

This language preserves existing systems:

- base building
- tactical combat
- district consequences
- Operation actions
- hidden network/intel
- civilian risk
- Saga/Operation/Quick Custom modes

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
- Existing gameplay and UI visual targets remain unchanged.
- Civilian safety and district consequence remain explicit constraints on offensive action.
