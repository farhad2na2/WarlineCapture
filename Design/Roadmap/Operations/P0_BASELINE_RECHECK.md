# Operations P0 baseline recheck

Recorded 2026-09-21 on `main` tip `1e0eb9da2120d31e620d1022c6cf2b9eda80c651`. Working tree for this package started clean. No Unity Editor launch was available in this Linux agent; type claims below were verified by reading source.

## Recheck vs BASELINE.md

Every BASELINE row still holds on this tip. Confirmed existing types:

| Claim | Verified type / path | Result |
|---|---|---|
| Dashboard placeholders | `Game.UI.Runtime.OperationsDashboardScreenView` aliases `PatrolButton` to `blackMarketButton` | Unchanged |
| District actions | `Game.UI.Runtime.DistrictOperationActionKind` = Patrol, DroneScan, Aid, Raid, Repair | Unchanged ordinals 0–4 |
| Campaign-named screen | `CampaignOperationsScreenView` remains Campaign mission select | Do not host city state here |
| Map ID validator | `Game.Configs.OperationMapIdentityRules` accepts `skirmish` / `ch##` only | Rejects `opmap.operations.*` and `scenario.operations.*` |
| Campaign outcomes | `Game.Missions.Contracts.MissionOutcomeKind` = None, Victory, Defeat | Cannot express Partial / Withdrawn / TechnicalFailure |
| Profile save | `Game.Runtime.PlayerProfileSaveData` has Campaign progress, no `operations` field | Shared seam |
| Shared launch | `MatchSceneView.OperationMapLaunch.cs` validates IDs through `OperationMapIdentityRules` | Shared seam |
| Neutral faction | `Game.Components.FactionIdentity.NeutralFactionId = 0`, player = 1 | Civilians already use faction 0 / `role.civilian.protected` |
| Tactical commands | `Game.Tactical.Contracts.TacticalCommandMode` includes Hold, Stop, Scan, Board | No Repair or Interact |
| Scenario authoring | `ScenarioSetupConfig` + `ScenarioUnitEntryConfig` remain reusable records | Campaign `TryValidate` still campaign-oriented |
| Feature IDs in source | `feature.operation_map`, `feature.unit_catalog`, `feature.tactical_commands` | Only those three Campaign IDs were found |

## Dirty / shared file inventory

This P0 branch owns only Operations contracts, Operations test assembly, and Operations roadmap docs.

Shared files that later packages will need, and that this branch **must not edit**:

| File | Why it is shared | P0 action |
|---|---|---|
| `Assets/Game/Scripts/Persistence/SaveDataModel.cs` | Profile envelope for Campaign / settings / quick-game | Document required `operations` field |
| `Assets/Game/Scripts/Persistence/SaveService.cs` | Serialized commit boundary | Blocker for P1 |
| `Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs` | Mode-tagged launch | Blocker for P3 |
| `Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs` | Published map/scenario grammar | Blocker before any Operations launch |
| `Assets/Game/Scripts/Configs/Game.Configs.asmdef` and other existing asmdefs | Directional references to `Game.Operations.Contracts` | Listed in `P0_SHARED_SEAMS.md` |
| `Assets/Game/Scripts/UI/Screens/OperationsDashboardScreenView.cs` | Placeholder bindings | Leave for P3 UI lane |
| `Assets/Game/Scripts/UI/Screens/DistrictDetailActionsScreenView.cs` | UI enum owner | Compatibility mapping lives in contracts |
| Localization catalog builders | EN/FA keys | Content lane after Game PM review |
| Entire `Design/Roadmap/Skirmish_Expansion/` tree | Concurrent Skirmish work | Untouched |

In-progress Skirmish files on `main` (E0 stress, 120-battle library, City Crossroads) were left intact. This package adds no Skirmish types and does not assume Skirmish checkpoint facilities exist.

Windows validation is later and only on the shadow worktree `D:\Projects\WarlineCapture-Operations`. The shared checkout `D:\Projects\WarlineCapture` stays with Programmer 1 / other tracks; do not open or lock its Library. See [P0_SHADOW_PROJECT.md](P0_SHADOW_PROJECT.md).

## False-type watch list

These names appear in Operations design docs as **new** work. They were **not** found as existing gameplay types:

- `OperationsRunComponent`, `OperationsCommandSystem`, `OperationsSaveData` on the profile
- `IUiOperationsGateway` in `Game.UI.Contracts`
- Tactical `Repair` / `Interact` command modes
- Civilian escort or evidence-carry systems
- `OperationMapIdentityRules` operations-namespace overloads

P0 therefore adds Operations-owned contracts and ledgers instead of pretending those types already compile in shared assemblies.
