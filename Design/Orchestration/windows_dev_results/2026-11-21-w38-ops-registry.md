# W38: Operations mission-keyed runtime registry, O001 unchanged (gap-table step 2)

Branch `missions/win-w38-ops-registry`, cut from integration fd231f6e7. Two commits:
- `e1e641832`: the registry, hard-code removal, reducer split and tests;
- `c1b24e0aa`: a CS0136 local-name fix in the HUD restriction gateway.

It is Operations-only shared code under the owner's standing OK and the 2026-11-19 Operations decisions. There are no unit-stat, rule or Skirmish/Campaign changes. `OldQuarter.asset` and `StreetSignals.asset` are byte-identical to integration, and the diff adds no LFS-tracked files.

## What changed
- **`Configs/OperationsMissionRegistry.cs` (new):**
  - Maps a catalog `operation.*` ID to its native config under `Resources/Operations/Missions/<code>`.
  - O001 keeps its shipped path `Operations/StreetSignals` through a one-row legacy table, so its asset, GUID and checkpoints don't move.
  - Also provides the copy prefix (`operations.o001.`), the new-run seed (the catalog canonical seed, 1102 for O001) and a cached `IsRegistered`.
  - There is no mission-ID switch.
- **`OperationsReconMissionConfig.TryValidate`:**
  - Identity, family (RECON), scenario, Operations map and deadline now come from the catalog row (`OperationsCatalogIndex`) instead of O001 literals.
  - Force counts come from the catalog package tokens through a finite size table: FP_LIGHT has 16 units including 2 recon, EP_CELL has 20.
  - `requiredScans` (3) replaces the literal 3.
  - Family HUD restriction flags are serialized data, defaulting to O001's values: building, production, economy, transport and air disabled; logistics and unrelated controls hidden; squad mask 1.
- **Hard-codes removed:**
  - `MatchSceneView.Operations.cs:9`: `IsOperationsSession` now asks the registry.
  - `OperationsMissionPresentationSystem`: the config load, `ContentAccessRuntime.Evaluate`, the new-run seed, the offer lookup and the copy prefix are all keyed by the selected mission.
    - The dashboard sends no mission ID yet, so `selectedMissionId` defaults to O001. O002 wires the selection there.
  - `UiShellEcsGateway.MissionHudRestrictions.cs:23`: the mission ID and HUD flags come from a new `OperationsMissionIdentityComponent` on the session root.
    - The launch projection adds it.
    - It is not part of the checkpoint image (the codec captures named parts only), so the checkpoint bytes are unchanged.
  - `OperationsReconMissionConfig.ResourcePath` is kept for existing callers.
- **`Runtime/Missions/OperationsObjectiveGraphReducer.cs` (new):**
  - Holds the SCAN, INTERACT (evidence recovery), evidence carry and EXTRACT/terminal steps, extracted verbatim from `OperationsReconObjectiveSystem`: same arithmetic, same order.
  - The system keeps native fact gathering (alive, dismounted, reachable, hostiles near the exit and the evidence) and calls the steps.
  - O002 nodes (ESCORT/HOLD/PROTECT/CLEAR) add steps here.
- **Not done in W38:**
  - the mission-parameterized harness (`OperationsReconLaunchSmokeValidation` still O001-only);
  - the ARIA Watch controls generated from node state;
  - O###-style jobs in `run_aria_queue.py`.
  These are gap-table follow-ups for O002.

## Validation (logs in `D:\Projects\WarlineCapture-program-logs\2026-11-21\win-dev-unity\`)
### EditMode, Operations suites
- **Filter:** OperationsReconCheckpoint, OperationsReconWorld, OperationsIntroductionPause, OperationsScenarioSourceBinding, OperationsDashboardScreen, CampaignOperationsScreen, OperationsMissionRegistry, and `Game.Tests.Editor.Operations.*`.
- **Baseline 8dfd7dfab** (`w38-utf2-base`): 101 run, 94 passed, 7 failed.
- **W38 c1b24e0aa** (`w38-utf2`): 106 run, 99 passed, 7 failed.
- **The 7 failures are identical in both runs:**
  - CampaignOperationsScreenTests ×2;
  - OperationsDashboardScreenTests ×3;
  - OperationsAriaPlayModeCaptureTests.PlayModeCaptureWiringPasses;
  - OperationsP0Tests.ShadowProjectIsIsolatedFromSharedCheckout.
- **New `OperationsMissionRegistryTests`: 5/5 pass.** They cover registry paths and seeds, O001's rules and HUD flags, catalog-mismatch rejection, and the SCAN, INTERACT/carry and EXTRACT priority order.

### O001 set on Windows
Run with the play-mode runner `InvokeUnity.ps1 -GuiLicensing -executeMethod`, no `-quit`, with ARIA runs using `WARLINE_ARIA_BACKGROUND_VALIDATION=1`. A warm retry was allowed once on a startup refund.

| Run | Baseline integration 8dfd7dfab (`o001-base-8dfd7dfab`) | W38 c1b24e0aa (`o001-w38-c1b24e0aa`) |
|---|---|---|
| RunAriaVictory EN 1102 | Refund on the cold SubScene bake, then **Passed** on the warm retry | Same: refund, then **Passed** on the warm retry |
| RunAriaVictory EN 2102 | **Passed** | **Passed** |
| RunAriaVictory EN 3102 | **Passed** | **Passed** |
| RunAriaVictory EN 4102 | **Passed** | **Passed** |
| RunAriaVictoryPersian 9102 | **Passed** | **Passed** |
| RunCheckpointResume | Failed: `Visible button is covered or clipped: RESUME ATTEMPT hit=none` | Failed, same message |
| RunLifecycle | **Passed** (`deploy-withdraw-save-return-redeploy`) | **Passed** |

- **Result: same or better on every row** against the Windows baseline.
- The RunCheckpointResume failure is pre-existing on Windows (the Mac passed it after the O001-finish fix). The lead ruled that it doesn't block W38; it is W44 item 4.
- RunManualVictory needs real mouse and keyboard input from a person, so it wasn't run. It is pending.
- The Persian run rewrote two text-alignment values in `SCN08_MatchHudContent.prefab`, the same writer the Mac lab saw. Restored after each set and not committed.

### Failed attempts kept
- `o001-base-fail-wrapper-quit`: my first baseline used `InvokeUnityExecuteMethodValidation.ps1`, which adds `-quit`, so Unity exited before play mode. A path-quoting bug in my script also made all runs share one log.
- `w38-fail-cs0136`: my CS0136 compile error, plus a log-path bug. Both fixed.

Real player and device acceptance are pending.
