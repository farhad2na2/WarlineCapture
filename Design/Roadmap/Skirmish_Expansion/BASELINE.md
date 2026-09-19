# Expansion baseline and technical decisions

Reviewed 2026-09-18 at `c69865400`. This is source/config inspection and review of existing reports, not a new gameplay or device run.

## Existing evidence

[Prototype completion](../Skirmish_Prototype/COMPLETION.md) records a working 1v1 Base Assault loop in both languages, 24 infantry plus one starting armored car per faction, real logistics, and played outcomes. Its 309 checks cover a historical build; later fixes have their own reports. None certify this expansion or hundreds of units. Device performance, independent-player comprehension, and pacing remain open.

Recent fixes/reports: [recruitment and camera interruption](../../AgentReports/SkirmishRecruitmentDelivery/README.md), [building selection](../../AgentReports/SkirmishBuildingSelection/README.md), [resource exchange](../../AgentReports/ResourceExchangeMobile/README.md). The latest terrain check has baked-map regression coverage and a successful Skirmish startup; live preview-and-confirmation verification was incomplete. Carry it into E0 rather than calling it passed.

## Source findings and required changes

Paths below are relative to the repository root. These observations identify work; they are not profiler measurements.

| Current owner | Observed constraint | Expansion change |
|---|---|---|
| `Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs` | Static 24-infantry cap, static 900-second duration, scalar rifle/car/tower balance; one resource preset name | Versioned match definition with roster, economy, size, map, objectives, visibility and difficulty references. Preserve the existing preset as a regression baseline. |
| `Assets/Game/Scripts/Systems/SkirmishCatalogPolicy.cs` | Resolves one preset and its allowlist | Resolve the immutable selected session definition; pre-index supported IDs. Do not load resources to repeatedly answer UI availability. |
| `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` | Name-based soldier/truck categorization; supports soldiers, two tray trucks and one tanker; other queued types get limit zero | Typed roster roles and budget costs. Per-faction alive/queued/in-transit reservation accounting; release exactly once. Preserve spawn connectivity. |
| `Assets/Game/Scripts/Systems/SkirmishCombatPolicy.cs` | `ApplyRoster` treats every name containing `soldier` as the same rifle; target assignment scans candidates per squad/member | Per-definition tuning and target domains; preserve specialist roles. Profile and replace broad target scans with the appropriate shared spatial query. |
| `Assets/Game/Scripts/Systems/SkirmishSquadAssignment.cs` | Four infantry slots and one car slot; managed count array and entity scan | Stable squad/group IDs separate from visible HUD positions; bounded UI projection; event-based assignment. |
| `Assets/Game/Scripts/Systems/SkirmishRulesSystem.cs` | Two base references; loss tracking checks each unit against tracked entries; several policies run from the update loop | Event/tag-based lifecycle accounting or indexed membership; mode objective interface. Generalize factions only for the later team package. |
| `Assets/Game/Scripts/Systems/SkirmishWorldSetup.cs` | Supply anchors and AI production/squad plans tuned for the tiny roster; hardcoded factions 1/2 | Data-driven layout, producer queues, logistics capacity and force composition. Retain fail-closed startup and ownership isolation. |
| `Assets/Game/Scripts/Systems/AITargetingSystem.cs` | Early Skirmish policy override bypasses the regular target scheduling path | Ensure expansion uses the measured scalable path rather than assuming existing generic optimization applies to Skirmish. |
| `Assets/Game/Scripts/Systems/UnitSpatialIndexBuildSystem.cs` | Spatial index exists; 2048-entry bound and 0.12-second refresh; includes more than combatants | Measure real entity membership/overflow. Define a complete bounded fallback; no silently unselectable or untargetable units. A 2048 bound is not evidence that 500 units perform well. |
| `Assets/Game/Scripts/Systems/UnitEngagementSystem.cs` | Burst jobs and spatial map already exist; acquisition cadence 0.12 seconds, temporary maps and synchronization | Profile allocation and completion cost in mixed fights. Reuse working acquisition semantics before considering job/layout changes. |
| `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs` and `UnitPathfindingScheduler.cs` | Adaptive queued job budget and long-distance infrastructure exist | Measure queue age, mass-order latency, mixed footprints and congestion. Preserve scheduling/traversal contracts during optimization. |
| `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs` and companion systems | Render/animation budgeting exists | Validate visual stability and sustained device cost with full roster and rapid camera motion, especially aircraft; existence is not certification. |
| `Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs` | New Skirmish check samples slope/height; desert rule includes 0.75 m elevation above grid origin; bypassed for scripted startup | Verify current preview/confirm first. Before new terrain, replace absolute flat-map assumptions with authored allowed foundation surfaces. Apply a separate startup layout audit to authored structures. |
| `Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs`, `SkirmishMatchComponents.cs` | Isolated session, saved setup snapshot, fixed two-side outcome shape | Extend versioned config/result IDs and migration. Do not put transient Entity references in saves. |

## Roster audit ledger to produce in E0/E1

Enumerate all **51 UnitGrid** and **23 BuildingDefinition** assets in `Assets/Game/Configs/Prefabs`. There are also other asset types in that directory. Inventory counts include civilians, crew, appearances and structures unrelated to conventional battle; do not use the count as a finished-roster claim.

For each entry record:

`StableId | prefab/config | display/localization keys | faction eligibility | role/variant | producer | prerequisites | cost | batch size | duration | delivery | Supply | target domains | damage/armor policy | vision/detection | movement footprint | transport | death/wreck | portrait/LOD | tested state | disposition/blocker`

Resolve IDs using a stable asset/catalog identity rather than substring matching. First identify whether missing YAML fields use meaningful serialized defaults. An asset's `canRequest`, weapon description, or `canAttack` value does not prove the producer, targeting, animation or delivery works. Civilian configs even contain attack flags; eligibility must be explicit.

Ground candidates: Rifleman variants, Heavy Gunner, Marksmen, Assault Breacher, Ghillie Rocketeer, Light Armored Car, three APCs, Battle Tank. Full roster candidates additionally include both attack helicopters, transport helicopter, drone, two jets, two missile launchers, Radar Tank, transport plane, and three truck types.

Barracks currently references one production entry. Helipad has three production entries; Airport has four. Resolve their actual prefab IDs, gating and delivery behavior before finalizing the expanded build menu. A new producer is a design/implementation task, not a claimed existing factory.

## Explicit design decisions

| Decision | Recommended default | Why / reconsider when |
|---|---|---|
| First opponent structure | 1v1 AI | Prove mixed-unit tactics before multiplying faction ownership and target diplomacy. |
| Main product scale | War, about 224 combat entities at category caps | Reaches the requested scale while leaving a smaller supported option. Device certification is required. |
| Same model variants | Role family with variant picker | Uses assets without requiring many near-identical primary buttons. |
| Unlock model | Three match readiness stages plus facility/resource requirements; all supported roster inspectable | Accepted direction in [Unlocks and Upgrades](UNLOCKS_AND_UPGRADES.md); no campaign grinding for Skirmish access. |
| Upgrade model | Facility capabilities and a small set of shared category upgrades paid with Materials | Applies to existing/future eligible units, resets for a new match; individual veterancy and permanent levels deferred. |
| Information | Full vision in ground slice; shared fog/intel before recon claims | Gives sensors a real purpose and avoids asymmetric AI knowledge. |
| Economy | Existing Materials/Oil/Fuel with automatic logistics | Expands an existing mechanic; avoids adding workers and another economy simultaneously. |
| Infantry scale | Four-person squads, several squads per group | Reuses the recruitment convention while reducing touch burden. |
| New frontline rule | Capture points drain tickets; no capture income bonus initially | Adds a clear reason to contest several areas without combining two hidden advantages. |
| Long sessions | Reliable pause/background first; then versioned match checkpoints before War becomes the mobile default | Current interruption recovery returns to setup. Do not promote a long-session default while still discarding interrupted battles. |

No task changes paid audio assets automatically. Prefer concise existing localized feedback for the first ground slice; scripts and new recording needs are a separate auditable content inventory.
