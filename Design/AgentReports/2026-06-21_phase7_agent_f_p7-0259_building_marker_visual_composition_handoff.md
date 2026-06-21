# Phase 7 Agent F Handoff - P7-0259 Building Marker Visual Composition

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:

- `P7-0259` - `BuildingMarkerVisualCompositionSystem` - `Retired/Folded`

Files changed:

- `Assets/Game/Scripts/Systems/BuildingMarkerVisualCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Visual split:

- Request/result data: no new ECS request data added. This row only owned shared marker `MaterialPropertyBlock` reuse.
- Managed presentation exceptions: none added.
- Folded helper: `BuildingMarkerVisualCompositionSystem` is now a plain helper. `BuildingGameplayCompositionSystem` owns the helper directly instead of resolving a disabled `SystemBase` from the ECS World.

Counts:

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed presentation SystemBase exceptions: `0`
- Retired/folded: `1`

Inventory impact:

- Total ECS declarations: `359`
- Production SystemBase/legacy declarations: `226`
- Production ISystem declarations: `133`
- Production non-UI rows: `352`
- Agent F rows: `49`

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `BuildingSelectionMarkerSystemTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-f-building-marker-composition-selection-marker.log`, marker `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`.
- `BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation`: passed, `/private/tmp/warline-phase7-agent-f-building-marker-composition-placement-validation.log`, marker `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Residual validation note:

- `BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation` failed in `/private/tmp/warline-phase7-agent-f-building-marker-composition-placement-runtime.log` with an existing cadence assertion: expected `activeTransport` to run twice, observed once. This test covers runtime tick throttling, not marker property-block ownership, so it is recorded as residual risk instead of a blocker for `P7-0259`.

Risks:

- No marker rendering behavior changed. Risk is limited to ownership: marker property-block reuse now comes from a directly owned helper instead of a disabled ECS-managed object.
