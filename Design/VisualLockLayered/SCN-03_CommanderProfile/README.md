# SCN-03 Commander Profile Visual Lock

Status: Target-lock mockup generated. Layer pack not generated yet.
Date: 2026-05-23

## Active Target

- Reference target: `reference/SCN-03_CommanderProfile_Landscape_Target.png`
- Prompt source: `prompts/SCN-03_CommanderProfile_TargetLock_V01.md`
- Canonical size: `2400 x 1080`

This is the full Commander Profile screen opened from the Main Menu `Commander` route. It is not the small Main Menu commander-side panel.

## Route

```text
SCN-02 Main Menu
  -> Commander
  -> SCN-03 Commander Profile
  -> Open Armory
  -> SCN-19 Armory
```

## Screen Purpose

- Show commander identity, title, portrait, level, and XP progress.
- Show profile overview stats and recent history.
- Show commander reward track and claimable rewards.
- Provide the clear `Open Armory` route into SCN-19 for roster inspection.
- Keep profile tabs local to this screen: Overview, Upgrades, History, Badges, Stats.

## Implementation Notes

- This is a target-lock mockup only; do not flatten it into Unity.
- All labels, values, counters, reward states, tab names, and recent history rows must be live UI text.
- `Open Armory` routes to `SCN-19 Armory`.
- `Edit ID` routes to commander identity editing / `POP-11` when available.
- The next step, after approval, is to request proper green-background separated layers using the active V15 workflow.
