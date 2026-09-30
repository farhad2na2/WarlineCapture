# Mission map binding ledger

| Item | Existing binding | Review candidate |
|---|---|---|
| Campaign mission ID | `saga.ch02.m02.supply_line` | Same |
| Scenario ID | `scenario.ch02.m02.supply_line` | Same |
| Logical map ID | `opmap.ch02.supply_yard_01` | `opmap.ch02.supply_line_refinery_review` |
| Physical map ID | `opmap.skirmish.desert_base_01` | `opmap.skirmish.refinerydistrict_prepared` |
| Physical semantic hash | `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c` | `2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778` |
| Logical definition | `Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_SupplyYard01.asset` | `Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02M02_RefineryReview.asset` |
| Runtime binding | Existing dense-city chapter binding | Prepared physical `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/RuntimeBinding.unity` |

The new logical definition consumes the prepared Refinery definition, surface,
grid, EntityScene and source binding. The mission builder regenerates mission
anchors and reuses the physical runtime scene; source binding validation expects
the physical scene identity. The original map asset remains for
rollback. The mission and scenario retain their IDs; the changed logical map ID
prevents an old launch payload from silently reinterpreting old coordinates.
Saved replay/resume behavior and packaged content still require validation.

## Power Relay review candidate

| Item | Existing binding | Review candidate |
|---|---|---|
| Campaign mission ID | `saga.ch02.m04.power_relay` | Same |
| Scenario ID | `scenario.ch02.m04.power_relay` | Same |
| Logical map ID | `opmap.ch02.power_relay_01` | `opmap.ch02.power_relay_refinery_review` |
| Physical map ID | `opmap.skirmish.desert_base_01` | `opmap.skirmish.refinerydistrict_prepared` |
| Physical semantic hash | `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c` | `2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778` |
| Logical definition | `Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_PowerRelay01.asset` | `Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02M04_RefineryReview.asset` |

The original logical definition remains for rollback. The candidate preserves
mission and scenario identities while changing the logical map ID so old
launch/resume coordinates cannot silently target the new sector. Saved-state
handling, packed content and normal-input acceptance are pending.

This managed worktree is a review lane. The production branch remains on the old
physical map until physical-map visual/device acceptance and the Supply Line
mission's normal-input/manual/ARIA acceptance gates pass.

## Airlift review candidate

| Item | Existing binding | Review candidate |
|---|---|---|
| Campaign mission ID | `saga.ch01.m04.airlift` | Same |
| Scenario ID | `scenario.ch01.m04.airlift` | Same |
| Logical map ID | `opmap.ch01.airlift_01` | `opmap.ch01.airlift_airfield_review` |
| Physical map ID | `opmap.skirmish.desert_base_01` | `opmap.skirmish.cityedgeairfield_prepared` |
| Physical semantic hash | `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c` | `e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22` |
| Logical definition | `Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_Airlift01.asset` | `Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01M04_AirfieldReview.asset` |
| Runtime binding | Existing dense-city chapter binding | `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity` |

The prepared source is reused with mission-owned anchors and a narrowly scoped
presentation socket for the decorative transport helicopter at the operational
pad. Source geometry is preserved; the scenario supplies the interactive
helicopter. Existing mission controls, objectives and passenger graph remain.
Saved-state handling, native journeys and human/device acceptance are tracked
in `Airlift/WORKSPACE.md`.

## Air Corridor review candidate

| Item | Existing binding | Review candidate |
|---|---|---|
| Campaign mission ID | `saga.ch04.m01.air_corridor` | Same |
| Scenario ID | `scenario.ch04.m01.air_corridor` | Same |
| Logical map ID | `opmap.ch04.air_corridor_01` | `opmap.ch04.air_corridor_airfield_review` |
| Physical map ID | `opmap.skirmish.desert_base_01` | `opmap.skirmish.cityedgeairfield_prepared` |
| Physical semantic hash | `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c` | `e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22` |
| Logical definition | `Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04_AirCorridor01.asset` | `Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04M01_AirfieldReview.asset` |
| Runtime binding | Existing dense-city chapter binding | `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity` |

Seventeen mission anchors consume the same prepared physical content. The
Airlift helicopter presentation override is absent. The original map remains
for rollback; other builders do not consume this mission's `MapPath`. English
manual and Persian ARIA full native journeys passed on final logical hash
`c7f3994aec248113199a64ea5e84c084188b90c4248b8c83ccc5e6a3a4aac8fe`.
The current-map saved Continue, rejected legacy-map launch, real radar-loss defeat
without rewards and fresh Retry through victory/return passed. Barracks origin
(515,597) clears the full 28×15 footprint plus service margin; western coverage
(630,610) is reachable and overlaps the northern defense. Human/device acceptance
and packaged device content remain pending.
See `AirCorridor/WORKSPACE.md` for exact evidence and failures.
