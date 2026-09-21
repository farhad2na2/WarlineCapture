# All 120 Skirmish programming briefs

**Status: Planned.** Twenty map/objective packets each contain six individually specified army/start variants. The [handoff](../IMPLEMENTATION_HANDOFF.md) explains the distinction between work ordinals 4–120 and stable S-IDs. Read the shared architecture, objectives, roster/economy and map contracts before the assigned packet.

| Map | Base Assault | Frontline Control | Breakthrough | Convoy Escort |
|---|---|---|---|---|
| Desert Base | [S001–S006](DB_BA.md) | [S007–S012](DB_FC.md) | [S013–S018](DB_BT.md) | [S019–S024](DB_CE.md) |
| City Crossroads | [S025–S030](CC_BA.md) | [S031–S036](CC_FC.md) | [S037–S042](CC_BT.md) | [S043–S048](CC_CE.md) |
| Mountain Pass | [S049–S054](MP_BA.md) | [S055–S060](MP_FC.md) | [S061–S066](MP_BT.md) | [S067–S072](MP_CE.md) |
| Industrial Basin | [S073–S078](IB_BA.md) | [S079–S084](IB_FC.md) | [S085–S090](IB_BT.md) | [S091–S096](IB_CE.md) |
| Airfield Plains | [S097–S102](AP_BA.md) | [S103–S108](AP_FC.md) | [S109–S114](AP_BT.md) | [S115–S120](AP_CE.md) |

Each packet is a bounded authoring assignment once its shared dependency tickets pass. [117 remaining work items](../WORK_QUEUE_004_120.csv) preserve S-IDs; [all 120 definitions](../IMPLEMENTATION_MANIFEST.csv) include prototype expansion recertification. [360 setup rows](../INITIAL_SETUP_MATRIX.csv) are deterministic planning vectors, not runtime acceptance.
