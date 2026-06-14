#!/usr/bin/env python3
from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GAME_SCRIPTS_ROOT = ROOT / "Assets/Game/Scripts"
OUTPUT_PATH = ROOT / "Design/Architecture/non_ecs_to_ecs_system_inventory.md"

TYPE_DECLARATION_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly)\s+)*"
    r"(?P<kind>class|struct)\s+(?P<name>[A-Za-z_]\w*)\s*(?P<bases>:[^{;\r\n]+)?",
    re.MULTILINE,
)
ECS_BASE_RE = re.compile(r"\b(ISystem|SystemBase|ComponentSystemBase|ComponentSystem|JobComponentSystem)\b")
MONO_BEHAVIOUR_RE = re.compile(r"\b(MonoBehaviour|UnityEngine\.MonoBehaviour)\b")

FIRST_WAVE = {
    "SelectionMoveCommandRequestSystem",
    "SelectedMoveOrderCommandSystem",
    "SelectionAttackCommandRequestSystem",
    "AttackOrderCommandSystem",
    "SelectionScanCommandRequestSystem",
    "ScanIntelCommandSystem",
    "SelectionTransportCommandRequestSystem",
    "TransportBoardingCommandSystem",
    "UnitTransportRopeDisembarkCommandSystem",
    "BuildingTargetMoveOrderSystem",
    "CitizenMovementCommandSystem",
}

MANAGED_BOUNDARY_NAMES = {
    "RtsSelectionPointerTargetCommandSystem",
    "RtsSelectionFocusCommandSystem",
    "RtsSelectionCommandResultFlushSystem",
    "SelectionHudFeedbackSystem",
    "SelectionOrderMarkerSystem",
    "RtsCameraRequestSystem",
    "RtsCameraSystem",
    "RuntimeCameraReferenceSystem",
    "BuildingUiCommandSystem",
    "BuildingPlacementCommandSystem",
    "RoadBuildCommandSystem",
    "BuildingRuntimeSpawnCommandSystem",
}

FOCUSED_SPLIT_NAMES = {
    "FocusedUnitCommandSystem",
    "UnitMoveOrderSystem",
    "UnitTargetOrderSystem",
    "BuildingProductionRequestSystem",
}

FOLD_NAME_TOKENS = (
    "Query",
    "Rule",
    "Cell",
    "Footprint",
    "Validation",
    "Context",
    "Source",
    "Projection",
    "Resolution",
)

MANAGED_NAME_TOKENS = (
    "Visual",
    "Camera",
    "Prefab",
    "Bootstrap",
    "Startup",
    "Spawn",
    "Runtime",
    "Marker",
    "Preview",
    "Feedback",
    "Diagnostic",
    "Composition",
    "Lifecycle",
    "Config",
    "ReadModel",
)

MANAGED_SELECTION_BOUNDARY_NAMES = {
    "FocusableUnitLookupSystem",
    "MatchHudSquadTraySelectionSystem",
    "MatchStartRequestSystem",
    "RtsSelectionInputStateSystem",
    "RtsSelectionInputSystem",
    "SelectedUnitOrderSnapshotSystem",
    "SelectionBuildingInteractionSystem",
    "SelectionRectangleRequestSystem",
    "SelectionStateSystem",
    "SelectionUiCommandSystem",
    "VisibleUnitSelectionSystem",
}

TRANSPORT_STATE_NAMES = {
    "UnitTransportAirPickupSystem",
    "UnitTransportCapacitySystem",
    "UnitTransportPassengerStateSystem",
}


@dataclass(frozen=True)
class Declaration:
    path: str
    name: str
    kind: str
    bases: str


@dataclass(frozen=True)
class InventoryEntry:
    declaration: Declaration
    disposition: str
    phase: str
    reason: str


def normalize(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def declarations() -> list[Declaration]:
    result: list[Declaration] = []
    for path in sorted(GAME_SCRIPTS_ROOT.rglob("*.cs")):
        text = path.read_text(encoding="utf-8")
        for match in TYPE_DECLARATION_RE.finditer(text):
            name = match.group("name")
            if not name.endswith("System"):
                continue
            result.append(
                Declaration(
                    path=normalize(path),
                    name=name,
                    kind=match.group("kind"),
                    bases=(match.group("bases") or "").lstrip(":").strip(),
                )
            )
    return result


def is_unity_ecs(declaration: Declaration) -> bool:
    return bool(ECS_BASE_RE.search(declaration.bases))


def is_mono_behaviour(declaration: Declaration) -> bool:
    return bool(MONO_BEHAVIOUR_RE.search(declaration.bases))


def is_editor_only(declaration: Declaration) -> bool:
    return "/Editor/" in declaration.path


def classify(declaration: Declaration) -> InventoryEntry:
    name = declaration.name
    path = declaration.path

    if name == "FactionIdentitySystem":
        return entry(declaration, "FoldIntoOwner", "Phase 9", "Static faction identity helper with no update lifetime; fold or rename into faction identity data/utilities.")
    if path.startswith("Assets/Game/Scripts/Rendering/Systems/UnitRenderBudget"):
        return entry(declaration, "FoldIntoOwner", "Phase 9/11", "Render-budget helper struct; fold into `UnitRenderBudgetSystem` owner or private helper jobs.")
    if path.startswith("Assets/Game/Scripts/Rendering/") and "Renderer" in declaration.bases:
        return entry(declaration, "ConvertToSystemBase", "Phase 11", "Managed rendering boundary using camera, materials, meshes, or Graphics APIs.")
    if name == "UnitHierarchicalPathSystem" or name.startswith("UnitPath"):
        return entry(declaration, "FoldIntoOwner", "Phase 6/9", "Pathfinding helper owned by the ECS pathfinding pipeline; fold into `UnitPathfindingSystem` or private jobs.")
    if name.startswith("MapSurface"):
        return entry(declaration, "FoldIntoOwner", "Phase 3/9", "Map-surface helper owned by pointer target resolution or pathfinding; fold into the responsible ECS/boundary owner.")
    if name in TRANSPORT_STATE_NAMES:
        return entry(declaration, "ConvertToISystem", "Phase 2/6", "Transport gameplay state mutation should become ECS request/state processing.")
    if name in MANAGED_SELECTION_BOUNDARY_NAMES:
        return entry(declaration, "ConvertToSystemBase", "Phase 3/4/8", "Selection/input helper mixes ECS state with camera/UI or managed caches; split gameplay data into ECS and keep managed edge explicit.")
    if name.startswith("Building") or name.startswith("Road"):
        return entry(declaration, "ConvertToSystemBase", "Phase 7", "Building/road flow currently mixes gameplay requests with managed placement, prefab, or session state; split ECS command data from managed boundary.")
    if name.startswith("Citizen") or name in {"FactionResourceSystem", "ResourceHaulerSystem"}:
        return entry(declaration, "ConvertToSystemBase", "Phase 10/11", "Managed city/population/resource model; split ECS simulation data from managed runtime boundary.")
    if name.startswith("Initial"):
        return entry(declaration, "ConvertToSystemBase", "Phase 10/11", "Initial spawn/bootstrap helper; move data projection into ECS initialization and keep managed scene edge thin.")
    if name in FIRST_WAVE:
        return entry(declaration, "ConvertToISystem", "Phase 2", "Known first-wave command/request processor with ECS request/result conversion path.")
    if name in FOCUSED_SPLIT_NAMES:
        return entry(declaration, "ConvertToISystem", "Phase 5/6/7", "Gameplay mutation helper must be split into ECS request processors and folded helper math.")
    if name in MANAGED_BOUNDARY_NAMES:
        return entry(declaration, "ConvertToSystemBase", "Phase 3/4/8", "Mixed UI/pointer/camera/presentation boundary; move gameplay to ECS first, keep managed Unity edge explicit.")
    if "/UI/" in path:
        return entry(declaration, "PassiveBoundary", "Phase 8", "UI runtime boundary or view helper; should display/request ECS data, not own gameplay policy.")
    if "/Composition/" in path:
        return entry(declaration, "PassiveBoundary", "Phase 10", "Scene composition boundary; extract gameplay policy into ECS systems, keep wiring thin.")
    if "/Environment/" in path:
        return entry(declaration, "ConvertToSystemBase", "Phase 11", "Runtime environment/city generation boundary; split ECS planning from managed prefab/coroutine spawning.")
    if any(token in name for token in FOLD_NAME_TOKENS):
        return entry(declaration, "FoldIntoOwner", "Phase 9", "Pure/read/helper-style runtime system; fold into owning ECS system/job unless it proves to need update lifetime.")
    if any(token in name for token in MANAGED_NAME_TOKENS):
        return entry(declaration, "ConvertToSystemBase", "Phase 8/10/11", "Managed visual/runtime/bootstrap/presentation responsibility; split ECS data from Unity object boundary.")

    return entry(declaration, "ReviewRequired", "Phase 0", "No safe automatic classification; inspect before implementation.")


def entry(declaration: Declaration, disposition: str, phase: str, reason: str) -> InventoryEntry:
    return InventoryEntry(declaration, disposition, phase, reason)


def format_table(entries: list[InventoryEntry]) -> str:
    lines = [
        "| File | Type | Current Base | Disposition | Owner Phase | Reason |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for inventory_entry in entries:
        declaration = inventory_entry.declaration
        base_type = declaration.bases or "(none)"
        lines.append(
            f"| `{declaration.path}` | `{declaration.name}` | `{base_type}` | `{inventory_entry.disposition}` | "
            f"{inventory_entry.phase} | {inventory_entry.reason} |"
        )
    return "\n".join(lines)


def write_inventory() -> None:
    all_declarations = declarations()
    unity_ecs = [declaration for declaration in all_declarations if is_unity_ecs(declaration)]
    mono_behaviours = [declaration for declaration in all_declarations if is_mono_behaviour(declaration)]
    editor_only = [declaration for declaration in all_declarations if is_editor_only(declaration)]
    denominator = [
        declaration
        for declaration in all_declarations
        if not is_unity_ecs(declaration)
        and not is_mono_behaviour(declaration)
        and not is_editor_only(declaration)
    ]
    inventory = [classify(declaration) for declaration in denominator]
    counts: dict[str, int] = {}
    for inventory_entry in inventory:
        counts[inventory_entry.disposition] = counts.get(inventory_entry.disposition, 0) + 1

    output = [
        "# Non-ECS System Conversion Inventory",
        "",
        "Generated by `Tools/Architecture/generate_non_ecs_system_inventory.py`.",
        "",
        "## Summary",
        "",
        f"- Total runtime `*System` declarations under `Assets/Game/Scripts`: `{len(all_declarations)}`.",
        f"- Unity ECS systems excluded: `{len(unity_ecs)}`.",
        f"- MonoBehaviour systems excluded: `{len(mono_behaviours)}`.",
        f"- Editor-only systems excluded: `{len(editor_only)}`.",
        f"- Runtime non-ECS conversion denominator: `{len(denominator)}`.",
        f"- Convert to `ISystem`: `{counts.get('ConvertToISystem', 0)}`.",
        f"- Convert to `SystemBase`: `{counts.get('ConvertToSystemBase', 0)}`.",
        f"- Fold into owner/job: `{counts.get('FoldIntoOwner', 0)}`.",
        f"- Passive/editor/UI/composition boundary: `{counts.get('PassiveBoundary', 0)}`.",
        f"- Review required: `{counts.get('ReviewRequired', 0)}`.",
        "",
        "## Editor-Only Exclusions",
        "",
        format_declarations(editor_only),
        "",
        "## Runtime Non-ECS Inventory",
        "",
        format_table(inventory),
        "",
    ]
    OUTPUT_PATH.write_text("\n".join(output), encoding="utf-8")


def format_declarations(items: list[Declaration]) -> str:
    if not items:
        return "(none)"
    return "\n".join(f"- `{item.path}`: `{item.name}`" for item in sorted(items, key=lambda item: (item.path, item.name)))


if __name__ == "__main__":
    write_inventory()
