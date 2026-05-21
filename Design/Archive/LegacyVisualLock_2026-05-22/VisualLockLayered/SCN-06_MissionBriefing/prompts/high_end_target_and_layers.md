# SCN-06 Mission Briefing High-End Layer Prompt

Use case: ui-mockup
Asset type: AAA mobile RTS Mission Briefing target plus separated implementation layer atlas.

Primary request:
Create a high-end WarlineCapture Mission Briefing landscape UI target at 1672 x 941, matching the existing premium dark graphite/cyan/gold HUD style and premium 2D isometric mission art direction.

Reference:
Use `reference/SCN-06_MissionBriefing_Landscape_Target.png` for layout density and chrome quality only. Do not preserve `3-6 Downtown Breakthrough`.

Canonical content:
- Header: MISSION BRIEFING
- Mission label: 1-5 BREACH ASSAULT
- ScenarioSetup: scenario.ch01.m05.breach_assault
- Level / Map: level.ch01.fortified_node_01
- Briefing: breach the fortified hostile node, destroy the enemy core, protect civilian control.
- Objectives: Breach outer gate 0/1; Destroy enemy core 0/1; Keep vehicle alive 0/1
- Star goals: Complete mission; Use breach route; Finish under 9:00
- Enemy intel: Defensive Garrison High; Armor Medium; Air Low
- Reward preview: CommanderXP, Credits, GearModule, UnitUnlock or BlueprintParts
- CTA: START MISSION

Layer atlas request:
Create a separate clean layer atlas on flat chroma key. The atlas must separate:
- shell frame and fill
- header frame and back button
- mission key art without frame or text
- mission image frame
- briefing panel frame/fill
- objectives panel frame/fill
- star goals panel frame/fill
- enemy intel panel frame/fill
- reward strip frame/fill
- reward tile backgrounds
- enemy intel tile backgrounds
- start mission button normal/pressed backgrounds
- icons: objective target, shield, breach/flag, star, infantry/garrison, armor, air, CommanderXP, Credits, GearModule, UnitUnlock, BlueprintParts

Layer rules:
- No reusable layer may contain text.
- Mission key art must not contain UI frames or text.
- Reward and intel icons must be standalone transparent PNGs.
- Button and tile backgrounds must be 9-slice compatible.
