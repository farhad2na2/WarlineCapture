# Airfield Plains source feasibility for S097–S120

The Airfield Plains catalog group retains its AP identity and planned map ID `opmap.skirmish.airfield_plains`. CityEdgeAirfield can be referenced through the generic Skirmish definition and logical-binding path. Grounded Signal has no construction cells in its playable window. The air and combined-arms rows have a bilateral Airport source-fit gate on both sources.

## Source bindings

Both definitions use a 2048 × 1024 grid at a 1 m cell size with world origin `(0,0,0)`. Each definition references the `Grid.asset` and `Surface.asset` in its own directory. The XZ windows below are half-open.

| Source | OperationMap definition | Playable XZ | Campaign logical binding |
|---|---|---|---|
| CityEdgeAirfield | `Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset` | `[400,1000) × [300,700)` | CH01-M04 Airlift and CH04-M01 Air Corridor use `Configs/OperationMaps/Chapter01/OperationMap_Ch01M04_AirfieldReview.asset` and `Configs/OperationMaps/Chapter04/OperationMap_Ch04M01_AirfieldReview.asset` under `Assets/Game`. Both pin the same prepared source and scene. Protected S004 uses the `Resources/SkirmishS004OperationMap.asset` logical binding. |
| Grounded Signal | `Assets/Game/GeneratedOperationMaps/CH04M04GroundedSignal/Definition.asset` | `[450,975) × [525,690)` | CH04-M04 uses `MissionDefinition_Ch04_M04_GroundedSignal.asset`, `ScenarioSetup_Ch04_M04_GroundedSignal.asset` and `opmap.ch04.grounded_signal`. It has its own prepared entity scene, grid and surface derived from CityEdgeAirfield. |

| Source pin | CityEdgeAirfield | Grounded Signal |
|---|---|---|
| Operation map ID | `opmap.skirmish.cityedgeairfield_prepared` | `opmap.ch04.grounded_signal` |
| Source identity | `3702e8f11717773ce79fe38461c50cfeaf0caf86e9a62b0670166d9461aea5db` | `1ea8e142ea10236b0ec18dec14b50eb3ddf2e33341fb83ea401b94cfd544c5c6` |
| Source content hash | `e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22` | `70188ed3b2fd8c2819bd927810d084363615fe2795079620febdd42b4c28c810` |
| Runtime scene | `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity` (`915718fcf0cd7422db987b388cd0d925`) | `Assets/Game/Scenes/OperationMaps/CH04M04GroundedSignal/RuntimeBinding.unity` (`1df7b83e8dbcb483fb195a45fa9403c7`) |
| Physical ECS scene beside it | `PreparedEntities.unity` (`cd836ff2ddd2a440e81db4a20332d266`) | `PreparedEntities.unity` (`436aa193ca6964810ac18c51cf428952`) |
| Grid GUID | `7c338e0bc78684837a39eed484be918e` | `ffff4e6877b49449c867e56dc0bb2fc8` |
| Surface GUID | `8f750533817554c5e8ec1d550cdd0554` | `8915f0ba1b35648f0a5a859ae8a9f6f7` |

The precedent is S050 pointing to the Citywide Alert definition through `campaignMap`, with scene GUID and source hash pinned in its logical binding. S074 uses the same fields with `OperationMap_IndustrialBasin.asset`, a logical window onto the shared desert scene. Its current binding resolves to the shared desert source, despite the RefineryDistrict label in the work queue. S097 can point to the unchanged CityEdgeAirfield definition and use the same generic `SkirmishMissionAuthoringEntry` path. The option-2 draft-selected measurement commit `c1600712d` was outside both the lab branch and integration when the source was evaluated. The checked-in `RunExistingCampaignLayoutDumpAndExit` already hosts CityEdgeAirfield through S004.

## Airport capacity

Each physical source has one Campaign runway strip. CityEdgeAirfield's two runway scene meshes lie along the same centreline at Z = 560 m. Grounded Signal connects those spans for its transport-plane insertion. These scene entities supply no faction-owned production Airport. No `building.airport_small` asset or structure ID was present in the lab. W34 specifies a 120 × 40 m footprint for fighter and strike production. Current transport-plane production requires the full Airport.

The offline survey decoded both complete 1 m surfaces and required the `BuildingPlacement` bit in every cell of each footprint. It found zero origins on either baked grid for the 210 × 55 m full Airport or the proposed 120 × 40 m small Airport, including 90° orientations. This screen uses fewer constraints than the native placement validator. CityEdgeAirfield has 120,092 construction cells across its playable window, with 14,354 unrotated 28 × 15 m Barracks origins before native scenery, road, level and occupancy checks. Grounded Signal has zero construction cells in its playable window and zero Barracks-sized footprints anywhere on the baked grid. Its Campaign builder assigns movement permissions for infantry and vehicles along the recovery and extraction routes. See `baked-survey.log` and `baked-capacity.log` in `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/ap-airfield-feasibility`.

## Native layout survey

The S004 diagnostic uses the existing binding at seed 104733 in English. The cold launch loaded the CityEdgeAirfield runtime scene at `cityedge-layout.log:1875` and completed the dump at line 3430. The harness then failed the run with `candidateSourceChangedDuringRun` at line 3601 and exited 2. The warm run loaded the same scene at `cityedge-layout-warm.log:984`, completed at line 2393 and passed its final source fingerprint at line 2465.

The warm dump yielded 848 native placement candidates from 13,809 tested origins and orientations at 16 m stride. It recorded 27 west and 29 east Barracks candidates and 49 Helipad candidates per half. Its Airport fallback loaded the 210 × 55 prefab with a runway at line 2277 and returned zero candidates. All 16 selected rectangles for the two S097 compounds were present in both dumps. The sum includes 12 opening facility rectangles, the two oil-pump rectangles that contain the tower sites, and two optional helipads. See `cityedge-dump-comparison.log:1–5` and `s097-layout-design-warm.log:1–16`.

Both layout runs reported non-blocking aircraft dependency diagnostics. The first TransportHeli geometry pin mismatch is at `cityedge-layout.log:1414`, and the first rotor geometry mismatch is at line 1433. The warm equivalents are at `cityedge-layout-warm.log:527,546`.

## Objective feasibility

| Objective | CityEdgeAirfield ground opportunity | Remaining source-fit evidence |
|---|---|---|
| Base Assault, S097–S102 | Two measured seven-structure compounds flank three statically clear approaches. The S097 Barracks are 512 m apart. | Established reserves and the larger live rosters need separate capacity checks. S099–S102 also require bilateral aircraft facilities. |
| Frontline Control, S103–S108 | The static screen found separate 24 × 16 m clear rectangles on the west apron, middle service road and east apron. | Prove native access from both compounds, independent infantry approaches and contest space for the authored roster. S105–S108 have the Airport gate. |
| Breakthrough, S109–S114 | The static screen found a 24 × 24 m area on the east perimeter. Three paths cross the middle of the map. Grounded Signal has a scripted APC extraction corridor as a Campaign precedent. | Prove two defended corridors, two tower sites and the evacuation space beyond the approaches. The southern path makes bypass testing necessary for a corridor pair near the apron and service road. S111–S114 have the Airport gate. |
| Convoy Escort, S115–S120 | The west apron fits three separate 6 × 10 m rectangles and the east perimeter has a 24 × 24 m receiving-area candidate. | Check the bays against the actual truck footprint and live starts, and measure two truck routes, holding pockets, destination clearance, tower pads and repair access. S117–S120 have the Airport gate. |

Grounded Signal's movement-only surface prevents the starting Barracks and production facilities for all four objectives under the current source and opening rules. The FC, BT and CE rows remain source-fit candidates pending their objective measurements. The static site examples come from `ap-objective-terrain.log:2–8`.

## S097 Base Assault template

The draft binds CityEdgeAirfield and places the designated Barracks at `(446, 627.5)` and `(958, 627.5)` in XZ. The selected starting pads reserve all seven facilities per faction. Its Standard Regular setup uses the existing `G/F/BA` budget with a 1,080 s deadline, 450 Materials, 120 Oil and 350 usable Fuel per side. The opening combat force remains eight rifles, four rocketeers, one car and one armored APC per faction. The two optional Helipad sites support later transport placement and are outside the starting structure grant.

The apron approach is 521.5 m, the north service road flank is 498.9 m and the southern runway-edge flank is 603.4 m. The route check combined the native layout bitmap, exact movement and blocker grids, and proposed structure rectangles. It checked 6 m envelopes along the three waypoint paths. Each infantry and vehicle spawn rectangle has all 120 and 168 cells clear, respectively. These checks measure static travel; the live match is a separate verification. See `s097-layout-design-warm.log:17–25`.

The warm dump pinned in the draft has SHA-256 `09a8cc1d1511913dbf2334a471b1b3f77b91061b4afb57082b6b811141b1e389`. The draft check passed at `s097-check-draft.log:1`. The decoded surface and native bitmap pad preflight passed at `s097-pad-preflight.log:24`. The draft index passed all 89 drafts at `draft-index.log:61`.

`AuthorFromDraft` compiled 12 variants with 168 measured instance bindings at `s097-author.log:636` and passed at line 790. The authored candidate is registered in the expansion catalog. The library row is Planned and the publication is InProgress. The containment check passed all 168 rows at `s097-containment.log:655`, with the batch pass at line 669. The existing Burst `BC1016` diagnostic appears at `s097-author.log:352` and `s097-containment.log:347`.

## Standard Regular smoke at 130365 en

The single queue job dispatched at `s097-smoke.log:572`. The native touch-menu path passed at line 1316. The active ECS source readback passed at line 1856 with the CityEdgeAirfield identity and its 2048 × 1024 metre grid. Both designated Barracks appeared at their authored coordinates, and each faction had the expected combat force, two logistics vehicles and a watchtower at line 1879. ARIA watch started at line 2722.

ARIA won at 236.008 match seconds with the clock frozen, player Barracks at 800 HP and enemy Barracks at 0 HP (`s097-smoke.log:63687`). The result and touch return to the main menu passed with zero interventions at line 63927. The queue recorded one win with `sourceUnchanged=1` and exited 0 at lines 64127 and 64142. The traces and automatic captures are under `harness-evidence/smoke` in the task log directory.

This smoke finished at 21.9% of the 1,080 s deadline, below the BA packet's 40–70% pacing target. ARIA used every starting combat role. The runtime recorded automatic logistics use and remaining unused production roles at `s097-smoke.log:63669`. Full roster use, the prescribed seed and locale matrix, separate visual review, manual journeys, live War and LargeWar capacity, checkpoint and recovery, and real-player/device acceptance remain pending.
