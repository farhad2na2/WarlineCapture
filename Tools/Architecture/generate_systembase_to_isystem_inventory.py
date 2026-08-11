#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from dataclasses import asdict, dataclass, replace
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ROOT = ROOT / "Assets/Game/Scripts"
DEFAULT_OUTPUT = ROOT / "Design/Architecture/systembase_to_isystem_inventory.md"

TYPE_DECLARATION_RE = re.compile(
    r"^[ \t]*(?:(?:\[[^\]\r\n]*(?:\r?\n[ \t]*\[[^\]\r\n]*)*\][ \t]*)\r?\n[ \t]*)*"
    r"(?P<modifiers>(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe)\s+)*)"
    r"(?P<kind>class|struct)\s+"
    r"(?P<name>[A-Za-z_]\w*)"
    r"(?:\s*<[^>{};\r\n]+>)?"
    r"\s*(?P<bases>:[^{;]+)?",
    re.MULTILINE,
)

NAMESPACE_RE = re.compile(
    r"^[ \t]*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*(?:[;{])",
    re.MULTILINE,
)

SYSTEM_BASE_TOKENS = (
    "SystemBase",
    "ComponentSystemBase",
    "ComponentSystem",
    "JobComponentSystem",
)

ECS_TOKENS = SYSTEM_BASE_TOKENS + ("ISystem",)

LIFECYCLE_METHODS = (
    "OnCreate",
    "OnStartRunning",
    "OnUpdate",
    "OnStopRunning",
    "OnDestroy",
    "Update",
    "LateUpdate",
    "FixedUpdate",
)

COROUTINE_RE = re.compile(r"\bIEnumerator\s+(?P<name>[A-Za-z_]\w*)\s*\(", re.MULTILINE)
PUBLIC_MEMBER_RE = re.compile(
    r"^[ \t]*(?:public|internal)\s+(?:static\s+)?(?:readonly\s+)?(?:override\s+)?"
    r"(?P<type>[A-Za-z0-9_<>,\.\?\[\]\s]+?)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*(?P<shape>\(|\{)",
    re.MULTILINE,
)
ATTRIBUTE_RE = re.compile(r"\[(?P<name>UpdateInGroup|UpdateBefore|UpdateAfter|DisableAutoCreation)[^\]]*\]")

UNMANAGED_ECS_OBJECT_REFERENCE_RE = re.compile(r"\bUnityObjectRef\s*<\s*GameObject\s*>")
MANAGED_TRANSFORM_RE = re.compile(
    r"\bUnityEngine\.Transform\b|"
    r"(?<![A-Za-z0-9_.])Transform\s*(?:\[\s*\])?\s+[A-Za-z_]\w*|"
    r"(?:<|,)\s*Transform\s*(?=[>,])|"
    r"\b(?:typeof|nameof)\s*\(\s*Transform\s*\)|"
    r"\(\s*Transform\s*\)|"
    r"\b(?:is|as)\s+Transform\b"
)

MANAGED_BLOCKER_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("GameObject", re.compile(r"\bGameObject\b")),
    ("Transform", MANAGED_TRANSFORM_RE),
    ("Camera", re.compile(r"\bCamera\b")),
    ("UnityEngine.Object", re.compile(r"\bUnityEngine\.Object\b")),
    ("ScriptableObject", re.compile(r"\bScriptableObject\b")),
    ("Resources", re.compile(r"\bResources\s*\.")),
    ("Object.Instantiate", re.compile(r"\b(?:Object|UnityEngine\.Object)\.Instantiate\s*\(")),
    ("Object.Destroy", re.compile(r"\b(?:Object|UnityEngine\.Object)\.Destroy\s*\(")),
    ("Find*", re.compile(r"\b(?:GameObject|Object|UnityEngine\.Object)\.Find[A-Za-z0-9_]*\s*\(|\bFindObject[A-Za-z0-9_]*\s*\(")),
    ("Camera.main", re.compile(r"\bCamera\.main\b")),
    ("Material", re.compile(r"\bMaterial\b")),
    ("Renderer", re.compile(r"\bRenderer\b")),
    ("Light", re.compile(r"\bLight\b")),
    ("ParticleSystem", re.compile(r"\bParticleSystem\b")),
    ("LineRenderer", re.compile(r"\bLineRenderer\b")),
    ("VisualEffect", re.compile(r"\bVisualEffect\b")),
    ("MonoBehaviour", re.compile(r"\bMonoBehaviour\b")),
    ("Coroutine", re.compile(r"\bCoroutine\b|\bStartCoroutine\s*\(|\bStopCoroutine\s*\(")),
    ("List<GameObject>", re.compile(r"\bList\s*<\s*GameObject\s*>")),
    ("Dictionary<..., GameObject>", re.compile(r"\bDictionary\s*<[^>\r\n]*GameObject")),
)

ECS_ACCESS_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("Entities.ForEach", re.compile(r"\bEntities\.ForEach\b")),
    ("SystemAPI.Query", re.compile(r"\bSystemAPI\.Query\b")),
    ("EntityQuery", re.compile(r"\bEntityQuery\b")),
    ("EntityManager", re.compile(r"\bEntityManager\b|\bstate\.EntityManager\b")),
    ("GetComponentLookup", re.compile(r"\bGetComponentLookup\s*<")),
    ("GetBufferLookup", re.compile(r"\bGetBufferLookup\s*<")),
    ("ToEntityArray", re.compile(r"\bToEntityArray\s*(?:<[^>]+>)?\s*\(")),
    ("ToComponentDataArray", re.compile(r"\bToComponentDataArray\s*(?:<[^>]+>)?\s*\(")),
    ("ECB", re.compile(r"\bEntityCommandBuffer\b")),
    ("jobs", re.compile(r"\bIJob(?:Entity|Chunk|ParallelFor|For)?\b|\bJobHandle\b")),
    (".Run", re.compile(r"\.Run\s*\(")),
    (".Schedule", re.compile(r"\.Schedule\s*\(")),
    (".ScheduleParallel", re.compile(r"\.ScheduleParallel\s*\(")),
)

GAMEPLAY_POLICY_TOKENS = (
    "Damage",
    "Health",
    "Attack",
    "Combat",
    "Path",
    "MoveOrder",
    "Selection",
    "BuildingPlacement",
    "Production",
    "Economy",
    "Resource",
    "Spawn",
    "Validate",
    "Command",
)

AGENT_TRACKER_FILES = {
    "AgentB": "Design/Architecture/phase7_agent_b_direct_startup_tracker.md",
    "AgentC": "Design/Architecture/phase7_agent_c_selection_commands_tracker.md",
    "AgentD": "Design/Architecture/phase7_agent_d_building_production_tracker.md",
    "AgentE": "Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md",
    "AgentF": "Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md",
    "Integration": "Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md",
}

VALIDATION_BY_OWNER = {
    "AgentB": "Architecture guard + compile + startup/diagnostics focused validation",
    "AgentC": "Architecture guard + compile + selection/command focused validation",
    "AgentD": "Architecture guard + compile + building focused validation",
    "AgentE": "Architecture guard + compile + road/city/citizen focused validation",
    "AgentF": "Architecture guard + compile + rendering/VFX focused validation",
    "Integration": "Architecture guard + compile",
}

MANUAL_REVIEW_OVERRIDES: dict[tuple[str, str, str], tuple[str, str, str]] = {
    ("Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs", "PerformanceDiagnosticsSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: diagnostics timing facade with public helper API. Fold into plain diagnostics helper/state or convert to narrow ECS diagnostics data without gameplay ownership.",
        "Folded diagnostics helper plus ECS diagnostics data where needed",
    ),
    ("Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs", "RuntimeGameplayStateSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: data-only runtime state mirror. Convert to narrow `ISystem`/component-backed request-result state while preserving managed composition read API through ECS data.",
        "`RuntimeGameplayStateSystem : ISystem`",
    ),
    ("Assets/Game/Scripts/Systems/FocusedUnitLifecycleSystem.cs", "FocusedUnitLifecycleSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: selected/focused lifecycle policy plus clicked-unit delegates. Split ECS focus mutation from managed click/description callbacks.",
        "Selection lifecycle `ISystem` processors plus passive click/read callbacks",
    ),
    ("Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushSystem.cs", "RtsSelectionCommandResultFlushSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: broad command-result orchestration. Split each command family into request/result `ISystem` processors and keep HUD callbacks as passive presentation hooks.",
        "Narrow command-result `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/RtsSelectionInputSystem.cs", "RtsSelectionInputSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: pointer/UI input shell plus command queueing. Keep input sampling passive and move command intent/state mutation to ECS request processors.",
        "Input boundary plus command intent `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/SelectionStateSystem.cs", "SelectionStateSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: selected/focused state cache with public helper surface. Replace cache helpers with ECS selected-state queries and request/result data.",
        "Focused/selected state `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/SelectionUiCommandSystem.cs", "SelectionUiCommandSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: UI command facade. Fold into UI request enqueueing and ECS command request buffers rather than preserving a runtime system owner.",
        "UI request enqueue helpers plus ECS command buffers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs", "BuildingBarrierSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: barrier/gate policy, breach targeting, and visual-door callbacks are mixed. Split grid/breach decisions from door presentation.",
        "Barrier/breach `ISystem` processors plus passive door presentation",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementAdapterSystem.cs", "BuildingPlacementAdapterSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: adapter/delegate surface only. Fold call routing into concrete placement systems or plain helpers.",
        "Folded placement adapter helpers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementCommandCompositionSystem.cs", "BuildingPlacementCommandCompositionSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: command context composer. Replace with explicit request/result data and construction helpers owned by placement command processors.",
        "Folded placement command context helpers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs", "BuildingPlacementInputSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: placement pointer/session state. Split input/session data processing from UI pointer boundary.",
        "Placement input/session `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs", "BuildingPlacementRedirectSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: runtime side effects, unit redirection, and marker refresh are mixed. Split redirect decisions from visual refresh callbacks.",
        "Placement redirect `ISystem` plus marker-refresh request data",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs", "BuildingPlacementValidationSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: pure placement validation/grid checks. Convert to `ISystem` or static Burst-safe validation helpers without managed presentation.",
        "`BuildingPlacementValidationSystem : ISystem` or static validation helpers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs", "BuildingResourceHaulerBridgeSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: resource-hauler bridge mixes building queries and move-order assignment. Split hauler order decisions into ECS request processors.",
        "Resource hauler order `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/BuildingRuntimeCompositionSystem.cs", "BuildingRuntimeCompositionSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: runtime context composer. Fold into explicit building runtime data/query helpers and remove recurring system ownership.",
        "Folded runtime composition helpers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs", "BuildingSpawnSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: production spawn, reservation cleanup, and combat spawn queries are mixed. Split spawn requests/reservations into ECS processors.",
        "Building spawn request/reservation `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotSystem.cs", "RuntimeCityBuildingPlotSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: city plot algorithm/state with no Unity-object blocker. Convert to focused `ISystem` or Burst-safe city plot helpers.",
        "`RuntimeCityBuildingPlotSystem : ISystem`",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs", "RuntimeCityDiagnosticSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: diagnostic log helper. Fold into diagnostics request/log data or a passive diagnostics helper.",
        "Folded runtime-city diagnostics helper",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityLayoutSystem.cs", "RuntimeCityLayoutSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: deterministic city layout algorithm/state. Convert to focused `ISystem` or Burst-safe layout helper.",
        "`RuntimeCityLayoutSystem : ISystem`",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs", "RuntimeCityLifecycleSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: lifecycle gate/yield orchestration. Split state transitions into ECS data and keep only external orchestration passive.",
        "Runtime city lifecycle `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityRoadBuildBridgeSystem.cs", "RuntimeCityRoadBuildBridgeSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: city-road bridge sync and road-chain creation. Split road sync requests from bridge/composition helpers.",
        "Runtime city road-build request `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityWalkabilitySystem.cs", "RuntimeCityWalkabilitySystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: walkability reservation/grid logic with no Unity-object blocker. Convert to focused ECS data processor or static Burst-safe helper.",
        "`RuntimeCityWalkabilitySystem : ISystem`",
    ),
    ("Assets/Game/Scripts/Systems/CitizenBuildingReadSystem.cs", "CitizenBuildingReadSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: citizen building read model bridges runtime building lists. Split building snapshots from citizen queries.",
        "Citizen building snapshot/read `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/CitizenHouseholdRegistrationSystem.cs", "CitizenHouseholdRegistrationSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: household registration and displacement are stateful domain logic. Split into focused citizen/household ECS processors.",
        "Citizen household registration `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/CitizenPopulationDiagnosticSystem.cs", "CitizenPopulationDiagnosticSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: citizen diagnostics/timing accumulator. Fold into diagnostics data or passive logging helper.",
        "Folded citizen diagnostics helper",
    ),
    ("Assets/Game/Scripts/Systems/CitizenPopulationEcsProjectionSystem.cs", "CitizenPopulationEcsProjectionSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: managed population storage projection into ECS. Split projection writes into ECS processors and retire managed storage ownership where possible.",
        "Citizen population projection `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/CitizenPopulationStateSystem.cs", "CitizenPopulationStateSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: population id/state store with no Unity-object blocker. Convert to ECS state data or focused `ISystem` owner.",
        "`CitizenPopulationStateSystem : ISystem` or ECS state data",
    ),
    ("Assets/Game/Scripts/Systems/CitizenRefugeeSystem.cs", "CitizenRefugeeSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: refugee assignment/upkeep spans citizens, resources, and destroyed homes. Split domain policy into focused ECS processors.",
        "Citizen refugee `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/CitizenStatusTransitionSystem.cs", "CitizenStatusTransitionSystem", "SystemBase"): (
        "DirectConvert",
        "Manual review: pure citizen status transition policy. Convert to focused `ISystem` or static Burst-safe transition helper.",
        "`CitizenStatusTransitionSystem : ISystem`",
    ),
    ("Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs", "RoadBuildCommandSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: command facade plus request processing. Split command enqueue/result data from road-build state mutation.",
        "Road-build command request/result `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs", "RoadBuildSessionSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: road/build session state and UI prompt data are mixed. Split session data mutations from UI prompt presentation.",
        "Road-build session `ISystem` processors plus UI read data",
    ),
    ("Assets/Game/Scripts/Systems/RoadNetworkSystem.cs", "RoadNetworkSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: broad road graph state store and mutation API. Split road graph data, stroke commands, and snapshot read models into focused processors.",
        "Road network data/request `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/RoadRuntimeGenerationSystem.cs", "RoadRuntimeGenerationSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: runtime road generation bridge. Split road creation requests from deferred ECS sync and diagnostics.",
        "Runtime road generation `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs", "UnitModelSpawnSystem", "ISystem"): (
        "Converted",
        "Keep as `ISystem`; camera visibility now comes from `RuntimeCameraSnapshotComponent` published by the managed camera boundary.",
        "`UnitModelSpawnSystem : ISystem` with camera snapshot boundary data",
    ),
    ("Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs", "UnitRenderBudgetSystem", "ISystem"): (
        "Converted",
        "Keep as `ISystem`; render budget math now consumes `RuntimeCameraSnapshotComponent` instead of managed `Camera`.",
        "`UnitRenderBudgetSystem : ISystem` with camera snapshot boundary data",
    ),
    ("Assets/Game/Scripts/Rendering/Systems/UnitAttachedLightSystem.cs", "UnitAttachedLightSystem", "SystemBase"): (
        "ManagedPresentationSystemBaseException",
        "Manual review: consumes ECS attached-light setup and cleanup data, but must tick Unity `Light` GameObjects and managed instance ownership.",
        "Counted managed light presentation `SystemBase` exception consuming ECS attached-light buffer data",
    ),
    ("Assets/Game/Scripts/Rendering/Systems/UnitSelectionMarkerSystem.cs", "UnitSelectionMarkerSystem", "ISystem"): (
        "Converted",
        "Keep as `ISystem`; marker instance and scale decisions stay ECS-only while object-outline materials/meshes are split to `UnitSelectionObjectOutlinePresentationSystem`.",
        "`UnitSelectionMarkerSystem : ISystem` with managed object-outline presentation boundary",
    ),
    ("Assets/Game/Scripts/Rendering/Systems/UnitSelectionObjectOutlinePresentationSystem.cs", "UnitSelectionObjectOutlinePresentationSystem", "SystemBase"): (
        "ManagedPresentationSystemBaseException",
        "Manual review: consumes selected unit marker ECS state and owns only selection object-outline `Material`, `Mesh`, and render-mesh presentation setup.",
        "Counted managed selection object-outline presentation `SystemBase` exception",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementVisualCompositionSystem.cs", "BuildingPlacementVisualCompositionSystem", "SystemBase"): (
        "RetireFold",
        "Manual review: visual composition/delegate owner. Fold into explicit placement visual request data and passive visual update boundary.",
        "Folded placement visual composition helpers",
    ),
    ("Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateSystem.cs", "BuildingPlacementVisualUpdateSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: placement visual update and validation callbacks are mixed. Split visual request data from placement validation/commit policy.",
        "Placement visual request `ISystem` plus passive visual boundary",
    ),
    ("Assets/Game/Scripts/Systems/FactionResourceSystem.cs", "FactionResourceSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: economy/resource state plus public read model. Split production/capacity mutation from UI/economy snapshots.",
        "Faction resource economy `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/ResourceHaulerSystem.cs", "ResourceHaulerSystem", "SystemBase"): (
        "SplitThenConvert",
        "Manual review: hauler phase/resource transfer policy. Split order/state transitions into ECS processors and keep helper math static.",
        "Resource hauler `ISystem` processors",
    ),
    ("Assets/Game/Scripts/Systems/UnitAttackSystem.cs", "UnitAttackSystem", "ISystem"): (
        "Converted",
        "Keep as `ISystem`; authored GameObject VFX playback is split into `UnitAttackVfxSystems.cs` managed presentation boundaries.",
        "`UnitAttackSystem : ISystem` with ECS VFX request data",
    ),
    ("Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs", "CombatGameObjectVfxPlaybackSystem", "SystemBase"): (
        "ManagedPresentationSystemBaseException",
        "Manual review: consumes ECS `CombatGameObjectVfxRequest` entities and unwraps `UnityObjectRef<GameObject>` only to play authored pooled GameObject VFX.",
        "Counted managed presentation `SystemBase` exception consuming ECS combat VFX requests",
    ),
    ("Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs", "UnitAttackVfxRequestSystem", "SystemBase"): (
        "ManagedPresentationSystemBaseException",
        "Manual review: consumes ECS `UnitAttackVfxRequest` entities and unwraps authored muzzle/impact GameObject prefab refs only at the VFX playback boundary.",
        "Counted managed presentation `SystemBase` exception consuming ECS unit attack VFX requests",
    ),
    ("Assets/Game/Scripts/Environment/RuntimeCityRAndDMapSystem.cs", "RuntimeCityRAndDMapSystem", "SystemBase"): (
        "ManagedPresentationSystemBaseException",
        "Manual review: retained managed R&D presentation boundary; request methods enqueue existing generation state and do not move simulation policy into the managed owner.",
        "Counted managed R&D presentation `SystemBase` exception",
    ),
    ("Assets/Game/Scripts/Rendering/Baking/OperationMapRenderMaterialBaseColorBakingSystem.cs", "OperationMapRenderMaterialBaseColorBakingSystem", "ISystem"): (
        "Converted",
        "Keep as a baking-world-only `ISystem`; managed material reads occur only in the filtered post-baking boundary and publish ECS material-property data.",
        "`OperationMapRenderMaterialBaseColorBakingSystem : ISystem` baking boundary",
    ),
    ("Assets/Game/Scripts/Rendering/Baking/OperationMapRenderVirtualizationBakingSystem.cs", "OperationMapRenderVirtualizationBakingSystem", "ISystem"): (
        "Converted",
        "Keep as a baking-world-only `ISystem`; the lexical Material blocker is ECS render metadata and the system publishes deterministic baked virtualization data.",
        "`OperationMapRenderVirtualizationBakingSystem : ISystem` baking boundary",
    ),
}

MANUAL_RISK_OVERRIDES: dict[tuple[str, str, str], str] = {
    ("Assets/Game/Scripts/Environment/RuntimeCityRAndDMapSystem.cs", "RuntimeCityRAndDMapSystem", "SystemBase"):
        "Low: reviewed managed R&D presentation boundary only",
    ("Assets/Game/Scripts/Rendering/Systems/UnitAttachedLightSystem.cs", "UnitAttachedLightSystem", "SystemBase"):
        "Low: reviewed managed attached-light presentation boundary only",
    ("Assets/Game/Scripts/Rendering/Systems/UnitSelectionObjectOutlinePresentationSystem.cs", "UnitSelectionObjectOutlinePresentationSystem", "SystemBase"):
        "Low: reviewed managed selection outline presentation boundary only",
    ("Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs", "CombatGameObjectVfxPlaybackSystem", "SystemBase"):
        "Low: reviewed managed VFX playback boundary only",
    ("Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs", "UnitAttackVfxRequestSystem", "SystemBase"):
        "Low: reviewed managed VFX playback boundary only",
}


@dataclass(frozen=True)
class Declaration:
    id: str
    type: str
    kind: str
    current_base: str
    path: str
    line: int
    scope: str
    owner_lane: str
    disposition: str
    managed_blockers: str
    gameplay_policy_risk: str
    public_api_call_sites: str
    first_safe_slice: str
    replacement_target: str
    validation_gate: str
    status: str
    accessibility: str
    namespace: str
    attributes: str
    lifecycle_methods: str
    public_members: str
    ecs_access: str
    managed_field_categories: str
    key: str


def normalize(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def strip_comments_and_strings(text: str) -> str:
    result: list[str] = []
    i = 0
    length = len(text)
    in_line_comment = False
    in_block_comment = False
    in_string = False
    in_verbatim_string = False
    in_char = False
    while i < length:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < length else ""

        if in_line_comment:
            if ch == "\n":
                in_line_comment = False
                result.append(ch)
            else:
                result.append(" ")
            i += 1
            continue

        if in_block_comment:
            if ch == "*" and nxt == "/":
                result.append(" ")
                result.append(" ")
                in_block_comment = False
                i += 2
            else:
                result.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if in_string:
            if ch == "\\" and not in_verbatim_string:
                result.append(" ")
                if i + 1 < length:
                    result.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if in_verbatim_string and ch == '"' and nxt == '"':
                result.append(" ")
                result.append(" ")
                i += 2
                continue
            if ch == '"':
                in_string = False
                in_verbatim_string = False
            result.append("\n" if ch == "\n" else " ")
            i += 1
            continue

        if in_char:
            if ch == "\\":
                result.append(" ")
                if i + 1 < length:
                    result.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if ch == "'":
                in_char = False
            result.append("\n" if ch == "\n" else " ")
            i += 1
            continue

        if ch == "/" and nxt == "/":
            result.append(" ")
            result.append(" ")
            in_line_comment = True
            i += 2
            continue

        if ch == "/" and nxt == "*":
            result.append(" ")
            result.append(" ")
            in_block_comment = True
            i += 2
            continue

        if ch == "@" and nxt == '"':
            result.append(" ")
            result.append(" ")
            in_string = True
            in_verbatim_string = True
            i += 2
            continue

        if ch == '"':
            result.append(" ")
            in_string = True
            in_verbatim_string = False
            i += 1
            continue

        if ch == "'":
            result.append(" ")
            in_char = True
            i += 1
            continue

        result.append(ch)
        i += 1

    return "".join(result)


def declaration_key(path: str, type_name: str, kind: str, current_base: str) -> str:
    return f"{path}|{type_name}"


def parse_existing_ids(output_path: Path) -> dict[str, str]:
    if not output_path.exists():
        return {}

    ids: dict[str, str] = {}
    in_inventory = False
    for line in output_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("## "):
            if line == "## Inventory":
                in_inventory = True
                continue
            if in_inventory:
                break
        if not in_inventory:
            continue
        if not line.startswith("| `P7-"):
            continue
        cells = parse_markdown_row(line)
        if len(cells) < 7:
            continue
        row_id = cells[0].strip("`")
        type_name = cells[1].strip("`")
        kind = cells[2].strip("`")
        current_base = cells[3].strip("`")
        path = cells[4].strip("`")
        ids[declaration_key(path, type_name, kind, current_base)] = row_id
    return ids


def resolve_existing_id(row: Declaration, existing_ids: dict[str, str]) -> str | None:
    exact = existing_ids.get(row.key)
    if exact is not None:
        return exact

    suffix = f"|{row.type}"
    matches = [row_id for key, row_id in existing_ids.items() if key.endswith(suffix)]
    if len(matches) == 1:
        return matches[0]
    return None


def parse_markdown_row(line: str) -> list[str]:
    trimmed = line.strip()
    if trimmed.startswith("|"):
        trimmed = trimmed[1:]
    if trimmed.endswith("|"):
        trimmed = trimmed[:-1]

    cells: list[str] = []
    cell_start = 0
    for index, char in enumerate(trimmed):
        if char != "|":
            continue
        escaped = index > 0 and trimmed[index - 1] == "\\"
        if escaped:
            continue
        cells.append(trimmed[cell_start:index].strip().replace("\\|", "|"))
        cell_start = index + 1
    cells.append(trimmed[cell_start:].strip().replace("\\|", "|"))
    return cells


def find_namespace(clean_text: str, index: int) -> str:
    namespace = ""
    for match in NAMESPACE_RE.finditer(clean_text[:index]):
        namespace = match.group(1)
    return namespace


def access_from_modifiers(modifiers: str) -> str:
    tokens = set(modifiers.split())
    for access in ("public", "internal", "private", "protected"):
        if access in tokens:
            return access
    return "implicit"


def current_base_from_bases(bases: str) -> str | None:
    for token in ECS_TOKENS:
        if re.search(rf"\b{re.escape(token)}\b", bases):
            return token
    return None


def declaration_body_for(clean_text: str, match: re.Match[str]) -> str:
    declaration_end = match.end()
    open_brace = clean_text.find("{", declaration_end)
    if open_brace < 0:
        return clean_text[match.start():declaration_end]

    depth = 0
    for index in range(open_brace, len(clean_text)):
        char = clean_text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return clean_text[match.start():index + 1]

    return clean_text[match.start():]


def scope_for(path: str) -> str:
    if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
        return "Editor"
    if "/Tests/" in path or path.startswith("Assets/Tests/"):
        return "Test"
    if path.startswith("Assets/Game/Scripts/UI/"):
        return "ProductionUI"
    return "ProductionNonUI"


def owner_lane_for(path: str, type_name: str) -> str:
    if path.startswith("Assets/Game/Scripts/UI/"):
        return "Integration"
    if path.startswith("Assets/Game/Scripts/Rendering/") or any(token in type_name for token in ("Visual", "Vfx", "Trace", "Impostor", "Marker", "Camera", "Quality", "Light", "Render")):
        return "AgentF"
    if path.startswith("Assets/Game/Scripts/Environment/") or type_name.startswith(("Road", "RuntimeCity", "RuntimeGrid", "RuntimeDecoration", "DayNight", "Citizen")):
        return "AgentE"
    if type_name.startswith(("Building", "MapBuilding", "BuildDrawer")):
        return "AgentD"
    if type_name.startswith(("RtsSelection", "Selection", "FocusableUnit", "FocusedUnit", "SelectedUnit")):
        return "AgentC"
    if type_name.startswith(("RuntimeGameplayState", "RuntimeDiagnostics", "PerformanceDiagnostics", "AI", "FactionEconomy", "RuntimeGridBootstrap", "InitialFaction", "InitialUnits")):
        return "AgentB"
    if path.startswith("Assets/Game/Scripts/Composition/"):
        return "AgentB"
    return "Integration"


def managed_blockers_for(text: str) -> list[str]:
    blocker_text = UNMANAGED_ECS_OBJECT_REFERENCE_RE.sub("UnityObjectRef", text)
    return [name for name, pattern in MANAGED_BLOCKER_PATTERNS if pattern.search(blocker_text)]


def ecs_access_for(text: str) -> list[str]:
    return [name for name, pattern in ECS_ACCESS_PATTERNS if pattern.search(text)]


def lifecycle_methods_for(clean_text: str) -> list[str]:
    methods = [
        method
        for method in LIFECYCLE_METHODS
        if re.search(rf"\b{re.escape(method)}\s*\(", clean_text)
    ]
    coroutine_methods = [match.group("name") for match in COROUTINE_RE.finditer(clean_text)]
    return methods + [f"Coroutine:{name}" for name in coroutine_methods]


def public_members_for(clean_text: str) -> list[str]:
    members: list[str] = []
    for match in PUBLIC_MEMBER_RE.finditer(clean_text):
        name = match.group("name")
        if name in ("class", "struct", "interface", "enum"):
            continue
        shape = "method" if match.group("shape") == "(" else "property"
        members.append(f"{name} ({shape})")
    return sorted(set(members), key=str.casefold)


def attributes_for(declaration_text: str) -> list[str]:
    open_brace = declaration_text.find("{")
    declaration_header = declaration_text if open_brace < 0 else declaration_text[:open_brace]
    return [match.group(0).replace("\n", " ") for match in ATTRIBUTE_RE.finditer(declaration_header)]


def managed_field_categories(blockers: list[str], text: str) -> str:
    categories: list[str] = []
    if any(blocker in blockers for blocker in ("GameObject", "UnityEngine.Object", "Object.Instantiate", "Object.Destroy")):
        categories.append("prefab/reference")
    if any(blocker in blockers for blocker in ("Transform", "Camera", "Renderer", "Material", "Light", "ParticleSystem", "LineRenderer", "VisualEffect")):
        categories.append("presentation view")
    if "ScriptableObject" in blockers:
        categories.append("config asset")
    if "List<GameObject>" in blockers or "Dictionary<..., GameObject>" in blockers:
        categories.append("managed collection")
    if re.search(r"\bNative(?:Array|List|HashMap|HashSet|Queue|Reference)\b", text):
        categories.append("native container")
    if re.search(r"\b(ComponentLookup|BufferLookup|EntityQuery|EntityTypeHandle|ComponentTypeHandle)\b", text):
        categories.append("query/lookup/cache")
    if not categories:
        categories.append("None")
    return ", ".join(dict.fromkeys(categories))


def gameplay_policy_risk(type_name: str, text: str, blockers: list[str]) -> str:
    hits = [token for token in GAMEPLAY_POLICY_TOKENS if token in type_name or token in text]
    if blockers and hits:
        return "High: managed blockers mixed with " + ", ".join(sorted(set(hits))[:4])
    if hits:
        return "Medium: " + ", ".join(sorted(set(hits))[:4])
    if blockers:
        return "Low: managed boundary only"
    return "None"


def disposition_for(scope: str, current_base: str, owner: str, blockers: list[str], public_members: list[str], risk: str, type_name: str) -> str:
    if scope == "ProductionUI":
        return "UIOutOfScope"
    if scope == "Editor":
        return "EditorOutOfScope"
    if scope == "Test":
        return "TestOutOfScope"
    if current_base == "ISystem" and blockers:
        return "ReviewRequired"
    if current_base == "ISystem":
        return "Converted"
    if blockers:
        if risk.startswith("High"):
            return "SplitThenConvert"
        if owner == "AgentF" or any(blocker in blockers for blocker in ("Camera", "Renderer", "Material", "Light", "ParticleSystem", "LineRenderer", "VisualEffect")):
            return "ManagedPresentationSystemBaseException"
        return "SplitThenConvert"
    if len(public_members) > 8 or risk.startswith("High"):
        return "ReviewRequired"
    if any(token in type_name for token in ("Composition", "Context", "Query", "ReadModel", "Boundary")) and current_base in SYSTEM_BASE_TOKENS:
        return "RetireFold"
    return "DirectConvert"


def first_safe_slice_for(disposition: str, type_name: str, blockers: list[str]) -> str:
    if disposition == "DirectConvert":
        return f"Convert `{type_name}` to `ISystem` preserving update attributes and ECS inputs/outputs."
    if disposition == "SplitThenConvert":
        return "Split pure ECS request/state processing from managed Unity-object/config boundary first."
    if disposition == "RetireFold":
        return "Search call sites and fold helper behavior into the owning ECS system or pure static helper."
    if disposition == "ManagedPresentationSystemBaseException":
        return "Document concrete Unity-object ticking blocker and ensure no gameplay policy remains here."
    if disposition == "Converted":
        return "Keep as `ISystem`; verify no managed blockers are introduced."
    if disposition.endswith("OutOfScope"):
        return "Out of Phase 7 non-UI gameplay denominator."
    return "Manual review before conversion."


def replacement_for(disposition: str, type_name: str) -> str:
    if disposition in ("DirectConvert", "Converted"):
        return f"`{type_name} : ISystem`"
    if disposition == "SplitThenConvert":
        return "Narrow `ISystem` processors plus explicit managed boundary if needed."
    if disposition == "RetireFold":
        return "Folded helper or retired file."
    if disposition == "ManagedPresentationSystemBaseException":
        return "Counted managed presentation/config/camera `SystemBase` exception."
    if disposition.endswith("OutOfScope"):
        return disposition
    return "ReviewRequired"


def status_for(disposition: str) -> str:
    if disposition == "Converted":
        return "Converted"
    if disposition == "ManagedPresentationSystemBaseException":
        return "ManagedException"
    if disposition.endswith("OutOfScope"):
        return "Deferred"
    return "Open"


def validation_for(owner: str, disposition: str) -> str:
    if disposition.endswith("OutOfScope"):
        return "Out of Phase 7 validation matrix."
    return VALIDATION_BY_OWNER.get(owner, "Architecture guard + compile")


def enumerate_declarations(root: Path, existing_ids: dict[str, str]) -> list[Declaration]:
    rows: list[Declaration] = []
    for path in sorted(root.rglob("*.cs")):
        rel_path = normalize(path)
        text = path.read_text(encoding="utf-8")
        clean_text = strip_comments_and_strings(text)
        for match in TYPE_DECLARATION_RE.finditer(clean_text):
            bases = (match.group("bases") or "").lstrip(":").replace("\n", " ").strip()
            current_base = current_base_from_bases(bases)
            if current_base is None:
                continue

            type_name = match.group("name")
            kind = match.group("kind")
            key = declaration_key(rel_path, type_name, kind, current_base)
            declaration_body = declaration_body_for(clean_text, match)

            line = clean_text.count("\n", 0, match.start()) + 1
            scope = scope_for(rel_path)
            owner = owner_lane_for(rel_path, type_name)
            blockers = managed_blockers_for(declaration_body)
            lifecycles = lifecycle_methods_for(declaration_body)
            public_members = public_members_for(declaration_body)
            risk = gameplay_policy_risk(type_name, declaration_body, blockers)
            manual_risk_override = MANUAL_RISK_OVERRIDES.get((rel_path, type_name, current_base))
            if manual_risk_override is not None:
                risk = manual_risk_override
            disposition = disposition_for(scope, current_base, owner, blockers, public_members, risk, type_name)
            first_safe_slice = first_safe_slice_for(disposition, type_name, blockers)
            replacement_target = replacement_for(disposition, type_name)
            manual_override = MANUAL_REVIEW_OVERRIDES.get((rel_path, type_name, current_base))
            if manual_override is not None:
                disposition, first_safe_slice, replacement_target = manual_override
            rows.append(
                Declaration(
                    id="",
                    type=type_name,
                    kind=kind,
                    current_base=current_base,
                    path=rel_path,
                    line=line,
                    scope=scope,
                    owner_lane=owner,
                    disposition=disposition,
                    managed_blockers=", ".join(blockers) if blockers else "None",
                    gameplay_policy_risk=risk,
                    public_api_call_sites=", ".join(public_members) if public_members else "None",
                    first_safe_slice=first_safe_slice,
                    replacement_target=replacement_target,
                    validation_gate=validation_for(owner, disposition),
                    status=status_for(disposition),
                    accessibility=access_from_modifiers(match.group("modifiers") or ""),
                    namespace=find_namespace(clean_text, match.start()) or "None",
                    attributes=", ".join(attributes_for(declaration_body)) or "None",
                    lifecycle_methods=", ".join(lifecycles) if lifecycles else "None",
                    public_members=", ".join(public_members) if public_members else "None",
                    ecs_access=", ".join(ecs_access_for(declaration_body)) or "None",
                    managed_field_categories=managed_field_categories(blockers, declaration_body),
                    key=key,
                )
            )

    sorted_rows = sorted(rows, key=lambda row: (scope_sort_key(row.scope), row.owner_lane, row.path, row.type, row.current_base))
    next_id = next_inventory_id(existing_ids)
    numbered_rows: list[Declaration] = []
    for row in sorted_rows:
        row_id = resolve_existing_id(row, existing_ids)
        if row_id is None:
            row_id = f"P7-{next_id:04d}"
            next_id += 1
        numbered_rows.append(replace(row, id=row_id))

    return numbered_rows


def next_inventory_id(existing_ids: dict[str, str]) -> int:
    highest = 0
    for row_id in existing_ids.values():
        match = re.fullmatch(r"P7-(\d+)", row_id)
        if match:
            highest = max(highest, int(match.group(1)))
    return highest + 1


def scope_sort_key(scope: str) -> int:
    return {
        "ProductionNonUI": 0,
        "ProductionUI": 1,
        "Editor": 2,
        "Test": 3,
    }.get(scope, 9)


def shell_quote_command(root_arg: Path, output_arg: Path, json_output_arg: Path | None) -> str:
    command = [
        "python3",
        "Tools/Architecture/generate_systembase_to_isystem_inventory.py",
        "--root",
        normalize_or_abs(root_arg),
        "--output",
        normalize_or_abs(output_arg),
    ]
    if json_output_arg is not None:
        command.extend(["--json-output", normalize_or_abs(json_output_arg)])
    return " ".join(command)


def normalize_or_abs(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def markdown_escape(value: object) -> str:
    text = str(value)
    text = text.replace("\n", " ").replace("|", "\\|")
    return text


def markdown_code(value: object) -> str:
    return f"`{markdown_escape(value)}`"


def count_where(rows: list[Declaration], **criteria: str) -> int:
    return sum(1 for row in rows if all(getattr(row, key) == value for key, value in criteria.items()))


def count_current_base(rows: list[Declaration], base: str, scope: str | None = None) -> int:
    return sum(1 for row in rows if row.current_base == base and (scope is None or row.scope == scope))


def format_count_map(rows: list[Declaration], field: str) -> str:
    counts: dict[str, int] = {}
    for row in rows:
        value = str(getattr(row, field))
        counts[value] = counts.get(value, 0) + 1
    return ", ".join(f"`{key}` {counts[key]}" for key in sorted(counts, key=str.casefold))


def format_rows(rows: list[Declaration]) -> str:
    lines = [
        "| Id | Type | Kind | Current base | Path | Line | UI/editor/test scope | Owner lane | Disposition | Managed blockers | Gameplay policy risk | Public API/call sites | First safe slice | Replacement target | Validation gate | Status |",
        "| --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            "| "
            + " | ".join(
                [
                    markdown_code(row.id),
                    markdown_code(row.type),
                    markdown_code(row.kind),
                    markdown_code(row.current_base),
                    markdown_code(row.path),
                    str(row.line),
                    markdown_code(row.scope),
                    markdown_code(row.owner_lane),
                    markdown_code(row.disposition),
                    markdown_escape(row.managed_blockers),
                    markdown_escape(row.gameplay_policy_risk),
                    markdown_escape(row.public_api_call_sites),
                    markdown_escape(row.first_safe_slice),
                    markdown_escape(row.replacement_target),
                    markdown_escape(row.validation_gate),
                    markdown_code(row.status),
                ]
            )
            + " |"
        )
    return "\n".join(lines)


def format_details(rows: list[Declaration]) -> str:
    lines = [
        "| Id | Accessibility | Namespace | Attributes | Lifecycle methods | Public members | ECS access shape | Managed field categories | Stable key |",
        "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            "| "
            + " | ".join(
                [
                    markdown_code(row.id),
                    markdown_code(row.accessibility),
                    markdown_code(row.namespace),
                    markdown_escape(row.attributes),
                    markdown_escape(row.lifecycle_methods),
                    markdown_escape(row.public_members),
                    markdown_escape(row.ecs_access),
                    markdown_escape(row.managed_field_categories),
                    markdown_code(row.key),
                ]
            )
            + " |"
        )
    return "\n".join(lines)


def count_public_members(row: Declaration) -> int:
    if row.public_members == "None":
        return 0
    return row.public_members.count("(method)") + row.public_members.count("(property)")


def public_helper_members(row: Declaration) -> list[str]:
    if row.public_members == "None":
        return []

    lifecycle_or_runner = {
        "OnCreate",
        "OnStartRunning",
        "OnUpdate",
        "OnStopRunning",
        "OnDestroy",
        "Execute",
        "Dispose",
    }
    helpers: list[str] = []
    for member in [part.strip() for part in row.public_members.split(",") if part.strip()]:
        name = member.split(" ", 1)[0]
        if name not in lifecycle_or_runner:
            helpers.append(member)
    return helpers


def format_assignment_rows(rows: list[Declaration]) -> str:
    lines = [
        "| Id | Type | Base | Disposition | Status | Path | First safe slice | Validation gate |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            "| "
            + " | ".join(
                [
                    markdown_code(row.id),
                    markdown_code(row.type),
                    markdown_code(row.current_base),
                    markdown_code(row.disposition),
                    markdown_code(row.status),
                    markdown_code(row.path),
                    markdown_escape(row.first_safe_slice),
                    markdown_escape(row.validation_gate),
                ]
            )
            + " |"
        )
    return "\n".join(lines)


def format_owner_lane_sections(rows: list[Declaration]) -> str:
    sections: list[str] = ["## Owner Lane Assignments", ""]
    for lane in ("AgentB", "AgentC", "AgentD", "AgentE", "AgentF", "Integration"):
        lane_rows = [row for row in rows if row.owner_lane == lane]
        sections.extend(
            [
                f"### {lane}",
                "",
                f"Rows: `{len(lane_rows)}`.",
                "",
                format_assignment_rows(lane_rows),
                "",
            ]
        )
    return "\n".join(sections)


def format_manual_review_queue(rows: list[Declaration]) -> str:
    review_rows = [row for row in rows if row.disposition == "ReviewRequired"]
    sections = [
        "## Manual Review Queue",
        "",
        f"Rows requiring Agent A/domain-owner review before conversion starts: `{len(review_rows)}`.",
        "",
    ]
    if review_rows:
        sections.append(format_assignment_rows(review_rows))
    else:
        sections.append("No rows currently require manual review.")
    sections.append("")
    return "\n".join(sections)


def format_manual_review_decisions(rows: list[Declaration]) -> str:
    reviewed_keys = {f"{path}|{type_name}|{current_base}" for path, type_name, current_base in MANUAL_REVIEW_OVERRIDES}
    reviewed_rows = [row for row in rows if f"{row.path}|{row.type}|{row.current_base}" in reviewed_keys]
    sections = [
        "## Manual Review Decisions",
        "",
        "These rows were previously `ReviewRequired` and were classified by Agent A without converting domain code.",
        "",
        f"Rows: `{len(reviewed_rows)}`.",
        "",
    ]
    if reviewed_rows:
        lines = [
            "| Id | Type | Base | Owner lane | Manual disposition | Status | Path | Reviewed first safe slice | Replacement target |",
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
        ]
        for row in reviewed_rows:
            lines.append(
                "| "
                + " | ".join(
                    [
                        markdown_code(row.id),
                        markdown_code(row.type),
                        markdown_code(row.current_base),
                        markdown_code(row.owner_lane),
                        markdown_code(row.disposition),
                        markdown_code(row.status),
                        markdown_code(row.path),
                        markdown_escape(row.first_safe_slice),
                        markdown_escape(row.replacement_target),
                    ]
                )
                + " |"
            )
        sections.append("\n".join(lines))
    else:
        sections.append("No manual review decisions are currently recorded.")
    sections.append("")
    return "\n".join(sections)


def format_broad_converted_review(rows: list[Declaration]) -> str:
    broad_rows = [
        row
        for row in rows
        if row.status == "Converted" and count_public_members(row) > 8
    ]
    sections = [
        "## Broad Converted ISystem Review Debt",
        "",
        "Converted rows in this section already use `ISystem`, but their public/helper surface exceeds the Phase 7 broad-system threshold and must be reviewed before using them as replacement patterns.",
        "",
        f"Rows: `{len(broad_rows)}`.",
        "",
    ]
    if broad_rows:
        lines = [
            "| Id | Type | Public member count | Owner lane | Path | Validation gate |",
            "| --- | --- | ---: | --- | --- | --- |",
        ]
        for row in broad_rows:
            lines.append(
                "| "
                + " | ".join(
                    [
                        markdown_code(row.id),
                        markdown_code(row.type),
                        str(count_public_members(row)),
                        markdown_code(row.owner_lane),
                        markdown_code(row.path),
                        markdown_escape(row.validation_gate),
                    ]
                )
                + " |"
            )
        sections.append("\n".join(lines))
    else:
        sections.append("No converted rows exceed the broad-system threshold.")
    sections.append("")
    return "\n".join(sections)


def format_converted_public_helper_review(rows: list[Declaration]) -> str:
    helper_rows = [
        row
        for row in rows
        if row.status == "Converted" and public_helper_members(row)
    ]
    sections = [
        "## Converted Public Helper API Review Debt",
        "",
        "Converted rows in this section already use `ISystem`, but still expose public/internal helper APIs beyond lifecycle/runner methods. Domain conversion slices must replace these helpers with ECS request/result data, plain stateless helpers, or documented integration exceptions before marking the related Phase 7 row finally clean.",
        "",
        f"Rows: `{len(helper_rows)}`.",
        "",
    ]
    if helper_rows:
        lines = [
            "| Id | Type | Helper count | Owner lane | Path | Helper APIs |",
            "| --- | --- | ---: | --- | --- | --- |",
        ]
        for row in helper_rows:
            helpers = public_helper_members(row)
            lines.append(
                "| "
                + " | ".join(
                    [
                        markdown_code(row.id),
                        markdown_code(row.type),
                        str(len(helpers)),
                        markdown_code(row.owner_lane),
                        markdown_code(row.path),
                        markdown_escape(", ".join(helpers)),
                    ]
                )
                + " |"
            )
        sections.append("\n".join(lines))
    else:
        sections.append("No converted rows expose public helper APIs beyond lifecycle/runner methods.")
    sections.append("")
    return "\n".join(sections)


def render_markdown(rows: list[Declaration], command: str, source_root: Path) -> str:
    production_rows = [row for row in rows if row.scope in ("ProductionNonUI", "ProductionUI")]
    production_non_ui = [row for row in rows if row.scope == "ProductionNonUI"]
    production_systembase = sum(1 for row in production_rows if row.current_base in SYSTEM_BASE_TOKENS)
    production_isystem = count_current_base(production_rows, "ISystem")
    share = 100.0 * production_isystem / max(1, production_isystem + production_systembase)

    sections = [
        "# SystemBase To ISystem Inventory",
        "",
        "Generated deterministically from the current source; wall-clock timestamp omitted.",
        f"Command: `{command}`.",
        f"Source root: `{normalize_or_abs(source_root)}`.",
        "",
        "## Summary",
        "",
        f"- Total ECS system declarations: `{len(rows)}`.",
        f"- Production `SystemBase`/legacy declarations: `{production_systembase}`.",
        f"- Production `ISystem` declarations: `{production_isystem}`.",
        f"- Current production `ISystem` share: `{share:.1f}%`.",
        f"- Production non-UI rows: `{len(production_non_ui)}`.",
        f"- Production UI rows: `{count_where(rows, scope='ProductionUI')}`.",
        f"- Editor rows: `{count_where(rows, scope='Editor')}`.",
        f"- Test rows: `{count_where(rows, scope='Test')}`.",
        f"- Scopes: {format_count_map(rows, 'scope')}.",
        f"- Owner lanes: {format_count_map(rows, 'owner_lane')}.",
        f"- Dispositions: {format_count_map(rows, 'disposition')}.",
        f"- Statuses: {format_count_map(rows, 'status')}.",
        "",
        "## Inventory",
        "",
        format_rows(rows),
        "",
        format_owner_lane_sections(rows),
        format_manual_review_queue(rows),
        format_manual_review_decisions(rows),
        format_broad_converted_review(rows),
        format_converted_public_helper_review(rows),
        "## Extended Details",
        "",
        format_details(rows),
        "",
    ]
    return "\n".join(sections)


def render_json(rows: list[Declaration]) -> str:
    return json.dumps([asdict(row) for row in rows], indent=2, sort_keys=True)


def write_output(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def check_output(path: Path, expected: str) -> bool:
    return path.exists() and path.read_text(encoding="utf-8") == expected


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate the Phase 7 SystemBase to ISystem inventory.")
    parser.add_argument("--root", default=DEFAULT_ROOT.as_posix(), help="Source root to scan.")
    parser.add_argument("--output", default=DEFAULT_OUTPUT.as_posix(), help="Markdown inventory output path.")
    parser.add_argument("--json-output", default=None, help="Optional machine-readable JSON sidecar.")
    parser.add_argument("--check", action="store_true", help="Verify outputs are current without writing them.")
    args = parser.parse_args()

    source_root = (ROOT / args.root).resolve() if not Path(args.root).is_absolute() else Path(args.root)
    output_path = (ROOT / args.output).resolve() if not Path(args.output).is_absolute() else Path(args.output)
    json_output = None
    if args.json_output:
        json_output = (ROOT / args.json_output).resolve() if not Path(args.json_output).is_absolute() else Path(args.json_output)

    existing_ids = parse_existing_ids(output_path)
    rows = enumerate_declarations(source_root, existing_ids)
    command = shell_quote_command(source_root, output_path, json_output)
    markdown = render_markdown(rows, command, source_root)
    json_content = render_json(rows) if json_output is not None else None

    if args.check:
        stale_outputs = []
        if not check_output(output_path, markdown):
            stale_outputs.append(output_path)
        if json_output is not None and json_content is not None and not check_output(json_output, json_content):
            stale_outputs.append(json_output)
        if stale_outputs:
            for stale_output in stale_outputs:
                print(f"stale generated output: {normalize_or_abs(stale_output)}")
            raise SystemExit(1)
        print(f"inventory is current: {normalize_or_abs(output_path)}")
        return

    write_output(output_path, markdown)
    if json_output is not None and json_content is not None:
        write_output(json_output, json_content)


if __name__ == "__main__":
    main()
