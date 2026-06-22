# Phase 7 Agent E Handoff - Runtime City Core Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0146` - `RuntimeCityBuildingPlotSystem` - Retired/folded from disabled `SystemBase` wrapper into a plain runtime-city plot algorithm helper.
- `P7-0166` - `RuntimeCityLayoutSystem` - Retired/folded from disabled `SystemBase` wrapper into a plain runtime-city layout algorithm helper.
- `P7-0180` - `RuntimeCityWalkabilitySystem` - Retired/folded from disabled `SystemBase` wrapper into a plain runtime-city walkability helper.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityLayoutSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityWalkabilitySystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result ECS contracts changed.
- Existing nested data contracts stayed on the same type names:
  - `RuntimeCityBuildingPlotSystem.PlotCandidate`
  - `RuntimeCityLayoutSystem.CityChainAxis`
  - `RuntimeCityLayoutSystem.CityLayoutData`
  - `RuntimeCityWalkabilitySystem.ReservedFootprint`
- `RuntimeCityCompositionSystem` now creates these helper classes directly instead of resolving disabled managed ECS systems from the default world.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `3`
- Inventory after regeneration: `263 total` ECS declarations, `130` production SystemBase/legacy declarations, `133` production ISystem declarations, `50.6%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-core-helper-fold-city.log -quit`: passed, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCityLayoutSystem.cs Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotSystem.cs Assets/Game/Scripts/Environment/RuntimeCityWalkabilitySystem.cs Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs Design/Architecture/systembase_to_isystem_inventory.md`: passed.

Risks:
- These helpers intentionally keep their historical `*System` type names so existing aliases and nested type references remain stable. They no longer inherit `SystemBase`, have no ECS lifecycle, and are manually owned by `RuntimeCityCompositionSystem`.
- Unity batchmode logged a CDN timeout after the runtime-city validation marker during shutdown; the focused validation passed before that network/shutdown noise.
