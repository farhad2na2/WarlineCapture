# WarlineCapture Handoff

Lane: Gameplay

Task: Step 17 - inventory final `BuildingPlacementSystem` facade blockers before deletion.

Files changed:
- `Design/Architecture/buildingplacement_retirement_audit.md`

Contracts touched:
- Added a Step 17 blocker inventory with exact production composition blockers, facade surface blockers, config/startup blockers, and editor test blockers.
- Replaced the old eight-item deletion gate with the corrected Step 17-25 sequence.
- Included the two final gates explicitly: remove architecture debt allowances and run the validation gate.

User-visible behavior:
- No gameplay or UI behavior change.
- Documentation-only inventory pass.

Validation run:
- `rg` inventory over `Assets/Game/Scripts` and `Assets/Tests`.
- `git diff --check`

Validation result:
- Production facade references are still isolated to `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` and `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`.
- Editor test blockers remain in the seven files listed in the audit.
- `git diff --check` passed.

Known gaps:
- This step intentionally did not remove the facade.
- `BuildingPlacementSystem` remains at 2051 lines and 120 public/internal declarations.
- Existing uncommitted Step 15/16 code changes are still in the worktree.
- Unrelated dirty files remain: `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` and `Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs`.

Cross-lane impacts:
- Architecture planning is now explicit about the remaining deletion blockers and validation gates.
- No art, scene, UI prefab, or balance data was intentionally changed.

Next recommended task:
- Step 18: extract remaining runtime context factories from the facade, starting with the runtime tick and boundary publish contexts used by `BuildingGameplayCompositionSystem`.
