# Supply Line map binding ledger

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

This managed worktree is a review lane. The production branch remains on the old
physical map until physical-map visual/device acceptance and the Supply Line
mission's normal-input/manual/ARIA acceptance gates pass.
