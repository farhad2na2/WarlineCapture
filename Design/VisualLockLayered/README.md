# Warline Capture V3 Visual Lock

This folder contains the current bright premium military-command UI direction.
The V3 set uses sharp 90-degree rectangular construction, solid charcoal panels,
vivid functional color blocks, hard shadows, large mobile controls, and dry
inland settings. V1/V2 images may remain as history, but they are not current
targets.

The canonical screen-by-screen manifest is
[`V3_SCREEN_INVENTORY.md`](V3_SCREEN_INVENTORY.md). It lists 46 final PNGs:
17 previously approved finals and 29 finals added in the full-coverage pass.

## V3 Coverage

| Family | Final screens and states |
|---|---|
| First launch | Language choice, comic playback, commander identity, ARIA guidance |
| Shell | Splash/loading, main menu, commander profile, settings, pause |
| Campaign | Chapter select, mission select, mission briefing, loadout/squad prep |
| Match | Main HUD, transport passengers, tactical feedback, tutorial presentation |
| Match tools | Expanded ARIA, full tactical map, build drawer, disabled build drawer, placement confirmation, placement validity, unit command wheel, targeting state |
| Operations | Dashboard, district detail, threat alert, route preview, raid confirmation, resource exchange, end-of-day report, intel reveal, ARIA takeover |
| Progression | Armory, ability/upgrade detail, store, reward unlock, victory, defeat |
| Connected routes | Inbox, events, commander ranking, command feed |
| Other modes | Skirmish setup |

## Hard Rule

A target is accepted only when its exact canonical PNG exists under
`Design/VisualLockLayered/<SurfaceId>/reference/` and is listed in the manifest.
Do not build a surface from a prompt-only entry, archived target, screenshot crop,
or chat-only preview.
