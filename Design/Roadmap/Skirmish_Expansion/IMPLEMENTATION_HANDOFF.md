# Skirmish battles 4–120: implementation handoff

Updated 2026-09-21. **Documentation only. Expanded scenarios remain Planned.** This supplement closes the source/schema/per-battle handoff gaps in the September 20 design. Numeric tuning is an executable starting specification for future implementation, not measured balance. It preserves 120 stable catalog IDs and the accepted five-map/four-objective/three-army/two-start design.

## Audit result

The earlier package was a substantial mode design, but **not sufficient as a self-contained programming assignment for the remaining battles**. It had 120 matrix rows and shared gameplay rules, without a per-entry asset recipe, source-to-new-system ownership, resolved setup expansion, stable-ID migration, or individual battle implementation/test packets. An agent would still have had to decide how to replace the three-prototype loader and which mechanics belonged where.

This handoff adds:

| Read order | Source | What the agent gets |
|---|---|---|
| 1 | This document | Scope, numbering, evidence limits and authority |
| 2 | [TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md) | Exact existing seams, proposed types, assemblies, launch/production/save contracts |
| 3 | [OBJECTIVE_IMPLEMENTATION.md](OBJECTIVE_IMPLEMENTATION.md) | Four objective state machines, edge cases, enemy/ARIA implementation |
| 4 | [ROSTER_AND_ECONOMY_IMPLEMENTATION.md](ROSTER_AND_ECONOMY_IMPLEMENTATION.md) | Concrete source assets, producer decisions, role gates, accounting and reconciled economy |
| 5 | [MAP_IMPLEMENTATION.md](MAP_IMPLEMENTATION.md) | Typed anchors/routes/zones/layout authoring and map-specific placement tests |
| 6 | [Battle packets](Scenarios/README.md) | All 120 individually indexed briefs in 20 packets of six, including every remaining battle |
| 7 | [AGENT_WORK_PACKAGES.md](AGENT_WORK_PACKAGES.md) | Bounded tickets, dependencies, owners, exact handoff prompt and review gates |
| 8 | [ACCEPTANCE.md](ACCEPTANCE.md) | Existing X01–X33 gates plus new implementation/ARIA checks |

Machine-readable companions: [120-entry implementation manifest](IMPLEMENTATION_MANIFEST.csv), [117-entry remaining work list](WORK_QUEUE_004_120.csv), [360 scenario/size starting packages](INITIAL_SETUP_MATRIX.csv), [source roster inventory](ROSTER_SOURCE_AUDIT.csv). The original [SCENARIO_CATALOG.csv](SCENARIO_CATALOG.csv) remains unchanged because the current catalog builder consumes its column order. Planning files are not runtime assets and do not make a catalog row Playable.

## Resolve “4–120” without changing published IDs

Source inspected in the current working tree, including uncommitted Industrial Basin/library work:

| Existing player battle order | Current catalog ID | Legacy runtime `ScenarioIndex` | Meaning |
|---|---|---:|---|
| 1 | S001 | 0 | Desert Base small Base Assault prototype |
| 2 | S025 | 1 | City Crossroads small Base Assault prototype |
| 3 | S073 | 3 | Industrial Basin prototype wiring present; this audit did not run it |
| Not a player battle | None | 2 | Editor stress probe; never migrate it to S003 or a fourth player battle |

Define **handoff ordinals** 1,2,3 as S001,S025,S073, then ordinals **4–120 as every other stable S-ID in numeric order**. Thus work item 4 = S002, 5 = S003, 6 = S004, and 120 = S120. These ordinals organize work only; they are not new save IDs, UI labels or runtime indices. The 117-row work list resolves the exact mapping.

Literal S004–S120 is also fully covered by the battle packets. S002 and S003 are included because they are **not** implemented by the second and third prototypes. S001/S025/S073 still need expansion certification; existing prototype wins cannot qualify their larger, fog-enabled, full-roster definitions. Therefore the plan covers all 120 while identifying the 117 remaining new combinations explicitly. Do not label the expanded catalog 3/120 accepted from this audit.

## Current technical gaps confirmed in source

| Source (under `Assets/Game/Scripts/`) | Observed current behavior | Required action |
|---|---|---|
| `Configs/SkirmishBattleCatalogConfig.cs` | Catalog entries map to `PlayableScenarioIndex`; validates three prototypes at indices 0/1/3 | Add versioned definition references and readiness manifest; preserve legacy mapping in one adapter |
| `Editor/SkirmishBattleCatalogBuilder.cs` | Reads design CSV, then hard-codes only S001/S025/S073 Playable; validation expects exactly three | Replace overrides with validated publication manifest; do not mark all rows Playable from CSV existence |
| `Configs/SkirmishPresetConfig.cs` | Resource-name/index switches; 24 infantry cap and 900-second constant | Keep legacy preset path; expanded sessions use definition/size/objective data |
| `Configs/QuickGameConfig.cs` | `NormalizeForBaseAssault()` resets difficulty, fog, win condition and other settings to Defaults | New versioned expanded selection model; don't send FC/BT/CE through this normalizer |
| `Composition/SkirmishLaunchProjection.cs` | Queues Base Assault-normalized config; source constants identify Desert Base | Resolve immutable battle definition before launch; reuse readiness/scene transport |
| `Composition/MatchSceneView.Skirmish.cs`, `MatchSceneView.OperationMapLaunch.cs`, `CampaignMissionOperationMapReuseUtility.cs` | Explicit mission/index cases for the prototypes and logical map reuse | Replace new-content dispatch with mode/session payload and data lookup, not 117 more branches |
| `Systems/SkirmishRulesSystem.cs` | Main-base death, surrender, fixed deadline; repeatedly tunes roster/groups and scans losses | Split objective facts/rules/outcome, event accounting and setup; preserve legacy BA reducer for regression |
| `Systems/SkirmishPopulationPolicy.cs` | Substring categories allow soldiers and limited truck types; other queues get zero limit | Typed unit roles, queued/in-transit/live reservations, category/Supply/support accounting |
| `Systems/SkirmishCombatPolicy.cs` | Overlay tuning and small-force targeting | Per-role immutable battle tuning and shared spatial acquisition; no blanket rifle conversion |
| `UI/Contracts/AriaSkirmishContracts.cs` | Five squad touch targets; Base Assault-focused intents | Versioned public objective/army/production observations, paged Army UI and generic skills |
| `UI/Shell/Ecs/AriaSkirmishPlanSystem.cs` | Small-battle planner with tactical exceptions | Four shared objective policies plus common affordable role/route planning; no S-ID solution scripts |
| `Persistence/SkirmishSaveMigration.cs` | Version 2 normalizes back to Base Assault, Normal and full visibility | Explicit legacy conversion, versioned expanded setup and full checkpoint lifecycle |

These are source findings, not a Unity/device/ARIA run. Existing shared-source modifications belong to concurrent Skirmish work; this task edits only design documents. The date-specific [BASELINE](BASELINE.md) remains historical evidence, supplemented by this audit.

## Fixed decisions agents can implement without asking for design

Environment asset implementation now includes the [Demo 2 integration guide](../../Demo2_Asset_Integration_Guide.md) and [source/output planning manifest](../../VisualConfigs/Demo2_Environment_Asset_Manifest.json). Assign D2-A01/A02 within SK-11 to qualify a small IB warehouse/logistics/utility candidate. Integrate accepted modules through each map's owning source/config and presentation path; retain stable S-IDs, map IDs, roster roles and the DB first-ground-slice priority. D2-A03/A04 join the affected SK-11/SK-13 acceptance work. Art selection is resolved; actual output GUIDs, dimensions, ownership and validation must be recorded by the implementation owner.

- Keep the 120 identities and exact objective/army/start matrix. Default a first visit to Regular/Standard; recommended War remains a user-visible suggestion gated by device and checkpoint certification.
- Implement shared ECS systems and typed assets. Add no per-scenario C# controller and no 120-way gameplay/ARIA switch. Each packet lists the exact shared types it consumes.
- Both factions use the specified legal resources/roles/costs/caps. BT/CE keep the documented two extra defender watchtowers, and no free waves. ARIA is required to play and win through visible controls for every released entry.
- Each scenario has one `SkirmishScenarioDefinitionConfig`, one `ScenarioSetupConfig`, one resolved layout variant, EN/FA copy, and an evidence row. Global role/start/objective data is shared, not copied 120 times.
- Create the proposed Ground Staging producer using the explicit modular authoring decision in the roster document; do not wait for the user to name a nonexistent vehicle factory.
- Follow the economy reconciliation in the roster document: Materials fabrication and Fuel refining are separate real facilities, and their input/output/time budgets are explicit. Do not implement the contradictory earlier 300-Materials recipe alongside a 100-Materials/minute claim.
- Preserve legacy prototype saves/results separately. No automatic victory, reward, or progress migration to an expanded definition. Unsupported data fails with a specific reason and safe setup return.
- If a required feature is missing, its named work package is the next implementation task. Agents may implement that dependency and continue; they must not ask the owner to choose a rule already specified here or silently simplify the battle.
- Routine tuning/geometry corrections are within implementation: record measured evidence and changed config version, preserve objective semantics/roster identity/fairness and update the affected packets. New modes, objectives, paid content, expanded 200-catalog scope or destructive unrelated edits remain outside this task.

## Authority and completion

The supplements supply implementation detail for PLAN, BATTLE_CATALOG, MATCH_SETUP, MAPS, AI_AND_ARIA and DELIVERY. Explicit September 21 corrections in these supplements supersede conflicting older planning suggestions; canonical economy/architecture/performance contracts still govern. Original numeric budgets remain initial tuning values unless a correction is identified. Future agents update the single owning shared contract before changing derived packet values.

Build the first expanded working reference, then certify 24-map batches toward 120. Work ordinals are not a requirement to implement S002 before a necessary shared system or to finish air before a ground slice. Use the dependency graph in AGENT_WORK_PACKAGES. A data-valid manifest means **planned**, not implemented; manual wins, ARIA wins, recovery, localization, and device gates remain explicit for each released configuration.
