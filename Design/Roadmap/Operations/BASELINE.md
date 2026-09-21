# Operations source baseline

Read-only source audit, 2026-09-21. No Unity launch or runtime verification was performed for this planning task. The working tree already contained Skirmish changes; this package does not alter them.

Paths below are relative to the repository root. **Existing** means observed in source, not accepted as a complete Operations implementation. All types proposed elsewhere in this package are **new** unless explicitly listed here.

| Existing source | Observed responsibility | Implementation decision |
|---|---|---|
| `Assets/Game/Scripts/UI/Screens/OperationsDashboardScreenView.cs` | Serialized dashboard references; Raid opens confirmation; End Day opens report; callbacks close modals | Replace placeholder behavior with request/result binding. Opening a report must not be treated as simulation. Preserve shell navigation and visual layout. |
| `Assets/Game/Scripts/UI/Screens/DistrictDetailActionsScreenView.cs` | Patrol/DroneScan/Aid/Raid/Repair action UI and `ActionRequested` event | Add a binder to a typed Operations gateway; move new domain action enum to contracts with an explicit compatibility mapping. |
| `Assets/Game/Scripts/UI/Popups/EndOfDayReportPopupView.cs` | Report display/action surface | Bind committed day-report data; Back/Continue cannot apply the day twice. |
| `Assets/Game/Scripts/UI/Contracts/UIRoute.cs` | Separate `Operations`, `DistrictDetail`, `Campaign`, `MissionBriefing`, `Match` routes | Operations remains distinct from the Campaign mission-select UI. |
| `Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs` | Campaign mission-selection screen despite its name | Do not put persistent district logic here. |
| `Assets/Tests/Editor/OperationsDashboardScreenTests.cs` | Route, prefab, shared-header, and modal-mount assertions | Preserve these and add actual action/state/result tests. These existing tests do not prove a simulated city. |
| `Design/AgentReports/OperationsChildNavigation/README.md` | Historical EN/FA Back-navigation evidence for Operations child pages | Preserve route history and popup cancellation; revalidate touched pages. |
| `Assets/Game/Scripts/Configs/OperationMapDefinition.cs`, `OperationMapCatalogConfig.cs`, `OperationMapCatalogEntryConfig.cs` | Map identity, delivery, spatial and presentation references | Reuse map loading/readiness; do not create an Operations-only scene loader. |
| `Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs` | Map/scenario validators currently accept chapter and Skirmish namespaces | Add explicit `operations` forms with tests before using proposed IDs; the current validator rejects them. |
| `Assets/Game/Scripts/Configs/ScenarioSetupConfig.cs` | Unit groups, roles, routes, required anchors, restrictions, defense/extraction/breach data | Reuse neutral authoring records. Its current mission runtime and runtime consumers are Campaign oriented. Add Operations graph data separately. |
| `Assets/Game/Scripts/Configs/ScenarioMissionRuntimeConfig.cs` | One delayed-wave record and Campaign build/runtime fields | Do not pretend it already supports arbitrary Operations objective graphs or multiple trigger waves. |
| `Assets/Game/Scripts/Configs/MissionDefinitionConfig.cs` | Campaign objective/star/reward/readiness configuration | Reuse conventions, not Campaign IDs or settlement ownership. Operations gets a separate definition and outcome type. |
| `Assets/Game/Scripts/Missions/Contracts/MissionContracts.cs` | Campaign payload/result enums; launch origin has FirstLaunch/CampaignOperations; outcome only Victory/Defeat | Introduce independent Operations payload/outcome. Preserve numeric enum values and old serialization. |
| `Assets/Game/Scripts/Runtime/Missions/CampaignMissionLaunchSystem.cs` | Campaign readiness, attempt cleanup, runtime setup | Factor reusable readiness/role cleanup only behind neutral contracts. Never launch Operations as a fabricated Campaign mission. |
| `Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem*.cs` | Campaign-specific mission phases and specialized rules | Reuse proven primitive behavior through narrow extraction; do not add O001–O060 switch cases. |
| `Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs` | Shared scene transition and map-launch composition, currently also touched by Skirmish work | One integration owner adds mode dispatch using contract payloads; views stay binders. Coordinate shared edits. |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Mission.cs` | Campaign-root query and Campaign result projections | Add an Operations gateway/projection rather than reusing cached Campaign roots. |
| `Assets/Game/Scripts/Persistence/SaveDataModel.cs` | Profile, settings, quick-game, Campaign progress; no Operations run model | Add optional versioned Operations envelope to profile and migration defaults. |
| `Assets/Game/Scripts/Persistence/SaveService.cs`, `JsonSaveRepository.cs` | Separate profile/settings/quickgame files; atomic single-file write exists | Reuse file infrastructure. Atomic file replacement is not a multi-file or stale-writer transaction; add serialized profile commit handling. |
| `Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.cs` | Existing Campaign progress/settlement pattern | Reference for regression and token handling, not owner of Operations rewards. |

## Foundations still required

There is no evidence in this inspected path of a completed Operations run state, district tick, offer director, action-point budget, tactical launch/result settlement, or Operations recovery model. Plan and verify those explicitly. Existing UI labels are not a backend specification.

Confirmed shared integration risks:

1. Map/scenario ID validation needs expansion; avoid weakening validation for arbitrary strings.
2. Campaign result types cannot express Partial/Withdrawn. A failed map load is not tactical defeat.
3. Campaign extraction/breach code contains mission phases and IDs. Extract shared primitives with M04/M05 regressions; do not copy their entire systems.
4. The economy design allows persistent Credits/Command and tactical Materials/Fuel/Oil. Existing fields still include legacy wallet materials/fuel/intel and tactical credits. New Operations logic must follow the design and keep those legacy fields untouched.
5. `OperationSupply` is a named inventory item. District Supply readiness is a metric. They need separate types, fields, UI copy, and telemetry.
6. Current dashboard aliases `PatrolButton` to `blackMarketButton` and `RaidButton` to `commandLogButton`. Audit serialized builder bindings and explicit semantics before wiring real spending or deployment.
7. Existing `Game.Runtime`, `Game.Components`, `Game.Configs`, UI contracts/shell, and Composition assemblies impose dependency direction. Do not place gameplay inside a Canvas view or add runtime-to-UI implementation dependencies.

The existing accepted Campaign/Skirmish evidence remains scoped to its own configurations. New Operations-specific systems, authored content, and device behavior need fresh evidence.
