# POP-05 Mission Result All Variants Target-Lock Prompt V01

Use the active `VisualLockLayered V15 3D Green-Screen Workflow` for target-lock references. This prompt defines the missing POP-05 result-state references. Do not generate implementation layers until the references are approved.

Surface id: `POP-05_MissionResult`
Canonical spec: `Design/WarlineCapture_Mission_Result_State_Spec.md`
Existing victory reference: `reference/POP-05_MissionResult_Landscape_Target.png`
Canonical size: `2400 x 1080`

General style:

- AAA mobile military RTS result/debrief screen.
- Full 3D single-map WarlineCapture direction.
- Middle-East town / forward command base / operation map visual language.
- Dark graphite and olive metal UI, compact command-base panels, gold only for positive/primary actions, warning amber/red for failure states.
- No old 2.5D isometric framing, no teal/cyan sci-fi skin, no loot chest/random reward presentation, no casual mobile style.
- Text should be readable and close to requested labels, but Unity implementation must use live text.

Shared layout for every variant:

- Top result header with outcome title, mission title, mode/source, duration, and difficulty.
- Left/center mission snapshot panel using 3D operation-map imagery.
- Objective/star panel with required objective rows and optional star goals.
- Performance stats panel.
- Rewards panel.
- Civilian/district consequence panel.
- Bottom action bar with state-specific buttons.

## Variant A - Partial Success

Create `reference/POP-05_MissionResult_PartialSuccess_Target.png`.

Header:

- Main title: `OBJECTIVE SECURED`
- Subtitle: `PRIMARY TARGET CAPTURED - CIVILIAN COST HIGH`
- Tone: successful but costly, gold mixed with amber caution.

Visible content:

- Two filled stars and one empty star.
- Objective rows: primary complete, civilians protected failed, losses low complete or warning.
- Stats: enemies defeated, units lost, civilians harmed, supplies spent.
- Rewards: reduced Commander XP, reduced Credits, Supplies recovered, Intel gained.
- Consequences: Civilian Safety down, District Trust down, Hostile Influence down, Infrastructure stable.

Buttons:

- `REPLAY`
- `ADJUST LOADOUT`
- Primary: `CONTINUE`

## Variant B - Defeat Failed

Create `reference/POP-05_MissionResult_Defeat_Target.png`.

Header:

- Main title: `OPERATION FAILED`
- Subtitle: `COMMAND SQUAD LOST BEFORE OBJECTIVE COMPLETE`
- Tone: warning amber/red, damaged command feed, no victory wings or celebration.

Visible content:

- Zero filled stars.
- Objective rows: primary objective failed, civilians protected warning/failed, extraction failed.
- Stats: enemies defeated, units lost, civilians harmed, resources spent.
- Rewards: participation XP only if configured, clear rewards locked/disabled, no first-clear reward.
- Consequences: Civilian Safety down, District Trust down, Hostile Influence up, Infrastructure damaged.

Buttons:

- `ADJUST LOADOUT`
- Primary: `RETRY OPERATION`
- `RETURN TO MAP`

## Variant C - Withdrawn

Create `reference/POP-05_MissionResult_Withdrawn_Target.png`.

Header:

- Main title: `FORCE WITHDRAWN`
- Subtitle: `COMMANDER ORDERED RETREAT - ASSETS RECOVERED`
- Tone: muted amber/olive, tactical withdrawal rather than failure celebration.

Visible content:

- No clear stars unless configured; show extraction/recovered rows.
- Objective rows: primary abandoned, command squad extracted, civilians unresolved.
- Stats: units extracted, units lost, cargo recovered, time in operation.
- Rewards: recovered Supplies/Fuel only, no clear reward, no first-clear reward.
- Consequences: Hostile Influence up, District Trust slightly down, Infrastructure stable or unknown.

Buttons:

- `REPLAY` if supported
- Primary: `RETURN TO MAP`
- `MAIN MENU`

## Variant D - Operation Resolved

Create only if needed after Partial Success is approved. It can reuse the Partial Success shell initially.

Header:

- Main title: `OPERATION RESOLVED`
- Subtitle: `DISTRICT ACTION COMPLETE`
- Tone: neutral command report, not a live-combat victory screen.

Buttons:

- Primary: `VIEW DISTRICT`
- `OPERATION REPORT`
- `CONTINUE`

Rejection rules:

- Reject if defeat/withdrawal uses victory wings or full-clear celebration art.
- Reject if reward rows look granted when the state only allows disabled or reduced rewards.
- Reject if civilian/district consequence rows are hidden.
- Reject if the screen looks like a different app from the existing victory target.
