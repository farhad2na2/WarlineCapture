# Operations P1 strategic core

Recorded 2026-09-21. Package 1 owns city and profile transactions. It does not launch tactical scenes, bind HUD, or publish O001–O003 content.

## What landed

Assembly `Game.Operations.Strategic` (no engine references, references only `Game.Operations.Contracts`):

| System | Behavior |
|---|---|
| `OperationsRunInitializationSystem` | One run, six districts, starting tuples, damaged service sites, intact backup sites, open main routes, day-1 offers |
| `OperationsActionSystem` | Analyze, Community, Service, Patrol, Allocate, Deescalate. One AP each. Per-district or citywide limits. Service requires enemy influence ≤ 70 and does not set a mission milestone |
| `OperationsOfferDirectorSystem` | Up to two offers per district, three citywide priorities, slot gates, raid/breach intel gate, stable offer ids for the day |
| `OperationsDayAdvanceSystem` | Incident expiry, simultaneous pressure and adjacency, finale recovery, clamp after the summed delta, AP refill to 3, next offers and at most one new incident |
| `OperationsConsequenceSystem` | Victory / half partial / defeat / withdrawn deltas, harm caps, repair site facts, success milestones only on Victory |
| `OperationsRewardSystem` | Operations-envelope Credits and CommanderXP only. First clear 100/50, repeat 20 capped at 60/day, end-day bonus 20. Practice and non-victories grant nothing |
| `OperationsProfileStore` | Revision-checked JSON commit, pending save, crash abandon, retry save |
| `OperationsStrategicGateway` | Forwards the P0 `IUiOperationsGateway` commands into the session. Conclude stays refused until a tactical partial predicate exists |

State records use the ARCHITECTURE component names (`OperationsRunComponent`, district, site, route, milestone, offer, incident, attempt). They are plain structs. Burst `ISystem` wrappers are not in this package.

Host marker, 16 checks:

```text
[OperationsP1Validation] result=Passed checks=16
```

`Tools/Operations/check_p1.py` compiles the contracts, strategic systems, and `OperationsP1Checks` with the .NET SDK and requires that marker. The same checks are the Unity `executeMethod` `Game.Tests.Editor.Operations.OperationsP1Validation.RunFocusedValidation`.

## Game PM seams still closed

P1 did not edit these. Attaching the city to the shipping profile or to ECS worlds is an integration change.

| Seam | Why it is still blocked |
|---|---|
| `Assets/Game/Scripts/Persistence/SaveDataModel.cs` | The Operations envelope is not yet a field on `PlayerProfileSaveData`. Campaign, quick-game, and wallet fields stay outside the commit document |
| `Assets/Game/Scripts/Components/Game.Components.asmdef` | `IComponentData` / buffer types need a reference to `Game.Operations.Contracts` |
| `Assets/Game/Scripts/Game.Runtime.asmdef` | `ISystem` scheduling needs that reference and must not depend on UI or Composition |
| `Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs` | Mode launch is package 3 |
| `Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs` | Shared validator still rejects `operations` map and scenario ids |
| Existing asmdefs and the localization catalog | Unchanged |

`OperationsCommitJson` refuses account field names `credits`, `materials`, `fuel`, `intel`, `commandAuthority`, `starsEarned`, `campaignMissionProgress`, and `commanderXp`. Reward totals use `operationsRewardCredits` and `operationsRewardCommanderXp` inside the Operations document. Campaign and quick-game bytes are held beside the store and are not serialized.

## Editor self-validation still open

This agent ran the host checks on Linux. It did not launch Unity. Programmer 2 later validates only the shadow project `D:\Projects\WarlineCapture-Operations`. Do not open `D:\Projects\WarlineCapture`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP1Validation.ps1
```

The wrapper hard-codes `-ProjectPath D:\Projects\WarlineCapture-Operations`, resolves the Editor from that project's `ProjectVersion.txt`, and calls `InvokeUnityExecuteMethodValidation.ps1` with `-GuiLicensing` and the marker above. Refresh the shadow worktree onto this branch first; the P0 ensure script still defaults to the P0 branch.

Package 2 (tactical rules) and package 3 (launch, return, checkpoint adapters, HUD) are not implemented here.
