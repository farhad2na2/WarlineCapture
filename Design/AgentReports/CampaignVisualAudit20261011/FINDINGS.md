# Campaign visual audit — all 25 missions

Implementation follow-up: [current fixes and verification](fixes/IMPLEMENTATION_RESULT.md), [updated native gallery](fixes/gallery.html). The observations below preserve the original baseline audit.

Audited 2026-10-11 at baseline `c88c79c5453532a3dca9c9c2fd0c9d37603daed7` in the separate T7 checkout, branch `codex/campaign-visual-audit-20261011`.

The biggest visual gains are coherent civic/bazaar paving, properly finished road/sidewalk transitions and runway ends, stronger destination identities, and opening cameras that show the player's actual force. More random texture or prop scatter would leave the main composition problems unresolved.

Start with the [visual gallery](gallery.html) and [implementation plan](IMPLEMENTATION_PLAN.md). [Structured findings](findings.json), [source/mission inventory](inventory.json), [hashed evidence manifest](evidence-manifest.json), and [workspace provenance](workspace.json) and [reproduction notes](REPRODUCTION.md) support the review.

## Scope and evidence

All 25 missions have a visual entry. Twenty-one produced native runtime environment captures; twenty have usable opening HUD evidence. Steel Push instead shows an immediate failure result. Four Chapter 5 missions fail native map readiness and were reviewed through freshly rendered authored-source previews. Those previews do not demonstrate a playable mission or its opening camera.

Captures use the existing GUI Unity Editor, Metal, 1600×900 runtime views and 1600×1000 source previews. The checked macOS wrapper runs every capture. An isolated unlocked save profile, programmatic menu deployment and native narrative Skip actions make the work reproducible without altering the user's save. Inspection cameras move only the camera; objective results are not injected. The broad overview deliberately exceeds ordinary gameplay framing and may show fog, terrain edges or voids unavailable to the player. Source-preview overview fog/post processing is suppressed for inspection; source detail views retain authored scene lighting.

Capture-helper pass markers mean images were written, not that gameplay passed. No complete normal-input mission playthrough or Android/player acceptance is claimed. Generated compile checks, wrapper logs and failed attempts are retained under `validation/`. V1 diagnostics list disabled lights and include an unused hour=0 field; neither is evidence of actual double lighting or midnight. V2 receipts record enabled light states, settled camera status and mission outcome.

## Priority findings

| Priority | Finding | Recommended first change |
|---|---|---|
| P0 | Four Chapter 5 maps reject packed entity identity counts | Trace bake/metadata ownership and regenerate coherent content; keep readiness guard intact |
| P0 | Steel Push opens on RESERVE DEFENSE FAILED with reserve depot/Barracks placement failures | Repair the functional placement/launch issue before dressing the site |
| P0 verification | Supply Line logs a failed refinery placement | Confirm the intended resource facility exists and its normal mission sequence works |
| P1 | Hall/bazaar looks like an asphalt grid with disconnected paving and scattered shade | A continuous civic stone forecourt, defined vehicle edge and usable stall clusters |
| P1 | Sidewalk and uncurbed road classes meet abruptly | Matched corner/curb returns, flush driveways and deliberate shoulder transitions |
| P1 | Runways look like strips ending abruptly in sand | Treat both assembled endpoints, thresholds, shoulders, taxiway joins and end ground |
| P1 | Several opening cameras show empty ground or crop the force/destination | Reframe after narrative settling, within actual HUD safe area |
| P1 | Shared maps repeat the same street composition across distinct missions | Function-specific destination props and camera views, preserving map bindings |
| P1 | Some bindings keep a source sun and Match sun active | Verify enabled ownership per map, compare one deliberate key, then tune materials |
| P2 | Uniform concrete/sand, isolated rocks and repetitive clutter | Large-scale material variation and small groups tied to real activity |

## Native launch gates

| Mission | Observed gate | Evidence |
|---|---|---|
| 07 Supply Line | Refinery at (740,475), footprint (25,15), placement rejected; runtime capture continues | [native log](validation/m07-01.log) |
| 17 Steel Push | Reserve depot at (805,650), footprint (24,23), and Barracks at (740,650), footprint (28,15), rejected; failure screen visible | [HUD](captures/saga.ch04.m02.steel_push/run-01/opening-hud.png), [native log](validation/m17-01.log) |
| 21 Citywide Alert | Generated identity count 2942 / expected 2979 | [retry log](validation/m21-02.log) |
| 23 Network Collapse | 2935 / 2972 | [retry log](validation/m23-02.log) |
| 24 Last Corridor | 2935 / 2972 | [retry log](validation/m24-02.log) |
| 25 Command Node | 2633 / 2695 | [retry log](validation/m25-02.log) |

Each failed Chapter 5 load was retried after a synchronous force import of its authored source; the mismatch persisted. This rules out that narrow cache refresh as a remedy, not every possible cache/content cause. Source previews preserve the authored file hash before/after. The repeated 37-identity difference in three maps is a useful lead; Command Node differs by 62. Recurring tent placement failures are also retained in logs for a shared placement audit. Art must not hide missing gameplay owners.

## Shared-source impact

The 25 mission definitions resolve to 11 physical source bindings. DenseCity is shared by 11 missions; CityEdgeAirfield by two; RefineryDistrict by four; AshLinePort by one. Grounded Signal, Armor Break and the five Chapter 5 missions each have their own existing authored source. Coordinate a source edit with every Campaign, Skirmish and Operations consumer. Do not infer new physical maps from mission count.

## Mission review

World coordinates below are (X, Z) from the shipping map's authored anchors. Suggestions describe future changes; observations describe the captured evidence. All anchors, asset paths and raw receipts are also in findings.json.

### 01. First Contact — P1

`saga.ch01.m01.first_contact` · **Native runtime environment and opening HUD**

A readable first encounter in a living Old Market, with a protected civilian route.

**Observed:** The settled opening keeps the street axis but places the nearest soldiers against the bottom tutorial/command controls. Native close views at the civilian safe-zone anchor show a narrow asphalt cross street running directly across the civic hall entrance, a perpendicular lane aimed at the doorway, isolated canopies and fragmented pale paving. Supplemental mesh-name scans also frame two smaller hall instances elsewhere; the primary civic-frontage evidence is story-civilian_safe_zone plus objective-detail.

**Camera:** Keep the patrol street axis; pull back only enough to include the command squad and the first safe civilian turn above the bottom HUD. Use the hall as the distant orientation landmark rather than the main encounter.

**Lighting:** Retain warm masonry with cooler, readable alley shadows. Reduce the chalky value similarity between white paving and facades; verify the active key light before changing exposure.

**Ground and roads:** Treat the hall entrance as a pedestrian stone forecourt with an explicit vehicle edge. Vary paving by use: large civic slabs, small market setts, compacted service-lane dust. Terminate asphalt and curbs deliberately at the plaza.

**Props and city design:** Build short stall clusters facing real pedestrian aisles: produce baskets, rolled rugs, shaded counters, handcarts and loading crates. Keep the patrol corridor and civilian turn unobstructed; give the safe route a legible entrance rather than random scattered props.

**Landscape:** Blend building foundations and curb edges into sand with restrained accumulation; stop repeating tiny rubble uniformly across every parcel.

**Locations:** `patrol_objective` (1792, 743); `civilian_safe_zone` (1750, 755); `civilian_evacuation` (1740, 775).

**Evidence:** [opening-hud](captures/saga.ch01.m01.first_contact/run-02/opening-hud.png), [objective-context](captures/saga.ch01.m01.first_contact/run-02/objective-context.png), [objective-detail](captures/saga.ch01.m01.first_contact/run-02/objective-detail.png), [mission-overview](captures/saga.ch01.m01.first_contact/run-02/mission-overview.png), [civic-hall-01-context](captures/saga.ch01.m01.first_contact/run-02/civic-hall-01-context.png), [civic-hall-01-front](captures/saga.ch01.m01.first_contact/run-02/civic-hall-01-front.png), [civic-hall-02-context](captures/saga.ch01.m01.first_contact/run-02/civic-hall-02-context.png), [civic-hall-02-front](captures/saga.ch01.m01.first_contact/run-02/civic-hall-02-front.png), [story-civilian_safe_zone](captures/saga.ch01.m01.first_contact/run-02/story-civilian_safe_zone.png), [DenseCity-source-runway-runway-01-end-a](source-runway-previews/saga.ch01.m01.first_contact/run-01/DenseCity-source-runway-runway-01-end-a.png), [DenseCity-source-runway-runway-01-end-b](source-runway-previews/saga.ch01.m01.first_contact/run-01/DenseCity-source-runway-runway-01-end-b.png); [receipt](captures/saga.ch01.m01.first_contact/run-02/receipt.json).

**Additional runway evidence:** endpoint images marked source-runway are authored-source previews, without runtime actors/HUD. [Source hash and receipt](source-runway-previews/saga.ch01.m01.first_contact/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 02. Establish Base — P1

`saga.ch01.m02.establish_base` · **Native runtime environment and opening HUD**

Reopen an abandoned forward post and understand immediately where the new Barracks belongs.

**Observed:** The empty sand/helipad opening persists after a 9.35-second warmup with cameraSettled=true. The squad and existing camp sit outside the useful center while ARIA asks to open Build. The overview presents a busy aircraft depot and repeated tent rows, competing with the abandoned-forward-post story.

**Camera:** Reframe around the actual legal Barracks build area and command squad, keeping the access gate in view. Confirm the construction instruction points to visible terrain and does not frame the helipad as the build target.

**Lighting:** Keep clear daylight construction readability; strengthen grounded contact contrast around slab edges and tent feet without making the open build area dark.

**Ground and roads:** Join the camp gate to its service apron with compacted wheel tracks and a believable slab-to-sand shoulder. Keep the build footprint clear; distinguish reserved building ground from aircraft servicing pavement. The shared DenseCity runway endpoint preview also shows a broad sand intrusion across its pavement edge near industrial tanks; audit that overlap and the raw ends before applying surface detail.

**Props and city design:** Use a few disused-post cues at the edges: folded shelter, stacked barriers, supply pallets and a weathered gate sign. Consolidate repeated tents into recognisable sleeping, repair and logistics groups without touching functional units.

**Landscape:** Replace evenly spaced rock scatter beside the build view with small natural groups; retain a readable clear construction area.

**Locations:** `friendly_spawn` (920.5, 425.5); `defense_boundary` (909.5, 349.5); `comms_focus` (925.5, 360.5).

**Evidence:** [opening-hud](captures/saga.ch01.m02.establish_base/run-02/opening-hud.png), [objective-context](captures/saga.ch01.m02.establish_base/run-02/objective-context.png), [objective-detail](captures/saga.ch01.m02.establish_base/run-02/objective-detail.png), [mission-overview](captures/saga.ch01.m02.establish_base/run-02/mission-overview.png); [receipt](captures/saga.ch01.m02.establish_base/run-02/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 03. Radar Warning — P1

`saga.ch01.m03.radar_warning` · **Native runtime environment and opening HUD**

Read two incoming convoy approaches and defend a credible forward-post checkpoint.

**Observed:** The first view centres a very wide divided road and pale median. Farther along, wrecks scatter across both carriageways, while the road has locally abrupt sand intrusions. A small watchtower and barriers do not yet make the defense line visually distinct from the boulevard.

**Camera:** Frame the squad, sensor and incoming road together; use a short camera-only overview for the two approaches, then return to a readable tactical distance.

**Lighting:** Preserve a stable daylight key and enough value contrast for approaching vehicles against dark asphalt and for the sensor against pale sand.

**Ground and roads:** Finish median terminations and junction curb continuity. Group repair patches and tyre wear around the checkpoint; inspect each sand intrusion as either deliberate damage with a transition or an unwanted overlap.

**Props and city design:** Dress the post with a small radar support cluster, cable route, generator and ordered blast barriers. Cluster wreckage into a believable incident off the drivable convoy lane instead of evenly distributing cars.

**Landscape:** Retain the ridgeline as an approach silhouette; add a coarse-to-fine rock transition at its foot, without filling vehicle clearance.

**Locations:** `squad_a` (910, 426); `ground_sensor` (949, 428); `contact` (830, 426).

**Evidence:** [opening-hud](captures/saga.ch01.m03.radar_warning/run-01/opening-hud.png), [objective-context](captures/saga.ch01.m03.radar_warning/run-01/objective-context.png), [objective-detail](captures/saga.ch01.m03.radar_warning/run-01/objective-detail.png), [mission-overview](captures/saga.ch01.m03.radar_warning/run-01/mission-overview.png); [receipt](captures/saga.ch01.m03.radar_warning/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 04. Airlift — P1

`saga.ch01.m04.airlift` · **Native runtime environment and opening HUD**

A two-stage rescue with an unmistakable specialist pickup, APC route and helicopter landing area.

**Observed:** The opening shows a broad bright concrete apron and very small soldiers. Inspection views reveal a narrow runway-like asphalt rectangle with a parked transport, abrupt ends and sparse surrounding airport infrastructure. The wide diagnostic view exposes the rectangular terrain edge; this is not proof that the shipping camera can reach that edge. Fresh source endpoint views confirm bare square-cut outer ends with no threshold bars or identifiers, an unblended sand shoulder and a parallel road separating the runway from the main apron. Two adjoining mesh modules make one continuous strip; their inner endpoints are not extra runway thresholds.

**Camera:** Keep APC, squad and the first destination legible in the opening. Provide a camera-only overview connecting the specialist pickup to the landing pad; avoid using whole-map bounds to determine normal zoom.

**Lighting:** The apron is very pale. Tune concrete roughness and slab-value variation before increasing global contrast. The legacy diagnostics list disabled lights too, so their duplicate entries do not prove double lighting.

**Ground and roads:** Give both runway ends threshold bars, end lines and appropriately oriented identifiers; add continuous shoulders, restrained touchdown wear and a compacted end transition inside the existing footprint. Align taxiway centrelines, hold markings and apron pavement at actual joins.

**Props and city design:** Add coherent perimeter-side servicing groups: baggage cart, cones, fire equipment and a sheltered specialist waiting area. Keep runway, aircraft envelopes and the rescue/APC path clear.

**Landscape:** Make the airport fence/ground edge and sparse vegetation intentional; reduce the uniformly scattered shrubs and repeated ground spots around the apron.

**Locations:** `carrier` (850, 610); `specialists` (910, 655); `landing` (760, 648).

**Evidence:** [opening-hud](captures/saga.ch01.m04.airlift/run-02/opening-hud.png), [objective-context](captures/saga.ch01.m04.airlift/run-02/objective-context.png), [objective-detail](captures/saga.ch01.m04.airlift/run-02/objective-detail.png), [mission-overview](captures/saga.ch01.m04.airlift/run-02/mission-overview.png), [story-landing](captures/saga.ch01.m04.airlift/run-02/story-landing.png), [story-specialists](captures/saga.ch01.m04.airlift/run-02/story-specialists.png), [CityEdgeAirfield-source-runway-runway-01-end-a](source-runway-previews/saga.ch01.m04.airlift/run-01/CityEdgeAirfield-source-runway-runway-01-end-a.png), [CityEdgeAirfield-source-runway-runway-01-end-b](source-runway-previews/saga.ch01.m04.airlift/run-01/CityEdgeAirfield-source-runway-runway-01-end-b.png), [CityEdgeAirfield-source-runway-runway-02-end-a](source-runway-previews/saga.ch01.m04.airlift/run-01/CityEdgeAirfield-source-runway-runway-02-end-a.png), [CityEdgeAirfield-source-runway-runway-02-end-b](source-runway-previews/saga.ch01.m04.airlift/run-01/CityEdgeAirfield-source-runway-runway-02-end-b.png); [receipt](captures/saga.ch01.m04.airlift/run-02/receipt.json).

**Additional runway evidence:** endpoint images marked source-runway are authored-source previews, without runtime actors/HUD. [Source hash and receipt](source-runway-previews/saga.ch01.m04.airlift/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity`.

### 05. Breach Assault — P1

`saga.ch01.m05.breach_assault` · **Native runtime environment and opening HUD**

Read a fortified relay compound with a gate, transmitter and recoverable archive.

**Observed:** The opening crops the target compound against the top HUD. The objective detail shows a mostly empty sand rectangle, two guard towers, a small shed and a bright dish, separated from the boulevard by an undifferentiated sandy gap.

**Camera:** Move the initial focus toward the gate enough to show gate, squad and at least the transmitter silhouette simultaneously; keep the target structure below the resource header.

**Lighting:** Use building and tower shadows to articulate the compound; preserve gate readability. Check light enablement before assigning cause to the multiple light names in the diagnostic list.

**Ground and roads:** Add a coherent gate apron and worn internal service path connecting gate, transmitter and archive. Finish the sidewalk crossing at the entrance with a dropped curb and clear pavement material change.

**Props and city design:** Create a compact communications installation: equipment cabinets, cable trays, generator, maintenance crates and protected archive doorway. Keep every gameplay target visually distinct and accessible; no duplicate functional objects.

**Landscape:** Blend the rear rock shelf and wall base with grounded rubble bands, avoiding isolated identical boulders across the empty yard.

**Locations:** `gate` (1020, 444); `core` (1038, 463); `archive` (1007, 463).

**Evidence:** [opening-hud](captures/saga.ch01.m05.breach_assault/run-01/opening-hud.png), [objective-context](captures/saga.ch01.m05.breach_assault/run-01/objective-context.png), [objective-detail](captures/saga.ch01.m05.breach_assault/run-01/objective-detail.png), [mission-overview](captures/saga.ch01.m05.breach_assault/run-01/mission-overview.png); [receipt](captures/saga.ch01.m05.breach_assault/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 06. Gridlock — P1

`saga.ch02.m01.gridlock` · **Native runtime environment and opening HUD**

A road crew restores a damaged service corridor so relief can reach the hospital.

**Observed:** Large plain transverse barriers, sand strips intruding onto asphalt, scattered cars and a distant shed dominate the route. The work sites and hospital function are weakly differentiated from generic battlefield rubble.

**Camera:** Start with Fadi, crew and the nearest work site in the usable centre of the screen, then offer an overview showing the second site and hospital direction.

**Lighting:** Preserve daylight visibility of crew and relief vehicles; use warmer work-zone accents against neutral asphalt rather than a global orange cast.

**Ground and roads:** Give each authored blockage a believable cause, such as a broken pavement edge and contained rubble, while preserving its exact gameplay footprint. Repair the sidewalk-to-damaged-shoulder transitions and the exposed sand/asphalt intersections.

**Props and city design:** Dress the two work sites differently with cones, a small tool staging area and compact material piles. Give the hospital delivery point a clear accessible entrance, medical-service sign and grouped aid crates; keep route and worker access open.

**Landscape:** Blend nearby rock shelves into the residential ground; reduce the sense of separate floating village islands through paths and consistent foundation treatment.

**Locations:** `workers` (685, 436); `site_a` (716, 426); `site_b` (766, 426); `hospital` (814, 426).

**Evidence:** [opening-hud](captures/saga.ch02.m01.gridlock/run-01/opening-hud.png), [objective-context](captures/saga.ch02.m01.gridlock/run-01/objective-context.png), [objective-detail](captures/saga.ch02.m01.gridlock/run-01/objective-detail.png), [mission-overview](captures/saga.ch02.m01.gridlock/run-01/mission-overview.png); [receipt](captures/saga.ch02.m01.gridlock/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 07. Supply Line — P0 placement verification; P1 art

`saga.ch02.m02.supply_line` · **Native runtime environment and opening HUD**

Understand the Oil to refinery to Fuel-storage chain as a working industrial site.

**Observed:** The opening is largely identical white slabs, pipe racks and repeated red processing units. The roadside gate is a gap in a fence across a sandy verge. Storage and processing areas have little visual hierarchy at tactical distance. The native log reports a failed SupplyLine_Building_Refinery placement at (740,475); verify the intended functional facility before accepting this mission or dressing the area.

**Camera:** Frame the first hauler and its loading destination, then give a concise overview of the three logistics stages; avoid letting foreground pipe racks dominate the first decision.

**Lighting:** Preserve readable pipe and vehicle shadows on concrete. Reduce the nearly uniform bright apron values through materials and local wear before changing exposure.

**Ground and roads:** Differentiate loading bay, processing apron and storage approach with concrete panel scale, local oil staining, drains and wheel paths. Build a flush, legible road-to-gate crossover with deliberate curb transitions.

**Props and city design:** Give oil loading a pump/hose cluster, the refinery a maintenance cabinet group and Fuel storage a bund/inspection-side identity. Use existing decorative assets and keep the working loading/spawn envelopes clear.

**Landscape:** Replace even edge gravel scatter with drainage and dust accumulation along fence bases; keep the distant industrial silhouette readable.

**Locations:** `oil` (573, 520); `refinery` (740, 475); `storage` (941, 540); `oil_hauler` (580, 505); `fuel_hauler` (765, 505).

**Evidence:** [opening-hud](captures/saga.ch02.m02.supply_line/run-01/opening-hud.png), [objective-context](captures/saga.ch02.m02.supply_line/run-01/objective-context.png), [objective-detail](captures/saga.ch02.m02.supply_line/run-01/objective-detail.png), [mission-overview](captures/saga.ch02.m02.supply_line/run-01/mission-overview.png); [receipt](captures/saga.ch02.m02.supply_line/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparedEntities.unity`.

### 08. Market Lifeline — P1

`saga.ch02.m03.market_lifeline` · **Native runtime environment and opening HUD**

Keep an active market supplied while distinguishing legitimate paperwork from a copied transfer.

**Observed:** The first quick capture caught rocks and trees during the camera transition. The repeat settles after 11.97 seconds and correctly shows the convoy on the boulevard; this is not a persistent off-target camera defect. The route still repeats the damaged highway composition, with scattered wrecks and sand intrusions and little market receiving activity near the convoy/manifest locations.

**Camera:** Retain the settled convoy framing. Check whether the pre-control transition can finish sooner, and keep ARIA instruction timing aligned with the useful view. Give manifest and market-receiving destinations clear camera-only focus points without spoiling verification.

**Lighting:** Maintain neutral daylight and locally distinct canopy colours so the civilian market feels active without masking friendly/hostile colours.

**Ground and roads:** Create an identifiable market receiving apron connected to the boulevard; reserve pedestrian stone paving around stalls and compacted service ground beside truck unloading. Finish curb drops and crossings at the access points.

**Props and city design:** Prioritise the user's bazaar request here: group produce, textile and household-goods stalls; include loading carts and a shaded manifest desk at each authored transfer point. Differentiate transfers through inspectable details, without visually revealing hidden guilt before verification.

**Landscape:** Move visual emphasis from oversized foreground rocks to shopfronts and pedestrian routes; retain existing movement geometry and civilian clearance.

**Locations:** `delivery` (797, 451); `manifest` (698, 440); `corrupt_manifest` (748, 462); `convoy` (684, 426).

**Evidence:** [opening-hud](captures/saga.ch02.m03.market_lifeline/run-02/opening-hud.png), [objective-context](captures/saga.ch02.m03.market_lifeline/run-02/objective-context.png), [objective-detail](captures/saga.ch02.m03.market_lifeline/run-02/objective-detail.png), [mission-overview](captures/saga.ch02.m03.market_lifeline/run-02/mission-overview.png); [receipt](captures/saga.ch02.m03.market_lifeline/run-02/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 09. Power Relay — P1

`saga.ch02.m04.power_relay` · **Native runtime environment and opening HUD**

Recognise a power substation, its repair point and a safer route for displaced families.

**Observed:** The initial frame is mostly a truck, asphalt and sand. The utility yard reads as an evenly spaced field of poles on a blank slab, with little equipment detail. The long straight roadside fence and generic container yard make the protected route hard to distinguish visually.

**Camera:** Show engineers and the repair destination first, with a camera-only route overview that clearly distinguishes the longer protected service road from the exposed shortcut.

**Lighting:** Keep small engineer silhouettes readable at the repair bay. Local equipment and paving contrast should carry the substation identity, not an overall darkening.

**Ground and roads:** Differentiate the utility yard with gravel equipment beds, concrete access strips and cable-trench covers inside existing traversable limits. Finish gate crossovers and sidewalk endings; use restrained wheel wear to reinforce the safe service route.

**Props and city design:** Place transformer/switchgear silhouettes, insulator groups, cabinets and cable reels around the repair area, plus a compact sheltered receiving point for families. Avoid dressing every pole identically or placing props on mission routes.

**Landscape:** Use sparse vegetation beyond the utility fence and service drainage at the slab edge; keep industrial and shelter ground treatments distinct.

**Locations:** `short_route` (700, 505); `safe_route` (705, 385); `shelter` (885, 385); `repair` (813, 487).

**Evidence:** [opening-hud](captures/saga.ch02.m04.power_relay/run-01/opening-hud.png), [objective-context](captures/saga.ch02.m04.power_relay/run-01/objective-context.png), [objective-detail](captures/saga.ch02.m04.power_relay/run-01/objective-detail.png), [mission-overview](captures/saga.ch02.m04.power_relay/run-01/mission-overview.png); [receipt](captures/saga.ch02.m04.power_relay/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparedEntities.unity`.

### 10. Route Reopened — P1

`saga.ch02.m05.route_reopened` · **Native runtime environment and opening HUD**

Reopen a working port logistics corridor, keep two lifelines moving and preserve the hub records.

**Observed:** The port has a useful river and bridge landmark, but repeated container stacks, very bright concrete bays and widely scattered tiny props dominate. The hub is represented by a tall tower and the water edge by a neat line of isolated rocks; the records destination is not visually obvious in these views.

**Camera:** Frame the disrupted link and first relief movement, with the bridge as a secondary landmark. Give the hub entrance a closer view that is not dominated by the tower roof.

**Lighting:** Keep the water/land distinction but reduce washed-out concrete. Check active source and Match lighting ownership for this port binding before tuning its exposure or fog.

**Ground and roads:** Make dockside slabs distinct from public sidewalk and service asphalt. Resolve gate and bridge approaches with continuous edging, flush crossings and road wear aligned to turning trucks. Improve the quay edge with a continuous foundation/water-contact treatment.

**Props and city design:** Group forklift/pallet/container activity at believable loading faces. Dress the hub doorway as a staffed routing office with a records cabinet or secured equipment room visible at tactical distance. Keep relief and Fuel bays visually different and traffic lanes open.

**Landscape:** Replace the equally spaced river rocks with clustered bank or quay detail and restrained waterline staining; do not move bridge or navigable geometry.

**Locations:** `relief_goal` (620, 385); `fuel_goal` (740, 505); `disrupted_link` (625, 505); `hub_gate` (595, 565); `records` (605, 595).

**Evidence:** [opening-hud](captures/saga.ch02.m05.route_reopened/run-01/opening-hud.png), [objective-context](captures/saga.ch02.m05.route_reopened/run-01/objective-context.png), [objective-detail](captures/saga.ch02.m05.route_reopened/run-01/objective-detail.png), [mission-overview](captures/saga.ch02.m05.route_reopened/run-01/mission-overview.png); [receipt](captures/saga.ch02.m05.route_reopened/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/AshLinePort/PreparedEntities.unity`.

### 11. Signal Trace — P1

`saga.ch03.m01.signal_trace` · **Native runtime environment and opening HUD**

Compare two observations, protect civilian radios and identify one armed carrier.

**Observed:** The opening and inspection views closely repeat Market Lifeline's wrecked boulevard. Radio use and the two observation locations are not expressed strongly through scenery; a player sees trucks and scattered soldiers before seeing an investigation setting.

**Camera:** Begin with the friendly observation group and first inspectable point. Offer a camera-only comparison of the two points, retaining visual context rather than zooming into anonymous vehicles.

**Lighting:** Use consistent daylight so identification relies on silhouettes and authored evidence, not enemies artificially highlighted by lighting.

**Ground and roads:** Give each observation point a small, grounded standing area connected to existing pavement. Finish the damaged-road and curb interfaces inherited from the shared corridor.

**Props and city design:** Add a civilian radio repair counter, rooftop domestic antennas and small receiver/recording kits at the actual observation points. Keep the confirmed hostile device distinct only when the mission reveals it; avoid decorative cues that prematurely identify the suspect.

**Landscape:** Reduce incidental tree/rock competition near the observation positions; preserve domestic courtyards and existing sight lines.

**Locations:** `manifest` (707, 430); `corrupt_manifest` (748, 462); `corrupt_convoy` (738, 456).

**Evidence:** [opening-hud](captures/saga.ch03.m01.signal_trace/run-01/opening-hud.png), [objective-context](captures/saga.ch03.m01.signal_trace/run-01/objective-context.png), [objective-detail](captures/saga.ch03.m01.signal_trace/run-01/objective-detail.png), [mission-overview](captures/saga.ch03.m01.signal_trace/run-01/mission-overview.png); [receipt](captures/saga.ch03.m01.signal_trace/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 12. Safehouse Sweep — P1

`saga.ch03.m02.safehouse_sweep` · **Native runtime environment and opening HUD**

Recognise two safehouse exits, occupied family homes and a courier/evidence objective.

**Observed:** The same highway, trucks and wrecks dominate again. The reviewed views do not communicate an enclosed safehouse with two useful exits; protected homes visually merge with generic residential rubble.

**Camera:** Shift mission guidance toward the verified house frontage and exits. Use a tactical angle where a roof does not hide both entrances; verify this against the actual exit anchors before changing the framing.

**Lighting:** Preserve readable doorway recesses without turning the family homes into dark silhouettes. Subtle awning and facade contrast can separate entrances from walls.

**Ground and roads:** Use continuous doorstep paving and short courtyard paths to connect the two exits to the existing street. Preserve legal route widths and avoid visually implying an inaccessible passage.

**Props and city design:** Dress protected occupied homes with domestic water, laundry and food-storage details; reserve shutters, controlled entry and verified equipment cues for the actual safehouse. Add a grounded ledger/courier handoff point rather than arbitrary roadside crates.

**Landscape:** Consolidate rubble at damaged building bases and leave occupied frontages maintained, making the inhabited-versus-damaged distinction intentional.

**Locations:** `delivery` (735, 421); `manifest` (694, 438); `corrupt_manifest` (720, 452).

**Evidence:** [opening-hud](captures/saga.ch03.m02.safehouse_sweep/run-01/opening-hud.png), [objective-context](captures/saga.ch03.m02.safehouse_sweep/run-01/objective-context.png), [objective-detail](captures/saga.ch03.m02.safehouse_sweep/run-01/objective-detail.png), [mission-overview](captures/saga.ch03.m02.safehouse_sweep/run-01/mission-overview.png); [receipt](captures/saga.ch03.m02.safehouse_sweep/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 13. False Front — P1

`saga.ch03.m03.false_front` · **Native runtime environment and opening HUD**

Contrast an unconfirmed depot report with a real civilian evacuation and a later verified ambush.

**Observed:** The native views are nearly indistinguishable from Signal Trace and Safehouse Sweep. The depot, exposed crossing and evacuation destination need a more coherent visual relationship; evenly distributed roadside wrecks dilute the specific threat.

**Camera:** Show civilian vehicles and the next safe route segment first. Frame the unconfirmed depot as context without making it look like an already approved target.

**Lighting:** Keep the depot visually neutral before verification. Build tension through occlusion and composition, while preserving civilian vehicle readability.

**Ground and roads:** Differentiate a maintained evacuation track from damaged roadside margins using coherent patches and tyre paths. Repair sudden sidewalk endings and avoid decorative rubble in actual escape lanes.

**Props and city design:** Use a plausible neutral depot loading front, an organised civilian assembly point and a restrained ambush position after confirmation. Add travel luggage or aid crates at the destination, not military props throughout every civilian space.

**Landscape:** Use existing rock/terrain edges to frame the exposed road; reduce repeated isolated boulders and giant empty sand gaps around the mission's active area.

**Locations:** `delivery` (746, 420); `manifest` (696, 438); `corrupt_manifest` (720, 450); `convoy` (684, 426).

**Evidence:** [opening-hud](captures/saga.ch03.m03.false_front/run-01/opening-hud.png), [objective-context](captures/saga.ch03.m03.false_front/run-01/objective-context.png), [objective-detail](captures/saga.ch03.m03.false_front/run-01/objective-detail.png), [mission-overview](captures/saga.ch03.m03.false_front/run-01/mission-overview.png); [receipt](captures/saga.ch03.m03.false_front/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 14. Evidence Chain — P1

`saga.ch03.m04.evidence_chain` · **Native runtime environment and opening HUD**

Pick up a witness and Dr. Lina at a clinic, protect the sealed archive and choose an air or armoured extraction.

**Observed:** The opening centres the escort on the divided boulevard, with clinic pickup and extraction destinations outside the immediate view. The broader route is dominated by wreckage and sand intrusions, so the protected medical/evidence journey has little distinct scenery support.

**Camera:** Frame the escort/APC together, then give a brief camera-only view of the clinic entrance and both extraction choices. Keep the witness pickup clear of foreground roofs and the HUD.

**Lighting:** Preserve readable people at the clinic and against the APC; use restrained cool medical accents against the warm settlement, without a new overall colour grade.

**Ground and roads:** Create an accessible clinic forecourt and a legible pickup crossover to the existing roadway. Give the helicopter receiving area a clear, appropriately surfaced pad and shoulder rather than relying on surrounding sand.

**Props and city design:** Use a small medical receiving cluster, a shaded waiting spot, physical archive cases and an extraction-side staging group. Keep medical activity separate from wreck dressing; no extra functional helicopter or APC duplicates.

**Landscape:** Consolidate roadside wreck clusters so the two evacuation paths remain visually legible; blend the clinic parcel with the street rather than leaving an isolated white slab.

**Locations:** `carrier` (979, 426); `specialists` (801, 427); `landing` (1006.93, 393.26); `aircraft` (1006.93, 393.26).

**Evidence:** [opening-hud](captures/saga.ch03.m04.evidence_chain/run-01/opening-hud.png), [objective-context](captures/saga.ch03.m04.evidence_chain/run-01/objective-context.png), [objective-detail](captures/saga.ch03.m04.evidence_chain/run-01/objective-detail.png), [mission-overview](captures/saga.ch03.m04.evidence_chain/run-01/mission-overview.png); [receipt](captures/saga.ch03.m04.evidence_chain/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 15. Network Break — P1

`saga.ch03.m05.network_break` · **Native runtime environment and opening HUD**

Breach a verified bunker gate, stop an erasure node and recover an audit while protecting neighbouring homes.

**Observed:** The compound closely repeats Breach Assault: a bare sandy enclosure, a dish, a small building and two watchtowers. The bunker/erasure/audit sequence is not strongly expressed by the architecture or ground, and the gate sits close to the top edge in the opening view.

**Camera:** Frame gate and first squad together, then use a camera-only step through the authorised targets and protected homes. Keep the protected side of the block visible without implying an attack command.

**Lighting:** Keep target entries readable in shadow; use material value and controlled equipment accents to distinguish the secured installation from civilian roofs.

**Ground and roads:** Connect gate, erasure node and archive with a maintained service strip. Finish the boulevard-to-compound crossing and give the bunker entrance a grounded reinforced threshold inside its existing footprint.

**Props and city design:** Differentiate this mission from the earlier relay compound with armoured entry details, cooling units, cable conduits, equipment cabinets and a clearly secured archive entrance. Dress protected homes domestically and retain their existing ownership and destruction behavior.

**Landscape:** Use rock contact and retaining detail to make the bunker look embedded rather than placed on sand; avoid adding inaccessible steps or altering the verified breach route.

**Locations:** `gate` (1020, 444); `core` (1038, 463); `archive` (1007, 463).

**Evidence:** [opening-hud](captures/saga.ch03.m05.network_break/run-01/opening-hud.png), [objective-context](captures/saga.ch03.m05.network_break/run-01/objective-context.png), [objective-detail](captures/saga.ch03.m05.network_break/run-01/objective-detail.png), [mission-overview](captures/saga.ch03.m05.network_break/run-01/mission-overview.png); [receipt](captures/saga.ch03.m05.network_break/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`.

### 16. Air Corridor — P1

`saga.ch04.m01.air_corridor` · **Native runtime environment and opening HUD**

Defend an operating medical flight corridor with linked radar and missile coverage on two approaches.

**Observed:** The opening mostly shows a roadside launcher and pale apron; the radar/air-defense relationship is not visually obvious. Inspection shows large repeated slabs, sparsely organised aircraft servicing space and an abrupt, minimally marked runway end.

**Camera:** Frame radar and the first launcher together, then provide a camera-only overview of west and north approaches. Maintain enough altitude for aircraft context without reducing ground units to dots.

**Lighting:** The enabled-light probe confirms one active source sun and disabled Match lights for this airfield binding. Preserve that ownership; improve apron/aircraft contrast locally rather than adding lights.

**Ground and roads:** Apply the runway-end treatment shared with Airlift: threshold/end markings, shoulder bands, restrained touchdown wear and deliberate transitions. Separate taxiway yellow guidance from road markings and link apron exits cleanly to actual taxiways.

**Props and city design:** Give radar a small service/generator cluster and the medical-flight area a distinct receiving shelter, ambulance-side staging and orderly ground equipment. Keep missile launch envelopes, runway and aircraft circulation clear.

**Landscape:** Organise perimeter vegetation and stones outside air operations surfaces; reduce the uniformly repeated small ground specks at tactical distance.

**Locations:** `squad_a` (600, 610); `squad_b` (670, 600); `ground_sensor` (630, 600); `contact` (520, 580); `north_contact` (900, 660).

**Evidence:** [opening-hud](captures/saga.ch04.m01.air_corridor/run-01/opening-hud.png), [objective-context](captures/saga.ch04.m01.air_corridor/run-01/objective-context.png), [objective-detail](captures/saga.ch04.m01.air_corridor/run-01/objective-detail.png), [mission-overview](captures/saga.ch04.m01.air_corridor/run-01/mission-overview.png); [receipt](captures/saga.ch04.m01.air_corridor/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity`.

### 17. Steel Push — P0 launch issue; P1 art

`saga.ch04.m02.steel_push` · **Native runtime environment; failed result visible**

Defend a working Fuel reserve and Relay approach against a credible armoured attack.

**Observed:** The captured HUD is already a RESERVE DEFENSE FAILED result, so it is not a usable opening view. The log independently reports failed placement of Building_SteelPush_ReserveDepot at (805,650) and Building_Barrack at (740,650). Environment views show a large flat sandy approach, aligned enemy vehicles and highly repeated oil infrastructure, with little visible defended-site identity. The independent repeat records missionOutcome=Defeat after settling. Its result also says Fuel site INTACT, so investigate initial Relay-approach/failure rules as well as placement; the logs do not alone prove the exact defeat cause.

**Camera:** Resolve and reproduce the initial placement/failure gate before approving an opening camera. Then frame friendly armor, reserve entrance and the incoming column together without hiding the Fuel site behind tanks or HUD.

**Lighting:** The RefineryDistrict repeat confirms the source Sun and Match directional key both enabled at intensity 1.25, with Match fill 0.12. Compare one intentional key after fixing launch correctness; keep tanks and reserve infrastructure separated from bright slab/sand values.

**Ground and roads:** After placement is valid, use compacted access tracks and bounded concrete loading aprons to explain how the reserve is serviced. Add contained heavy-vehicle wear; preserve current routes, army budgets and defense distances.

**Props and city design:** Make the reserve a recognisable defended logistics site with pump/control equipment, sensible storage grouping and a marked civilian allocation area. Restore the intended authoritative functional owner before adding decoration.

**Landscape:** Break up the empty approach through broad material variation and grouped sparse vegetation at edges, leaving a clear armoured engagement corridor.

**Locations:** `squad_a` (760, 625); `armor_support` (790, 625); `civilians` (835, 625); `contact` (630, 625).

**Evidence:** [opening-hud](captures/saga.ch04.m02.steel_push/run-02/opening-hud.png), [objective-context](captures/saga.ch04.m02.steel_push/run-02/objective-context.png), [objective-detail](captures/saga.ch04.m02.steel_push/run-02/objective-detail.png), [mission-overview](captures/saga.ch04.m02.steel_push/run-02/mission-overview.png); [receipt](captures/saga.ch04.m02.steel_push/run-02/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparedEntities.unity`.

### 18. Split Front — P1

`saga.ch04.m03.split_front` · **Native runtime environment and opening HUD**

Distinguish a defended forward base, a verified artillery battery and protected civilian shelters.

**Observed:** The opening communicates a line of tanks near small plain buildings, but the wider view is a sparse flat field between repetitive pumps and refinery columns. The two fronts have little distinct ground or prop identity.

**Camera:** Keep base defenders visible while showing the launcher's first target direction. Provide a deliberate camera-only battery view and return point, rather than pulling the player into a visually anonymous empty field.

**Lighting:** Use material and silhouette contrast to separate battery, base and shelters; confirm source/Match light enablement before exposure changes.

**Ground and roads:** Give the base a compacted service yard, the verified battery a restrained disturbed-ground signature and civilian shelters maintained access paths. Connect each to existing road edges with deliberate shoulder/sidewalk transitions.

**Props and city design:** Add clear but compact command, ammunition and maintenance clusters around the base and battery, with domestic water and shade details at the shelters. Do not decorate protected shelters as military targets.

**Landscape:** Replace evenly dispersed saplings and isolated rocks with small contextual groups; keep both fronts and long-range firing visibility legible.

**Locations:** `squad_a` (782, 625); `armor_support` (850, 625); `civilians` (707, 668); `contact` (700, 625).

**Evidence:** [opening-hud](captures/saga.ch04.m03.split_front/run-01/opening-hud.png), [objective-context](captures/saga.ch04.m03.split_front/run-01/objective-context.png), [objective-detail](captures/saga.ch04.m03.split_front/run-01/objective-detail.png), [mission-overview](captures/saga.ch04.m03.split_front/run-01/mission-overview.png); [receipt](captures/saga.ch04.m03.split_front/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparedEntities.unity`.

### 19. Grounded Signal — P1

`saga.ch04.m04.grounded_signal` · **Native runtime environment and opening HUD**

Unload specialists at the apron, recover military relay hardware and leave by APC while protecting the civilian terminal.

**Observed:** The opening shows only part of the transport aircraft, an APC and a sandy gap beside the road. The relay is a small object in a very large fenced blank concrete square shared with a helipad; the terminal's civilian purpose is weak in the inspected views. The focused terminal view reads as identical aircraft hangars and open slabs; the relay sits almost alone in its large fenced square. These destinations need distinct civilian and military support details.

**Camera:** Show the aircraft unloading point, specialists and APC together. Give the relay and protected terminal separate camera-only views with visible entrances, keeping the civilian destination out of the military target framing.

**Lighting:** The settled native receipt confirms both the source Sun and Match directional key enabled at intensity 1.25, plus Match fill 0.12. Compare a single intended key before changing exposure. Preserve aircraft/specialist contrast and separate terminal pedestrian paving from military relay concrete.

**Ground and roads:** Apply the runway-end and taxiway transition pass here too. Make the specialist unloading point a coherent apron edge with a flush APC crossover; distinguish relay service concrete from the terminal pedestrian forecourt. The separate source endpoint pass confirms the same raw outer ends and shoulder treatment on two adjoining runway modules. Treat only the assembled outer ends as operational thresholds.

**Props and city design:** Create a compact relay installation around its actual owner: cabinets, cable reels, generator and a service canopy. Give the terminal a few grouped benches, luggage carts and covered passenger access, preserving staff and vehicle clearance.

**Landscape:** Blend sandy margins into the airport shoulder; remove the visual impression of rocks randomly scattered through servicing space without altering playable boundaries.

**Locations:** `plane` (600, 560); `specialists` (610, 590); `carrier` (635, 590); `relay` (800, 648); `civilian_terminal` (515, 650).

**Evidence:** [opening-hud](captures/saga.ch04.m04.grounded_signal/run-02/opening-hud.png), [objective-context](captures/saga.ch04.m04.grounded_signal/run-02/objective-context.png), [objective-detail](captures/saga.ch04.m04.grounded_signal/run-02/objective-detail.png), [mission-overview](captures/saga.ch04.m04.grounded_signal/run-02/mission-overview.png), [story-civilian_terminal](captures/saga.ch04.m04.grounded_signal/run-02/story-civilian_terminal.png), [story-plane](captures/saga.ch04.m04.grounded_signal/run-02/story-plane.png), [story-relay](captures/saga.ch04.m04.grounded_signal/run-02/story-relay.png), [CH04M04GroundedSignal-source-runway-runway-01-end-a](source-runway-previews/saga.ch04.m04.grounded_signal/run-01/CH04M04GroundedSignal-source-runway-runway-01-end-a.png), [CH04M04GroundedSignal-source-runway-runway-01-end-b](source-runway-previews/saga.ch04.m04.grounded_signal/run-01/CH04M04GroundedSignal-source-runway-runway-01-end-b.png), [CH04M04GroundedSignal-source-runway-runway-02-end-a](source-runway-previews/saga.ch04.m04.grounded_signal/run-01/CH04M04GroundedSignal-source-runway-runway-02-end-a.png), [CH04M04GroundedSignal-source-runway-runway-02-end-b](source-runway-previews/saga.ch04.m04.grounded_signal/run-01/CH04M04GroundedSignal-source-runway-runway-02-end-b.png); [receipt](captures/saga.ch04.m04.grounded_signal/run-02/receipt.json).

**Additional runway evidence:** endpoint images marked source-runway are authored-source previews, without runtime actors/HUD. [Source hash and receipt](source-runway-previews/saga.ch04.m04.grounded_signal/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH04M04GroundedSignal/PreparedEntities.unity`.

### 20. Armor Break — P1

`saga.ch04.m05.armor_break` · **Native runtime environment and opening HUD**

Coordinate combined arms to defeat a military command group while protecting the relief corridor and Fuel depot.

**Observed:** The opening is mostly empty sand and a road corner, with tanks at the top edge. Inspection shows a grid of sparse damaged buildings and an enormous repeated concrete aircraft apron. The military command destination and protected relief corridor have weak visual distinction.

**Camera:** Frame the initial radar/air-defense/armor group at a useful command scale; then show the assault approach and protected relief lane with camera guidance only. Keep the initial infantry above the bottom controls.

**Lighting:** Preserve target identification against asphalt and pale aprons. Verify this source's actual light ownership, then add restrained material-value separation rather than flattening all surfaces with ambient fill.

**Ground and roads:** Use depot concrete, heavy-vehicle track wear and a more maintained relief lane to distinguish the mission's spaces. Finish the sand-road-sidewalk junctions and aircraft-service pavement seams while keeping the existing footprint.

**Props and city design:** Create recognisable command and logistics clusters at the real objectives: antenna/command equipment, repair staging and protected relief supplies. Reduce parade-like repetition among nonfunctional aircraft scenery only where ownership permits; do not alter actual forces.

**Landscape:** Give existing settlement edges a coherent foundation and rubble gradient. Keep the armored corridor open instead of inventing new mountains or narrowing it with decoration.

**Locations:** `radar` (1440, 529); `launcher_air` (1480, 529); `launcher_ground` (1280, 529); `command_squad` (1385, 515); `relief` (1900, 480); `contact` (1510, 529).

**Evidence:** [opening-hud](captures/saga.ch04.m05.armor_break/run-01/opening-hud.png), [objective-context](captures/saga.ch04.m05.armor_break/run-01/objective-context.png), [objective-detail](captures/saga.ch04.m05.armor_break/run-01/objective-detail.png), [mission-overview](captures/saga.ch04.m05.armor_break/run-01/mission-overview.png); [receipt](captures/saga.ch04.m05.armor_break/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH04M05ArmorBreak/PreparedEntities.unity`.

### 21. Citywide Alert — P0 runtime gate; P1 art

`saga.ch05.m01.citywide_alert` · **Authored source preview — runtime readiness failed**

Make the clinic and utility service two distinct, quickly readable response destinations while protecting both communities.

**Observed:** Native deployment fails the packed identity-count gate (2942/2979), including after source reimport. Fresh authored-source views show a small detailed settlement on a rock shelf north of an oversized road grid, long dead-end branches into sand, abrupt pavement endings, and diffuse repeated ground scatter. The clinic and utility anchor contexts are predominantly road and sand in this source-only view; runtime-spawned facilities are not represented, so their absence here is not evidence that they are missing in a successful mission.

**Camera:** After the readiness fix, capture the real opening with both response groups, then give clinic and utility separate camera-only views. Keep each engineer and its next interaction visible; no opening-camera acceptance can be made from source previews.

**Lighting:** Source detail previews contain authored Match lighting, with readable but closely valued sand and paving. Inspect actual runtime enabled lights before exposure adjustments. Keep clinic pedestrians legible against maintained entrance paving.

**Ground and roads:** Finish the boulevard branches with believable curb returns or service-road shoulder transitions. Add connected pedestrian approaches at the actual clinic and utility entrances once their functional positions are confirmed; distinguish civic stone from bounded utility concrete.

**Props and city design:** Use medical shade, water and grouped aid supplies for the clinic; cabinets, pumps, cable access and maintenance storage for the utility. Concentrate density near occupied frontages and real destinations, preserving the recruitment build zone and both response lanes.

**Landscape:** Blend the settlement's rock shelf into its sand apron with medium/small stones and a clear pedestrian connection. Group palms off curb corners, reduce uniform scatter, and make the straight blue water strip's banks/ends plausible within the existing source.

**Locations:** `reinforcement_perimeter` (1040, 426); `clinic` (906, 450); `utility` (1206, 450); `clinic_recovery` (906, 438); `utility_recovery` (1206, 438).

**Evidence:** [CH05M01CitywideAlert-source-preview-detail](source-previews/saga.ch05.m01.citywide_alert/run-01/CH05M01CitywideAlert-source-preview-detail.png), [CH05M01CitywideAlert-source-preview-overview](source-previews/saga.ch05.m01.citywide_alert/run-01/CH05M01CitywideAlert-source-preview-overview.png), [CH05M01CitywideAlert-source-preview-story-clinic](source-previews/saga.ch05.m01.citywide_alert/run-01/CH05M01CitywideAlert-source-preview-story-clinic.png), [CH05M01CitywideAlert-source-preview-story-reinforcement_perimeter](source-previews/saga.ch05.m01.citywide_alert/run-01/CH05M01CitywideAlert-source-preview-story-reinforcement_perimeter.png), [CH05M01CitywideAlert-source-preview-story-utility](source-previews/saga.ch05.m01.citywide_alert/run-01/CH05M01CitywideAlert-source-preview-story-utility.png); [receipt](source-previews/saga.ch05.m01.citywide_alert/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH05M01CitywideAlert/PreparedEntities.unity`.

### 22. Trust Under Fire — P1

`saga.ch05.m02.trust_under_fire` · **Native runtime environment and opening HUD**

Read two protected evacuation routes, their shelters and the broadcast relay without confusing them with military targets.

**Observed:** The settled opening HUD is centered on a mostly empty dark paved yard with scattered props; no convoy is prominent in the usable center. Inspection shows a very regular container field, repeated small installations, a straight canal and bridge, and broad white concrete. The native receipt confirms both the source Sun and Match directional key are enabled.

**Camera:** Frame the north convoy and its first safe route segment at deployment; provide a camera-only south-route/shelter view and an explicit return to the active convoy. Keep the 15-minute evacuation task visually tied to vehicles rather than an empty yard.

**Lighting:** Compare a single intentional source key against the current two enabled 1.25-intensity keys, retaining only deliberate fill. Keep convoy silhouettes and shelter entrances readable; do not use the fogged high-altitude diagnostic overview to judge normal gameplay exposure.

**Ground and roads:** Make the protected north and south routes maintained service corridors with consistent shoulders and flush receiving crossovers. Break container-yard concrete into believable handling/stacking bays rather than a continuous identical tile grid. Give the canal a coherent engineered quay edge.

**Props and city design:** Group evacuation supplies and shade at the actual shelters, distinguish loading bays from empty container storage, and add a compact relay support cluster. Avoid arbitrary repeated isolated barrels across circulation space.

**Landscape:** Group river-edge rocks and sediment where they meet banks rather than repeating evenly spaced stones down the channel; soften warehouse foundations and the sand-to-concrete edge.

**Locations:** `north_start` (470, 505); `north_arrival` (930, 505); `north_shelter` (929, 525); `south_start` (470, 385); `south_arrival` (930, 385); `south_shelter` (929, 405); `relay` (535, 630).

**Evidence:** [opening-hud](captures/saga.ch05.m02.trust_under_fire/run-01/opening-hud.png), [objective-context](captures/saga.ch05.m02.trust_under_fire/run-01/objective-context.png), [objective-detail](captures/saga.ch05.m02.trust_under_fire/run-01/objective-detail.png), [mission-overview](captures/saga.ch05.m02.trust_under_fire/run-01/mission-overview.png); [receipt](captures/saga.ch05.m02.trust_under_fire/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH05M02TrustUnderFire/PreparedEntities.unity`.

### 23. Network Collapse — P0 runtime gate; P1 art

`saga.ch05.m03.network_collapse` · **Authored source preview — runtime readiness failed**

Make a three-node verification sequence, physical audit recovery and protected extraction legible without spoiling unverified targets.

**Observed:** Native deployment fails the packed identity-count gate (2935/2972) on both attempts. The authored-source review shows the same dominant boulevard and rock-shelf settlement composition as Citywide Alert, with long thin side roads, identical paving and broad empty sandy parcels at the three node contexts. Runtime mission owners and units are excluded from these previews; source emptiness must not be mistaken for proof of missing runtime nodes.

**Camera:** Once the mission loads, begin on engineer, carrier and escort. Use node-by-node camera-only guidance at a consistent tactical distance, then a separate audit pickup/extraction framing; verify progress never issues movement commands implicitly.

**Lighting:** Keep communications hardware readable against paving and preserve civilian silhouettes. Compare the final runtime binding with the intended single-key setup before tuning contrast; no runtime lighting verdict is possible from this failed launch.

**Ground and roads:** Give each real node a modest service pad and connected entrance, using a shared coherent material family with distinct activity rather than three arbitrary colored islands. Finish curbs at the narrow side-road junctions; use a clear maintained extraction crossover on the existing road.

**Props and city design:** Add compact antenna/cabinet/cable/generator arrangements only around confirmed functional nodes. Use physical archive containers and a sheltered handover point at the audit gate. Keep unconfirmed sites visually neutral and civic services free of military dressing.

**Landscape:** Reduce disconnected rock and plant noise between the route and node footprints; strengthen the existing settlement edge with a coherent foundation transition. Preserve current long-range sight lines and path widths.

**Locations:** `audit_gate` (1066, 438); `extraction` (1006, 426); `node_1` (906, 426); `node_2` (1066, 426); `node_3` (1206, 426).

**Evidence:** [CH05M03NetworkCollapse-source-preview-detail](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-detail.png), [CH05M03NetworkCollapse-source-preview-overview](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-overview.png), [CH05M03NetworkCollapse-source-preview-story-audit_gate](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-story-audit_gate.png), [CH05M03NetworkCollapse-source-preview-story-extraction](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-story-extraction.png), [CH05M03NetworkCollapse-source-preview-story-node_1](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-story-node_1.png), [CH05M03NetworkCollapse-source-preview-story-node_2](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-story-node_2.png), [CH05M03NetworkCollapse-source-preview-story-node_3](source-previews/saga.ch05.m03.network_collapse/run-01/CH05M03NetworkCollapse-source-preview-story-node_3.png); [receipt](source-previews/saga.ch05.m03.network_collapse/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH05M03NetworkCollapse/PreparedEntities.unity`.

### 24. Last Corridor — P0 runtime gate; P1 art

`saga.ch05.m04.last_corridor` · **Authored source preview — runtime readiness failed**

Read a repaired relief corridor and five purposeful receiving tasks: medicine, Fuel, reinforcements, engineer and authority keys.

**Observed:** Native deployment fails the packed identity-count gate (2935/2972), unchanged after reimport. Authored-source views show a uniform divided road ending abruptly on sand near the receiving area, a slim blue water strip with a hard rectangular end, and repeated empty side-road parcels. The repair context has almost the same ground treatment as its surroundings. The preview does not include runtime-spawned receivers or convoys.

**Camera:** After loading is repaired, frame the initial delivery group and first repair direction. Present the receiving gate and handover positions together at readable scale; avoid framing five visually indistinguishable patches or a whole-map overview.

**Lighting:** Maintain delivery vehicle and receiver contrast at normal gameplay zoom. Use material separation at the receiving yard first, then verify runtime key/fill ownership rather than relying on source-preview exposure.

**Ground and roads:** Create a continuous relief-route surface narrative: localized repair/work patch at the actual repair point, deliberate edge transitions, and a flush maintained receiving yard. Give the visible road end a believable termination inside the existing bounds instead of raw cut asphalt.

**Props and city design:** Group medicine under shade, fuel equipment in a distinct service bay, reinforcement staging in clear open space, and a compact secure key handover point. Keep all five interactions identifiable with existing icon/localization conventions and retain vehicle turning and unload envelopes.

**Landscape:** Finish canal banks and end treatment where the legal camera sees them. Pull palm clusters away from narrow access joins and replace evenly repeated small rock scatter with groups tied to the settlement edge.

**Locations:** `receiving` (1240, 450); `receiving_gate` (1240, 426); `repair` (1120, 420); `medicine_receiver` (1240, 432); `fuel_receiver` (1240, 420); `reinforcement_receiver` (1216, 432); `key_receiver` (1228, 426).

**Evidence:** [CH05M04LastCorridor-source-preview-detail](source-previews/saga.ch05.m04.last_corridor/run-01/CH05M04LastCorridor-source-preview-detail.png), [CH05M04LastCorridor-source-preview-overview](source-previews/saga.ch05.m04.last_corridor/run-01/CH05M04LastCorridor-source-preview-overview.png), [CH05M04LastCorridor-source-preview-story-receiving](source-previews/saga.ch05.m04.last_corridor/run-01/CH05M04LastCorridor-source-preview-story-receiving.png), [CH05M04LastCorridor-source-preview-story-receiving_gate](source-previews/saga.ch05.m04.last_corridor/run-01/CH05M04LastCorridor-source-preview-story-receiving_gate.png), [CH05M04LastCorridor-source-preview-story-repair](source-previews/saga.ch05.m04.last_corridor/run-01/CH05M04LastCorridor-source-preview-story-repair.png); [receipt](source-previews/saga.ch05.m04.last_corridor/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH05M04LastCorridor/PreparedEntities.unity`.

### 25. Command Node — P0 runtime gate; P1 art

`saga.ch05.m05.command_node` · **Authored source preview — runtime readiness failed**

Make the military command core, audit release and protected civic services clearly different destinations for the finale.

**Observed:** Native deployment fails the packed identity-count gate (2633/2695), also after reimport. The authored-source preview shows a large three-sided/walled sandy compound immediately beside the settlement, with a wide entrance gap, scattered trees and little underlying floor hierarchy. Clinic and utility contexts repeat the same sparse boulevard setting as earlier Chapter 5 maps. Runtime-spawned command structures and protected facilities are not shown, so the source preview cannot judge their final silhouettes.

**Camera:** After the readiness fix, frame the escort and breach direction, then make the core/audit-release sequence visible without placing the clinic or utility inside the military target composition. Capture actual native opening and interaction views before approval.

**Lighting:** Separate the military core silhouette from pale sand and keep the audit interaction readable under mobile shadows. Verify real source/Match light states after successful deployment; source lighting is provisional evidence only.

**Ground and roads:** Within the existing compound, connect entrance, core and audit handover with a coherent maintained service surface around the actual runtime footprints. Use bounded concrete and restrained tracks rather than filling the entire enclosure with an identical slab. Finish gate-to-road curb and sand transitions.

**Props and city design:** Dress the real command owner with communications, secure equipment and organized service storage; give the audit release a visible physical handover station. Keep nearby civic facilities civilian, and avoid scenery duplicates that resemble additional gameplay targets.

**Landscape:** Integrate compound wall feet and adjacent rock-shelf buildings into the ground. Keep palms away from the breach path and use sparse grouped planting to frame the enclosure without masking units or shrinking the combat envelope.

**Locations:** `clinic` (906, 450); `utility` (1240, 450); `breach` (1100, 440); `core_audit` (1130, 465); `audit_release` (1130, 476); `safe_receiving` (1006, 438).

**Evidence:** [CH05M05CommandNode-source-preview-detail](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-detail.png), [CH05M05CommandNode-source-preview-overview](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-overview.png), [CH05M05CommandNode-source-preview-story-audit](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-story-audit.png), [CH05M05CommandNode-source-preview-story-breach](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-story-breach.png), [CH05M05CommandNode-source-preview-story-clinic](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-story-clinic.png), [CH05M05CommandNode-source-preview-story-core_audit](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-story-core_audit.png), [CH05M05CommandNode-source-preview-story-utility](source-previews/saga.ch05.m05.command_node/run-01/CH05M05CommandNode-source-preview-story-utility.png); [receipt](source-previews/saga.ch05.m05.command_node/run-01/receipt.json).

**Physical source:** `Assets/Game/Scenes/OperationMaps/CH05M05CommandNode/PreparedEntities.unity`.
