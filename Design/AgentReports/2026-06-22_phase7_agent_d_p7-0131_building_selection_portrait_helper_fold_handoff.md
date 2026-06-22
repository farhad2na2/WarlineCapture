# Phase 7 Agent D Handoff - P7-0131 BuildingSelectionPortraitSystem Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0131` - `BuildingSelectionPortraitSystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingSelectionPortraitSystem` was a disabled `SystemBase` wrapper with only a static selection portrait resolver.
- New: `BuildingSelectionPortraitSystem` is a static selection portrait helper. Existing `BuildingSelectionCompositionSystem` wiring still calls the same `Resolve` method.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingSelectionPortraitSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/AgentReports/2026-06-22_phase7_agent_d_p7-0131_building_selection_portrait_helper_fold_handoff.md`

Behavior preserved:
- `Resolve(RuntimeBuildingEntity building, Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab)` still returns null for null runtime buildings.
- It still tries the building definition prefab portrait first.
- It still falls back to the runtime building instance portrait.

Counts:
- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded helpers: `1`

Inventory impact:
- Total ECS system declarations: `318`
- Production `SystemBase`/legacy declarations: `185`
- Production `ISystem` declarations: `133`
- Production non-UI rows: `311`
- Agent D rows: `49`
- SplitThenConvert rows: `108`
- Open rows: `163`

Validation:
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, 0 warnings, 0 errors
- Unity composition smoke command with log `/private/tmp/warline-phase7-agent-d-selection-portrait-helper-fold-smoke.log`: passed, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`
- Unity runtime building validation command with log `/private/tmp/warline-phase7-agent-d-selection-portrait-helper-fold-runtime-building.log`: passed, marker `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`
- Architecture guard command with log `/private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- `git diff --check`: passed

Risks:
- No runtime behavior change is intended. This slice removes only fake ECS inheritance from a static selection portrait helper.
- The helper still resolves Unity `Sprite` values by design because this is a presentation/HUD selection boundary, not hot ECS gameplay.

Next guidance:
- Continue Agent D with the next low-risk helper if one remains; otherwise start a documented split-before-convert slice for a broader building placement or building selection owner.
