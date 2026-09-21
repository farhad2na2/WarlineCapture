# Demo 2 asset integration guide

Updated: 2026-09-21. Status: **Implementation instructions; assets not yet integrated or certified by this work.**
Owners: environment art for variants/materials; map owner for placement/surfaces; gameplay owner for interactive objects; mode owner for publication and evidence.

Read the [reuse decision and mode assignments](Demo2_Asset_Reuse_Plan.md) first. The [asset manifest](VisualConfigs/Demo2_Environment_Asset_Manifest.json) records verified vendor paths/GUIDs, proposed project-owned output paths, intended ownership and adoption priority. It is a planning manifest, not a runtime catalog, Addressables manifest or list of accepted assets. The [full scene inventory](AgentReports/Demo2AssetReview/prefab_inventory.json) is reference evidence; it is not an instruction to import every prefab.

## 1. Choose the asset and integration scope

1. Pick an existing mission/map from the reuse plan and record its current scenario ID, logical map ID, physical source binding, content version and presentation kind. DB/CC/IB can share physical source content: a local-looking scene edit may affect more than one mode.
2. Pick individual manifest entries or one of the four kits below. Record the source GUID and dependency hash, desired footprint, material overrides, role and consuming maps. Use the actual prefab under `Assets/Synty/PolygonBattleRoyale/Prefabs/`, rather than copying a Demo 2 scene instance with unknown overrides.
3. Assign one explicit owner: independent decoration, surface/blocker geometry, or gameplay object. Identify attached props separately: a roof generator or sign belonging to a destructible building follows that building's visual state and must not become a second independent presentation owner.
4. Inspect current worktree and Editor state. Preserve concurrent Gridlock and Skirmish work. Author in an isolated candidate source/config set with its own output paths; do not edit a generated migration candidate and expect it to survive rebuilding. Do not change a production definition merely to preview the art.

| Kit | Initial asset entries | First target | Adoption rule |
|---|---|---|---|
| Logistics | Warehouse, container, loaded pallet, medical crate | IB candidate yard; later CH02 supply/logistics sites and Operations D03 | Keep freight lanes, supply pads and production/delivery exits clear. Crates have no pickup/economy behavior unless a real objective owns it. |
| Utilities | Generator, transformer, radio tower, wire spool | CH02 Power Relay; CH03 signals; Operations D02/D03/D05 | Visual cluster around a typed service/relay anchor. Repair/Scan/Interact state comes from gameplay definitions. |
| Perimeter | Barrier, base wall, wire fence, guard tower | AP and Chapter 4 compounds; Operations D06 | Declare openings and blocking policy. A decorative tower cannot silently become a weapon or supply extra defender capacity. |
| Crossing | Bridge 01/02, broken bridge, quay slab/wall | Operations D04 | Two independent traversable land routes; broken pieces stay outside mandatory routes. No naval or physical bridge-collapse feature. |

The first integration is one small Logistics + Utilities yard in an IB candidate. It does not move Skirmish's first expanded ground-battle milestone from DB to IB. Other teams can continue with established desert assets while the kit is being qualified.

## 2. Create project-owned prefabs and materials

The manifest proposes these destinations; this documentation task has not created them:

```text
Assets/Game/Prefabs/Environment/Demo2Adapted/<Family>/ENV_D2_<Name>.prefab
Assets/Game/Art/Environment/Demo2Adapted/Materials/
```

Use the Unity Editor or connected Unity CLI/Pipeline with the repository's Unity skill. Before editing, run `rtk proxy unity status --project-path /Users/farhad/Projects/WarlineCapture --format json`, inspect available commands and confirm Edit mode and the intended authoring scene. A running/paused game is not an authoring workspace. Preserve it and use an isolated project/candidate workflow if necessary. Keep Hub open and signed in. Do not hand-edit scene/prefab/asset YAML with a reachable Editor.

For an independent decorative asset in the Editor:

1. Locate the exact source prefab from the manifest in the Project window. Create a **Prefab Variant** in the proposed destination, or create a project-owned wrapper containing a linked vendor-prefab child when a separate visual root is needed. Use the variant/Editor prefab workflow; do not use Apply All to write back into the vendor prefab.
2. Keep the wrapper/root transform at identity. Put measured visual alignment offsets on a dedicated visual child. Fit the base to the actual ground, measure renderer bounds and compare doors, barriers and equipment against existing infantry and vehicles. Prefer uniform scale; do not copy arbitrary scale from a display scene or stretch vehicles to fill grid cells.
3. Reuse shared source materials initially. Where the approved desert palette requires changes, duplicate only the materials that need overrides into the project-owned material folder and assign those overrides in the variant. Preserve shared mesh/texture references where possible; do not clone a texture atlas per prop or modify the shared vendor material globally.
4. Match warm terrain, restrained saturation, roughness and weathering under the actual match lighting. Remove or adapt inappropriate signage/faction markings in project-owned assets. Inspect glass, transparency, shadows, normal orientation and emission. The reviewed Battle Royale samples use `Synty/Generic_Basic`, while the sampled desert tent uses URP/Lit: do not assume identical shader or instancing behavior, or run a bulk shader conversion without evidence.
5. Inspect every collider, renderer and component. Keep or replace colliders only according to the map's surface/gameplay ownership route below. Strip unneeded lights, cameras, audio and demo scripts from the project-owned variant if present. Do not indiscriminately delete colliders from a gameplay object or duplicate them across owners.
6. Save through the Editor, commit the prefab/material `.meta` files with their assets, and record output GUIDs and overrides in the manifest. A rerun must reuse those assets/GUIDs rather than delete and recreate them. Keep variants out of runtime Resources unless the existing owning subsystem explicitly requires that path.

For automated authoring, write a bounded Editor builder that accepts explicit source/output paths, loads the source with `AssetDatabase`, instantiates it with `PrefabUtility`, applies recorded overrides, saves only project-owned outputs and destroys temporary objects in `finally`. Use prefab APIs that preserve source linkage when producing variants; a wrapper is a different intentional output. Record all touched paths before execution. No Demo 2 builder or menu is claimed to exist yet.

## 3. Bind the correct kind of ownership

Follow the [authored ECS workflow](Architecture/operation_map_authored_ecs_workflow.md) and [runtime ownership chain](Architecture/operation_map_runtime_ownership_chain.md). Resolve the target definition's actual presentation kind and residency mode before choosing the authoring path; the asset pack does not select a renderer architecture.

| Intended role | How to add it | Required evidence |
|---|---|---|
| Independent decoration | Add the adapted prefab to the selected map's source/config under its correct presentation owner. In the EntityScene path use `RenderOnly` and stable presentation identity. Keep it out of gameplay registries. | Exactly one rendered owner; no unintended selection, collision, target, income, AI behavior or hidden navigation blocker. |
| Building attachment | Place the visual under its gameplay building's appropriate intact/destroyed visual root. | It follows owner transform, damage/destruction and culling; no duplicated independent row or floating surviving roof prop. |
| Terrain, road, bridge, ramp or static blocker | Author the matching surface/proxy in the operation-map source using the existing `MapBakeGroupAuthoring` roles. Keep surface/proxy ownership out of the entity-presentation SubScene. | Renderer/surface/blocker agreement, grounded joins, real unit travel and declared cover/line-of-fire semantics. |
| Interactive or destructible building/site | Reuse the existing gameplay definition/authoring pattern, with one authoritative owner, stable identity, measured footprint, health/faction/selection policy and visual states. Bind its placement and scenario role through existing configs. | Normal player interaction, correct objective facts, targetability, destruction/blocker policy, save/retry and cleanup. |
| Gameplay vehicle | Separate roster task. Use the supported `UnitGridAuthoring`/catalog path, movement/rig/weapon/transport/fuel/producer data and ECS conversion. | Full production-to-death lifecycle, controls, role/counter, faction visuals and device cost. Deferred for this environment adoption. |

For existing-map migration, placement configs remain protected inputs until the current tracker retires them. Create/rebuild the candidate from the owning source/config and preserve one-to-one placement identity. Do not add a manual extra gameplay entity beside the placement-driven entity. Visibility slots and renderer proxies never own health, faction, grid, production or objective state.

Static barriers need an explicit policy: decorative/passable, permanent blocker, or destructible obstruction. A fence mesh cannot decide that policy from its name. A destroyed wreck must match the authored occupancy policy and remain compatible with alternate routes. Keep all mission-critical props visible/readable at gameplay zoom.

## 4. Place and connect the map content

1. Stage the small kit beside established desert buildings and representative units. Inspect near/far camera views before spreading it across a map. Keep authored roads, base pads, runway strips, landing zones, convoy holding bays and extraction areas reserved.
2. Fit terrain and proxy surfaces to the visuals. For bridges, check both deck edges, support/abutment contact, ramp seams, slope, clearance, actual heavy-vehicle width/turning, two-way passing and the independent alternate crossing. Water cannot become a fallback traversable surface.
3. Bind typed objective/route anchors in the target map definition. Scenarios reference the map and role IDs; they must not locate a transformer or tower by GameObject/prefab name. Preserve the established Mission → ScenarioSetup → OperationMap ownership chain.
4. For a repair target, keep the generator/transformer visual separate from the authoritative site state. Bind the existing or planned shared repair interaction to that site's role; show progress from real facts. Swapping a material or enabling a mesh does not complete a repair objective.
5. For Gridlock, preserve the current crew/work/obstruction/relief-route behavior. The source currently has `CH02M01GridlockWorldBuilder` and attempt-owned placements: a later art pass changes the owning builder's presentation inputs, not generated prefab YAML or a shared city obstacle. Recheck actual clearing, traversal and retry cleanup.
6. Keep existing logical map/scenario IDs for art revisions. Update content version, source/dependency hashes and affected generated outputs through their owners. A genuinely new physical layout follows the existing identity contract; do not invent a sixth Skirmish map for this kit.

## 5. Rebuild only the target map's content

The following source files are navigation aids, not universally safe commands to run on any selected map:

| Existing owner | Scope to check before calling |
|---|---|
| `Assets/Game/Scripts/Editor/StaticMapPresentationBaker.cs` | Its public default menu methods construct current desert-map inputs; use the intended map's explicit input path through an owning Editor builder. |
| `Assets/Game/Scripts/Editor/OperationMapCurrentMapBaker.cs` | Current-map orchestration, not a generic selector for IB or an Operations district. |
| `Assets/Game/Scripts/Editor/MapBuildingPlacementBakeEditor.cs` and `MapVehiclePlacementBakeEditor.cs` | Match/current-map entry points and protected placement ownership must be checked before reuse. |
| `Assets/Game/Scripts/Editor/OperationMapMinimapRasterBaker.cs` | Despite “Selected Map” in its menu label, inspected source loads fixed definition/surface paths and writes the desert-map output. It also requires zero-degree minimap orientation. Parameterize in its owner or use the target map's established builder; selecting another map alone is insufficient. |
| `Assets/Game/Scripts/Editor/OperationMapAddressablesLayoutBuilder.cs` | Existing desert-map paths/groups. A new district needs correctly scoped configuration, not a blind call to this default entry point. |

Use the selected map's current builder chain for placements, surface/blockers, metadata, presentation, minimap and package references. If it lacks explicit map inputs, implement the narrow parameterization in the owning tool and validate output isolation before running it. Do not temporarily repoint shared constants or overwrite a frozen rollback package. Reinspect source because these tool limitations may change after this review.

For static presentation, use map-scoped manifests/chunks. For EntityScene/virtualized presentation, regenerate the relevant identities/database/bake outputs and rerun readiness and transform/material/bounds parity. Never leave both the original source renderer and its baked replacement active. Do not add `Demo2.unity`, its lighting/volume/water assets or the entire pack folder to build scenes/Addressables merely to obtain these props. Audit actual dependency closure and existing shared-art ownership.

After content changes, update source/content hashes, minimap and preview imagery for the consuming map. Preserve old checkpoint/content compatibility or use the mode's explicit incompatible-content recovery; do not silently load a saved attempt into changed obstruction geometry.

## 6. Validate and record acceptance

Use existing [performance budgets](Architecture/performance_regression_contract.md) and the consuming mode's acceptance plan. Do not create unmeasured triangle or draw-call limits in this guide. The reviewed tank has 23 renderers and sampled props have no LODGroups; measure real cost and add appropriate shared LOD/culling/instancing only where compatible with the current presentation path.

| Gate | Evidence required for affected content |
|---|---|
| D2-V1 Source and output | Source GUID/path resolves; output GUID and dependency hash recorded; no missing meshes/materials/scripts; vendor and protected baseline unchanged; repeat authoring preserves GUIDs. |
| D2-V2 Art/readability | Desert comparison under identical lighting/camera/unit load; near/far and low/high pitch; team/selection/objective clarity; EN/FA HUD safe areas; no inappropriate signage or hidden units. |
| D2-V3 Ownership and geometry | One owner per visual/gameplay object; correct attachments/destruction; collider/surface/blocker agreement; largest vehicle and infantry traverse actual routes; producer/air/convoy clearances. |
| D2-V4 Build and lifecycle | Target map only; updated hashes/preview/minimap; correct build dependencies; no duplicated source/baked rendering; two launch/exit cycles, retry/replay and compatible checkpoint behavior. |
| D2-V5 Device cost | Same-load baseline/candidate comparison of CPU/GPU, renderer/material work, memory, loading and package size; supported-device tests under the existing budgets. |
| D2-V6 Mode behavior | Affected manual gameplay and required ARIA wins, objective loss/interaction/recovery cases and cross-mode regressions for shared source changes. Existing victories on old geometry do not certify the new revision. |

Unity executeMethod/test/build/capture runs must follow root `AGENTS.md`: on macOS use `rtk proxy Tools/CI/invoke_unity_macos.sh` with explicit timeout/log, no direct Unity executable or `-batchmode`; retain full logs and check exit status plus the actual suite's required pass marker. Use the checked Windows wrappers on Windows. Do not replace these with CLI build/run/test. Never close an active Editor, reset IPC or kill Hub to make room for validation. Proposed validators must exist before documenting them as executable commands.

Store evidence in `Design/AgentReports/Demo2AssetReview/Integration/<kit>/<map>/<revision>/` (proposed output), including manifest revision, source/output GUIDs, map/scenario IDs, source/build/content hashes, changed paths, before/after captures, wrapper logs, actual result/seed/locale/device, failures and rollback instructions. Record art and runtime statuses separately. A working preview is `Authored`, not `Accepted`.

## 7. Delivery packages and handoff

These are shared art work items inside existing mode packages; they do not add missions, maps or gameplay systems.

| ID / status | Owner and dependency | Deliverable / exit |
|---|---|---|
| D2-A01 — Planned | Environment art; source review available | Resolve shortlist, create project-owned variants/materials, record GUIDs, compare desert scale/palette. D2-V1/V2. |
| D2-A02 — Planned | Map/presentation owner; A01 | Candidate IB logistics/utility yard, correct placement/surface/presentation ownership and isolated rebuild. D2-V3/V4 plus initial device-cost comparison. |
| D2-A03 — Planned | Mode map/content owners; A02 accepted for reuse and relevant mechanics ready | Apply accepted kit per Campaign chapter, Skirmish map or Operations district assignments; certify bridge geometry separately for D04. D2-V1–V6 for affected content. |
| D2-A04 — Planned | Integration/QA; A03 | Publish only accepted content revision, update art/manifest evidence, affected mode readiness and regression record; retain supported fallback. |

Skirmish attaches A01/A02 to SK-11/E3 map work and A03/A04 to SK-11/SK-13/E8. Operations attaches source/greybox review to P2 and adapted district content to P6/B12/B30/B60. Campaign attaches the asset-use manifest to each future mission's authoring plan; Gridlock's optional later art pass belongs to G5 and repeats affected G6 evidence. Shared asset qualification does not require completion of all Skirmish gameplay or the entire 120-battle catalog.

Suggested implementation handoff:

> Implement D2-A01/A02 or the assigned kit/map revision using this guide and the target mode plan. Inspect current source and Editor ownership, preserve other work, resolve verified source GUIDs, create only project-owned assets through Editor APIs, classify each visual/surface/gameplay owner and rebuild only the target map. Keep original IDs and gameplay contracts. Record actual visual/navigation/build/device and affected manual/ARIA evidence, failures and output hashes. Update manifest/status only to the level demonstrated; do not certify a mission from asset availability.
