# M3 source-growth attribution

Base: `b6b8153d241786b8e368a3910f07fb435396805e`. This compares the exact base bytes with the working source for every path reported by architecture audit-02, with audit-03/04 membership recorded in the JSON. It does not waive any guard failure or declare the aggregate passed. Audit-04 repeated all 17 checks: 11 passed and 6 failed. The six M3-changed command, narrative and removal paths no longer appear in the size violations. Remaining failing paths are unchanged from base, except the already-failing UIShellContentView identity described below. The aggregate remains failed.

The existing source-growth manifest, approved exceptions, identity hashes and test thresholds are unchanged. M3 command, narrative-localization and building-removal logic was split into focused partial files of the same owners; the campaign projection's progress hashing lives with its existing catalog/progress lookup partial.

| Path under `Assets/Game/Scripts/` | Base lines / bytes | Current lines / bytes | Attribution |
|---|---:|---:|---|
| `Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs` | 493 / 22128 | 493 / 22128 | Unchanged from base; reported violation predates M3 |
| `Composition/Narrative/FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper.cs` | 99 / 3526 | 70 / 2450 | Changed by M3; current source below base size |
| `Composition/Narrative/FirstLaunchNarrativeSequencePresentationSystemHelper.cs` | 491 / 21331 | 479 / 20959 | Changed by M3; current source below base size |
| `Composition/Narrative/SharedLocalizationTextCompositionSystemHelper.cs` | 50 / 1572 | 50 / 1572 | Unchanged from base; reported violation predates M3 |
| `Systems/BuildingRuntimeEntityCompositionSystemHelper.cs` | 340 / 16636 | 316 / 15803 | Changed by M3; current source below base size |
| `Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs` | 741 / 29338 | 631 / 24838 | Changed by M3; current source below base size |
| `Systems/SelectionUiReadModelLookup.cs` | 826 / 33960 | 697 / 28761 | Changed by M3; current source below base size |
| `UI/Components/BattleHudRuntimeFeedbackView.cs` | 523 / 20716 | 523 / 20716 | Unchanged from base; reported violation predates M3 |
| `UI/Components/MatchHudSelectionPanelView.cs` | 852 / 37351 | 852 / 37351 | Unchanged from base; reported violation predates M3 |
| `UI/Components/MatchHudSquadTrayView.cs` | 581 / 24486 | 581 / 24486 | Unchanged from base; reported violation predates M3 |
| `UI/Components/MatchHudTransportPassengerDrawerView.cs` | 585 / 25332 | 585 / 25332 | Unchanged from base; reported violation predates M3 |
| `UI/MenuDiagnosticsUiSystemHelper.cs` | 295 / 10315 | 295 / 10315 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/AriaCommandAssistantPopupView.cs` | 559 / 24424 | 559 / 24424 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/BuildDrawerCatalogRuntimeView.cs` | 430 / 20510 | 430 / 20510 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/BuildPlacementConfirmationBarView.cs` | 507 / 20663 | 507 / 20663 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/MatchHudMinimapInputUiSystemHelper.cs` | 1308 / 53991 | 1308 / 53991 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs` | 538 / 23988 | 436 / 18704 | Changed by M3; current source below base size |
| `UI/Screens/ResourceExchangePopupRuntimeView.cs` | 211 / 7555 | 211 / 7555 | Unchanged from base; reported violation predates M3 |
| `UI/Screens/ResourceExchangePopupView.cs` | 423 / 17145 | 423 / 17145 | Unchanged from base; reported violation predates M3 |
| `UI/Shell/ResourceExchangeShellBinding.cs` | 104 / 3705 | 104 / 3705 | Unchanged from base; reported violation predates M3 |
| `UI/Shell/UIShellContentView.cs` | 985 / 43208 | 872 / 38638 | Changed by M3; current source below base size |

`UIShellContentView.cs` is a special case: its expected post-hardening identity already differs from base HEAD. The M3 changes further change its identity while reducing its size and keeping binding/cleanup responsibilities in partial files of the existing owner. The guard remains failed; size reduction is not permission to bypass an identity guard.

Exact source SHA-256 values are in [architecture_attribution.json](architecture_attribution.json). This attribution preserves the distinction between a passing M3 behavior test, the absence of new size growth, and an all-green repository architecture audit.
