# CH02-M02 Supply Line: RefineryDistrict review candidate

Updated 2026-10-01. This managed checkout isolates the mission candidate from `main`.
The logical map `opmap.ch02.supply_line_refinery_review` uses the prepared
RefineryDistrict physical map at semantic hash
`2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778`.
The original Supply Yard map asset remains available for rollback.

## Layout

| Role | X,Z | Decision |
|---|---:|---|
| Working oil pump | 573,520 | Inside the refinery's south fence, north of the road |
| Pump service gate | South fence, around X=581–601 | One 20 m opening aligned with the truck approach |
| Working refinery | 740,475 | South of the main road within the logistics yard fence |
| Emergency reserve | 941,540 | East depot area |
| Oil hauler | 580,505 | Main road approaching the new pump gate |
| Fuel hauler | 765,505 | Main road approaching the refinery |
| Alternate lane | 660,460 | Southern logistics route; native truck arrival passed |

The earlier pump at (560,510) was beside the road and sidewalk. The map generator
now opens a single service gate in the south fence; it does not duplicate or
hand-edit fence scene records. The earlier 14 m gate failed a two-cell vehicle
clearance route once the working pump footprint was included. The final 20 m
gate passes the generated-grid route from (580,505) to the pump service side at
(591,520). `CH02M02SupplyLineConfigBuilder` checks this on every mission rebuild.

The refinery previously crossed the authored fence at Z=516. Its current
footprint is clear of that fence. The prepared fence segments themselves were
contiguous in the source inventory; the visible overlap came from mission
building placement. The original prototype was not declared defective on that
evidence.

## Current review preparation

The final English ARIA journey passed 40 completed native tanker deliveries,
actual southern-lane arrival, the 20-barrel civilian allocation, a 20-second
full-reserve hold, debrief, result and ordinary Campaign return. Its full log is
`Logs/supply-line-review-watch-11.log.gz`; native images are under
`Evidence/20261001-180302-en-aria`. The Persian independent manual journey passed at 630,506 active milliseconds,
with ARIA off, the same actual delivery/arrival/hold facts, the corrected
mission-specific result objectives and ordinary Campaign return. Its full log is
`Logs/supply-line-review-manual-05.log.gz`; images are under
`Evidence/20261001-181939-fa-IR-manual`. The mission is ready for human Editor
review. Human acceptance and real device acceptance remain pending.

The refinery route uses a finite, once-only 450-barrel operating allowance and
an authored 500-barrel depot capacity. Starting Fuel does not grant delivery
credit: the objective requires 40 newly refined barrels actually unloaded.
Completed unloads are counted independently of simultaneous Fuel spending.
The visible counter updates during mission guidance and Select/Move cues.
The 15-minute deadline and 13-minute time star match the longer industrial route.

Three native ECS detour checks and 31 resource-hauler rule checks passed.
Player reroutes use hauler identity after the automatic haul order is removed;
arrival, not order acceptance, recovers the prepared-map lane. Legacy-map
completion behavior and the Steel Push/Split Front 200-barrel depot budgets
remain explicitly scoped. Other retained mission gameplay has not been rerun.

## Historical evidence and remaining gates

- Prepared candidate regeneration passed with 217 independent owners:
  `Evidence/supply-line-pump-candidate-02.log`.
- Packed content build passed at the current hash:
  `Evidence/supply-line-pump-packed-03.log`.
- Mission checkpoint, rules, configuration, presentation and truck-access guard
  passed: `Evidence/supply-line-pump-checkpoint-02.log`.
- Native mission load and all three exact building origins passed in
  `Evidence/supply-line-pump-watch-final.log`.
- [Native pump and gate view](native-pump-gate.png) is a diagnostic world-only
  camera capture from the playable Editor, framed for layout inspection.
- Full ARIA normal-input completion **failed** in that run. The initial Editor
  Game View was narrower than the ARIA button's canvas coordinates. Expanding
  the Game View allowed a real click to start ARIA, but it became `Blocked`
  after 66 actions before the mission outcome. The log is retained as failed
  evidence, not a pass.
- At that historical checkpoint, manual-control full mission, result and return, renewed packed native route
  suite at this physical hash, visual review, and Android/device acceptance
  were pending. Earlier Native21 route proof belongs to the previous map hash.

No new fuel or reserve gameplay button is in the generated HUD. The first 20
stored Fuel barrels are protected automatically for civilians.
