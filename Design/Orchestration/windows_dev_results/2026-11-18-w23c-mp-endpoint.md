# Windows dev results — W23c Mountain Pass BT S061–S066 / CE S067–S072: endpoint past the defenses, tappable (2026-11-19)

Branches `missions/win-w23-mp-bt-exit` and `missions/win-w23-mp-ce-dest`, both merged with integration `93ab7906d` (BT `44ee65392`, CE `076284dd6`, no conflicts). CE side: `2026-11-18-w23c-mp-ce.md` on the CE branch.
The Windows dev lead did this task. Read-only analysis plus offline scans; no Unity run, no draft change, no shared code.

**Outcome: stopped at step 4.** On the current Mountain Pass source (Citywide Alert, host S050), neither an endpoint past both corridors (step 2) nor moved corridors (step 3) fit together with:
- the per-edge tap margins;
- the W23 30 m distance check;
- defenses covering the corridors.

No draft was changed. The W23b drafts and r4 measurements stay as they are, unaccepted.

## 1. Tap margins per edge (step 1)
Derived from the camera clamp and the HUD rects with a geometry script that copies the clamp (`codex\notes\w23c_edge_margins.py`), not from a Unity run.

**Camera on S050:**
- The view profile has pitch 65, yaw 35, FOV 46 (`Assets/Game/Configs/SkirmishExpansion/Scenarios/S050/Logical/SkirmishMissionLogicalBinding_S050.asset:2155-2159`). The code caps pitch at 50 (`SkirmishCameraFraming.cs:38-44,84-90`).
- Heights (camera Y):
  - 28.69 by default;
  - 42.18 after any base focus (`SkirmishPresentationSystem.cs:228`);
  - 40 on mobile (`SelectionRuntimeConfigStartupSystemHelper.cs:17,208`).
- The objective Focus button only pans (`UiShellEcsGateway.PublicObjective.cs:104`).
- Yaw 35 means north is not up: screen-right is world (0.82, −0.57).
- Camera bounds are x 860–1300, z 300–520, and the playable area is 8 m inside that (`Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset:26-31`).
- The clamp keeps the camera's ground-footprint box inside the bounds. At 42.18 that box is 118×112 m.

**HUD at 1920×1080:**
- The measured open play area is x 457–1444, y 241–855 (`SkirmishS002CameraRetestTests.cs:28`).
- Left: squad panel and ARIA column, x ≤ 459 (`MissionHudTouchLayoutView.cs:55-59`).
- Bottom: squad tray (y ≤ 241) and command rail (y ≤ 202).
- Top: objective strip, y ≥ 855 (`SkirmishMatchView.cs:56-58`).
- Right: blocked from x 1444. This edge is known only from the measured play area.
- The objective outline does not block taps (raycastTarget=false).

**Margin from the camera bound that the tap point (area centroid) needs:**

| Edge | 28.69 | 40 (mobile) | 42.18 | Used (+10 %) | From the playable edge |
|---|---|---|---|---|---|
| East (x 1300) | 34 | 47 | 49 | **54** | 46 |
| North (z 520) | 33 | 46 | 48 | **53** | 45 |
| West (x 860) | 11 | 15 | 16 | **18** | 10 |
| South (z 300) | 13 | 18 | 19 | **21** | 13 |

- **Tap box.** The centroid must lie within x 878–1246 and z 321–467. Near corners the margin rises to 70–78 m. On a 2400×1080 phone, east and north grow to ~58–69 m.
- **Check against play results.** W23 MP (1271,446) is 29 m from the east bound and failed. W23b MP (1082,450) is 70 m from the north bound and worked. CC W23b on S026 also worked.

## 2. Step 2: no endpoint past both corridors
**The map is open over its full depth.** The band z 412–436 is a highway: flag 17 = road + not building-level, and the z 428 row is road. Measured routes cross it along the map's length. Examples:
- W23b, from (1046,354) to (1082,450): flood 110 m against a 102 m straight line;
- W23, from (1186,354) to (1271,446).

So a column can go round Corridor A (x 1092–1163, z 387.5–407.5) and Corridor B (x 1043–1140, z 337–363.5) by the highway or the north lane (z 440–470). That is how the W23b columns reached the exit.

**Scan.** Clear (road/sidewalk/clear) 16×16 and 12×12 rectangles with the centroid in the tap box, and every enemy structure, staging/rally anchor and defense tower at least 30 m away. Output: `codex\notes\endpoint-scan-w23c.txt`.

| Drafts | Eastmost passing rectangle | Nearest enemy item |
|---|---|---|
| Field S061/S063/S065 | (1104..1120, 440..456) | tower b 30.5 m |
| Ground established S062 | (1088..1104, 440..456) | satellite dish 31.0 m |
| Air/combined established S064/S066 | (1096..1112, 332..348) | fuel bladder 32.0 m |

- None of these is past the corridors. The field and ground spots sit in the north lane, which no corridor crosses. The air/combined spot overlaps Corridor B.
- East of the corridors (x ≥ 1164), the best tappable clear spot is 13 m from an enemy structure (the refinery).
- The enemy base fills x 1100–1270 between z 330 and 470.

## 3. Step 3: moving the corridors does not work
- **Corridors would have to cut the whole map depth.** Because the map is open over its full depth, any corridor pair a column must cross would have to span all ~170 m of passable depth (z ~308–476) in a band between the player start (base x ≤ 1016) and the endpoint (x ≥ ~1068). Corridors are two rectangles, so they would have to stack across that band, roughly x 1020–1066.
- **That band is the player's base.** It holds the player refinery (1072.5,387.5; 25×15), fuel bladder (1043,337) and fabrication depot (1093.5,364). A corridor there overlaps blocked building cells.
- **Defenses can't cover it legally.** Tower b is about 90 m away. To cover the band, the enemy towers would have to stand inside the player base, and they would also fall within 30 m of the endpoint, against the W23 check.
- **Not tried:** a corridor around the endpoint, or a corridor containing it. Two rectangles cannot close the W, E and S approaches to the north lane, and they would still be ≥55 m from any legal tower position.

## 4. Step 4: what fails, and existing sources that may fit
**What fails on Citywide Alert:**
- The camera area is 440×220 m; the playable area is 424×204 m.
- The tap box leaves 368×146 m for centroids.
- The enemy base occupies the east 170 m of it, and the highway band joins the north and south halves along the whole length.
- The other CH05 sources share these exact bounds (x 860–1300, z 300–520): Network Collapse, Last Corridor and Command Node (`.../CH05M0{3,4,5}*/Definition.asset`). So they have the same depth limit; not assessed further.

**Existing Campaign sources with more room (bounds from each `Definition.asset`; none measured for BT/CE yet):**

| Source | Camera bounds | Note |
|---|---|---|
| Armor Break (CH04-M05) | x 740–2020, z 260–780 (1280×520 m) | The policy's first source for Skirmish MP ("Existing Armor Break approach sector"). |
| Trust Under Fire (CH05-M02) | x 400–1000, z 300–700 (600×400 m) | — |
| City Crossroads (S026 host, shared desert/dense-city world) | playable 400×615 m | CC BT S037–S042 now wins 6/6 with a tappable exit (W23b). |
| Shared desert world, CityEdgeAirfield, RefineryDistrict, AshLinePort | 2048×1024 m worlds | Sector to be chosen. |
| Grounded Signal (CH04-M04) | camera x 420–995, z 500–700 (575×200 m) | Too shallow; same problem. |

The source choice is the lead's call. No source was changed.

## 5. ARIA stops from the W23b logs (diagnosis, not fixed)
**S061 Blocked 115** (`win-dev-unity\w23b-mp-bt\menu\unity.log`):
- ARIA chose `Move` for group 19 with intent `OpenObjectiveCorridor` at 124.7 s while the goal was not in view (`inView=False ground=False`).
- It tapped the ground three times (134.0 / 141.9 / 145.6 s), but none was accepted (`accepted` stayed 2).
- Then `order_retry` at 155.5 and 207.5 s.
- Then `[AriaBlocked] reason=115 kind=Control attempts=3` and `[AriaBlockedControl] control=Move moveMode=False selectedGroups=1 selectedMask=4 breacherStage=2`.
- Reading: the Move control press did not arm move mode for the selected group three times in a row, so the input layer stopped ARIA. The corridor centroids are inside the tap box: A (1127.5,397.5), B (1091.5,350.3). So this is not the edge problem. It looks like an input or selection issue in ARIA (shared code), not draft data.

**S068 Blocked 101:** see `2026-11-18-w23c-mp-ce.md` on the CE branch.

## Open
- **Mountain Pass BT/CE source fit:** the lead's decision. Options: move the MP families to Armor Break or another source, or relax the 30 m check or the corridor rule.
- **S061 Blocked 115:** ARIA Move control (shared code, needs the owner's OK).
- **S068 Blocked 101:** CE branch.
- The W23b MP drafts stay unmerged.
