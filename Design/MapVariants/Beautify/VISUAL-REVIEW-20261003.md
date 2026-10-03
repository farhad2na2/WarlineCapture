# Map beautification visual review — 2026-10-03

The detailed native visual pass is built across all five maps. The approved mockups remain the direction; these implemented renders have not yet received visual acceptance. The user explicitly deferred mission playtesting until the look is accepted.

## Implemented visual pass

- Refinery, airfield, port and Frontier: world-space sand texture; weathered paving; cracks and stains; service and bay paint; denser low verge dressing and cargo vignettes; warm sun and ambient lighting; saved runtime-binding atmosphere.
- Port container layout retained. Frontier regrouped into blocks and lanes as approved; it has no bound missions.
- Dense City: flat asphalt over the former dirt road visuals, tiled kerbs, surface wear and lane paint, warmer ground, rubble and paper debris, low verge cover, colored market fabric and baskets, rooftop shades and owner-linked laundry. All 18 mission cameras have distinct asset-based filenames.
- Removed oversized polygon ground stamps after native review. Corrected the canopy fit and cloth face normals. Prior rejected images and logs are retained in per-map Evidence folders.

## Evidence gates

| Gate | Result |
|---|---|
| Current scripts compiled in live worktree Editor | Passed; stale-assembly execution is refused |
| Refinery, airfield and port semantic pins | Unchanged, asserted in each build |
| Variant classification and deterministic replay | Passed |
| Dense City building/surface authoring and 18 mission definition file hashes | Unchanged in the main visual build |
| Later city fabric and street decoration changes | Owner state unchanged; final tuning is material-only |
| Native captures | 35 saved at 1600 × 900 |
| Visual review / mockup-quality acceptance | Pending; surfaces and lighting are closer, but city focal dressing and vegetation still need a richer authored treatment |
| Normal-input ARIA / result / return playthrough | Deferred by user; no readiness claim |
| Runtime content rebake / latest packed content / device and mobile performance | Pending after visual acceptance |

These are native art renders using the temporary PC Premium shadow profile. The Editor returns to Match with Mobile_RPAsset restored. They are not screenshots of a played mission or proof of mobile performance. Scenes and generated art remain uncommitted pending visual acceptance.

## Semantic pins

- RefineryDistrict: `2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778` (unchanged)
- CityEdgeAirfield: `e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22` (unchanged)
- AshLinePort: `3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5` (unchanged)
- Frontier: `f719b0b55cde5cabbff5413a4910522962e96276eba3cb8738ed4eabb47c22bf` (approved new layout)

## Mockup comparisons

### Refinery / Supply Line

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/SupplyLine/supplyline-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-zoom-refinery.png) |

### Airfield

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/CityEdgeAirfield/airfield-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/CityEdgeAirfield/after-OperationMap_Ch01M04_AirfieldReview.png) |

### Port

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/AshLinePort/ashlineport-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/AshLinePort/after-OperationMap_Ch02M05_PortReview.png) |

### Frontier

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Frontier/frontier-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/Frontier/after-port-close.png) |

### Dense City street

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/DenseCity/densecity-street-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_DistrictEdge01.png) |

### Dense City outskirts

| Approved mockup | Native implementation |
|---|---|
| ![Mockup](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/DenseCity/densecity-outskirts-beautify-mockup-v01.jpg) | ![Native](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_RouteReopened01.png) |

## All native screenshots

### SupplyLine — 6 views

- [after-battle](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-battle.png)
- [after-overview](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-overview.png)
- [after-zoom-gate](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-zoom-gate.png)
- [after-zoom-pumpgate](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-zoom-pumpgate.png)
- [after-zoom-refinery](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-zoom-refinery.png)
- [after-zoom-yard](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/SupplyLine/after-zoom-yard.png)

### CityEdgeAirfield — 4 views

- [after-OperationMap_Ch01M04_AirfieldReview](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/CityEdgeAirfield/after-OperationMap_Ch01M04_AirfieldReview.png)
- [after-OperationMap_Ch04M01_AirfieldReview](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/CityEdgeAirfield/after-OperationMap_Ch04M01_AirfieldReview.png)
- [after-zoom-apron](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/CityEdgeAirfield/after-zoom-apron.png)
- [after-zoom-edge](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/CityEdgeAirfield/after-zoom-edge.png)

### AshLinePort — 3 views

- [after-OperationMap_Ch02M05_PortReview](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/AshLinePort/after-OperationMap_Ch02M05_PortReview.png)
- [after-zoom-containers](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/AshLinePort/after-zoom-containers.png)
- [after-zoom-quay](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/AshLinePort/after-zoom-quay.png)

### Frontier — 4 views

- [after-canal](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/Frontier/after-canal.png)
- [after-overview](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/Frontier/after-overview.png)
- [after-port-blocks](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/Frontier/after-port-blocks.png)
- [after-port-close](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/Frontier/after-port-close.png)

### DenseCity — 18 views

- [after-OperationMap_Ch01_Airlift01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_Airlift01.png)
- [after-OperationMap_Ch01_BreachAssault01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_BreachAssault01.png)
- [after-OperationMap_Ch01_ConvoyApproach01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_ConvoyApproach01.png)
- [after-OperationMap_Ch01_DistrictEdge01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_DistrictEdge01.png)
- [after-OperationMap_Ch01_ForwardPost01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch01_ForwardPost01.png)
- [after-OperationMap_Ch02_HospitalCorridor01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_HospitalCorridor01.png)
- [after-OperationMap_Ch02_OldMarket01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_OldMarket01.png)
- [after-OperationMap_Ch02_PowerRelay01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_PowerRelay01.png)
- [after-OperationMap_Ch02_RouteReopened01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_RouteReopened01.png)
- [after-OperationMap_Ch02_SupplyYard01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch02_SupplyYard01.png)
- [after-OperationMap_Ch03_EvidenceChain01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch03_EvidenceChain01.png)
- [after-OperationMap_Ch03_FalseFront01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch03_FalseFront01.png)
- [after-OperationMap_Ch03_NetworkBreak01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch03_NetworkBreak01.png)
- [after-OperationMap_Ch03_SafehouseSweep01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch03_SafehouseSweep01.png)
- [after-OperationMap_Ch03_SignalTrace01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch03_SignalTrace01.png)
- [after-OperationMap_Ch04_AirCorridor01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch04_AirCorridor01.png)
- [after-OperationMap_Ch04_SplitFront01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch04_SplitFront01.png)
- [after-OperationMap_Ch04_SteelPush01](/Users/farhad/Projects/WarlineCapture-Beautify/Design/MapVariants/Beautify/Evidence/20261003-dense-review/DenseCity/after-OperationMap_Ch04_SteelPush01.png)

## Continuation

Keep visual work ahead of playtesting. The next visual iteration should prioritize authored hero details in the actual camera views (market goods, richer vegetation and distinct edge vignettes), rather than increasing a procedural object count alone. Preserve mission clearances, the bound semantic pins, protected source art, renderer-free map roots and the separate atmosphere roots. Do not commit regenerated scenes until the implemented look passes review.
