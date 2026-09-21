# Skirmish Game View evidence

Programmer 1 dumps Regular Standard Game View captures here on the Windows
Skirmish shadow (`D:\Projects\WarlineCapture-Skirmish`).

## S002

Required for the guarded Playable flip:

| File | Source |
|---|---|
| `s002-regular-standard-104731-playing.png` | `Tools/Warline/Skirmish/Launch S002 Regular Standard Game View` |
| `s002-regular-standard-104731-gameview.json` | Written by the same probe (`forcedVictory=0`) |

A copy is also written under
`Design/AgentReports/SkirmishExpansion/S002/_Evidence/`. Either folder
satisfies the S002 flip. Focused Editor tests never persist Playable.

## S003

| File | Source |
|---|---|
| `s003-regular-standard-104732-playing.png` | `Tools/Warline/Skirmish/Launch S003 Regular Standard Game View` |
| `s003-regular-standard-104732-gameview.json` | Written by the same probe (`forcedVictory=0`) |

A copy is also written under
`Design/AgentReports/SkirmishExpansion/S003/_Evidence/`. Either folder
satisfies the S003 flip. That flip writes only the S003 row. S002 stays
Playable. The checked-in manifest leaves S003 InProgress until the confirm
write.
