# Codex lab results for Mountain Pass Base Assault aircraft startup

Branch `missions/ba-air-startup`. Unity 6000.5.8f1, Standard/Regular, English. Lab logs and raw captures are in `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ba-air-startup`. Earlier failure logs are in the sibling `author-ba-fix-cc-mp` directory.

## Failure and measurement

| Candidate | S052 | S054 |
| --- | --- | --- |
| Original player transport failure | `aria/unity.log:529765` | `aria/unity.log:626788` |
| Revised enemy helipad, preferred and actual (392,160) | `startup-S052.log:2017` | `startup-S054.log:1868` |
| Revised enemy transport failure | `startup-S052.log:2040` | `startup-S054.log:1891` |

The S052 native layout dump reproduced the revised startup at seed 130365. The buildings placed before either aircraft was allocated (`measure-S052.log:2490`, `:2513`). The allocator rejected the player transport’s requested cell (87,195) and placed it at (79,198) using the ground fallback (`:2562`, `:2588`). The earlier player failures logged a helipad origin of (151,152) on the S050 site (`aria/unity.log:529719`, `:626742`). At the nominal player pad center (1020,459.5), the 16 × 15 m typed mask sits within the helipad reservation at X 152–167, Z 152–166. That geometry suggests the same rejection. The original run captured only the failed member. Within each revision, S052 and S054 used identical air coordinates.

The enemy transport's 1 × 1 footprint at (400,167) passed the grid bounds, walkability, raw occupancy and typed AirReturn mask checks. The allocator rejected the cell because the helipad reserved it and the dynamic blocker bit was clear. All ground fallback cells failed admission (`measure-S052.log:2613`). The helipad also reserved the proven sibling fallback cell (393,170), and the typed pad excluded the next cell west at (392,170) (`:2665`, `:2691`). The allocator returned false, then startup reported `BlockedSpawn` (`:2717`, `:2742`). The dump completed after the startup failure (`:2848`, `:2863`).

The Base Assault frame is 245.32857 m across. The S058/S060 south-flank waypoint makes their frame 264.87494 m across. Projecting the nominal X 1261 m through the narrower frame gives the runtime X 1260.99988 m (`measure-S052.log:2613`). On the 1 m grid at X 860 this floors to cell 400. Subtracting half the helipad's 16-cell width requests origin X 392. At origin X 393 the even-width reservation begins at X 394, leaving the measured fallback strip at X 393. Lane 49 recorded the successful east origin and both aircraft for S058 (`2026-11-20/lane49/a/unity.log:431090`, `:431136`) and S060 (`:508616`, `:508662`), relative to the program log root. The opening actor rows record the cell (393,170) at (1253.5,470.5) for both siblings (`sibling-spawn.log:1`, `:2`).

## Draft changes

Both drafts use the measured native cell centers (947.5,495.5) and (1261.5,467.5) for their player and enemy helipad anchors and instance pads (`measure-S052.log:2562`, `:2639`). The two typed AirReturn rectangles retain their authored centers and 16 × 15 m dimensions. The placements still use Citywide Alert (`opmap.ch05.citywide_alert`), with source definition `Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset`, content hash `5c9233dca1ecae738c3883dcac6eaed33da51596bc8bb87613881794005a463d` and scene reference `9bef15c600f5d4f8da58222b9e8d08c0`. The existing grid is 440 × 220 at (860,300), with grid asset `61d7b7f9c413a41bea1cb7267696c46c` and surface asset `a04245b024f1940dc829f8eb6675c6ce`.

## Validation

S055 warm-up used the touch menu at 130366/en (`warmup-S055.log:679`, `:1477`). It recorded Victory / TicketsDepleted at 616.139 s, passed the result-screen return and completed the queue (`:11222`, `:11484`, `:11729`).

The draft checker passed S052 and S054 (`check-drafts.log:1`, `:2`).

| Mission | Re-authoring, 12 variants | Layout containment, 256 rows |
| --- | --- | --- |
| S052 | Passed (`author.log:1492`) | Passed, failures=0 (`layout.log:778`) |
| S054 | Passed (`author.log:1681`) | Passed, failures=0 (`layout.log:1122`) |

The batch markers are `author.log:1704` and `layout.log:1136`.

The smoke queue used the validation library and touch menu at 130365/en (`smoke.log:575`, `:56743`). Both missions started with a transport for each faction. The native helipad grants landed at (79,188) and (393,160) in both missions.

| Mission | Touch menu | Helipad origins | Initial roster | ARIA Play and Confirm |
| --- | --- | --- | --- | --- |
| S052 | `smoke.log:1294` | `:1950`, `:1973` | `:2019` | `:3000` |
| S054 | `smoke.log:57340` | `:57967`, `:57990` | `:58036` | `:59009` |

The first S052 gameplay failure was `AriaBlocked` on Attack during ObserveBattle (`smoke.log:56496`, `:56512`). The harness aborted with `ariaStopped:Blocked` at 500.467 match seconds (`:56562`, `:56587`).

S054 recorded Defeat / MainBaseDestroyed at 683.805 match seconds and passed the frozen result and touch return (`smoke.log:226564`, `:226804`, `:226876`). The queue recorded S052 Abort and S054 Defeat, with both attempts stored and the candidate unchanged (`:226992`, `:227007`, `:227022`). The archived opening actor rows locate the two transports at (79,198) and (393,168) for both missions (`smoke-opening-transports.log:1–4`).

The Editor also reported nonblocking canonical aircraft and measured rotor geometry failures on the unchanged warm-up and in both smoke launches (`warmup-S055.log:764`, `:782`, `smoke.log:659`, `:677`, `:56820`, `:56839`). Those checks flag stale aircraft geometry evidence. Visual review, full manual playthrough, and real player and device acceptance remain pending.
