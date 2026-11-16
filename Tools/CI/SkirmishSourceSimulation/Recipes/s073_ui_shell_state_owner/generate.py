"""Generate an isolated S073 source-host copy of the original shell owner and call site."""
from __future__ import annotations

import hashlib
from pathlib import Path


RUNNER = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"
OWNER = "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs"
STARTUP = "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupDispositionState.cs"
COMPONENTS = "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs"
UI_ENUMS = "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs"
POPUP_ENUMS = "Assets/Game/Scripts/UI/Contracts/UiShellPopupContracts.cs"
HOST_TYPES = "Tools/CI/SkirmishSourceSimulation/Adapters/112_MetadataSystemTypes.cs"

OUTPUT_RUNNER = "SourceS073LegacyNormalRun.UiShellStateOwner.cs"
OUTPUT_OWNER = "UiShellStateSystem.source-host.cs"
OUTPUT_STARTUP = "UiShellStartupDispositionState.source.cs"
OUTPUT_COMPONENTS = "UiShellStateOwnerComponents.extracted.cs"
OUTPUT_HOST_TYPES = "S073HostMetadataSystemTypes.UiShellOwner.cs"

REGISTRATION = "            UiShellEcsGateway.RegisterAsRuntimeGateway();"
REGISTRATION_BOUNDARY = "            // Source-host external boundary: runtime gateway registration omitted."
HOST_CONSTRUCTION = "        using var host = new AutonomousWorld(em, buildings);"
OWNER_CALL = "        var uiShellStateOwnerInitialization = SourceS073UiShellStateOwnerBootstrap.Initialize(em, host);"
OWNER_ADMISSION = "        JToken uiShellStateOwnerDiagnosticOption = run[\"s073_ui_shell_state_owner_diagnostic\"];\n        if (uiShellStateOwnerDiagnosticOption != null && uiShellStateOwnerDiagnosticOption.Type != JTokenType.Boolean)\n            throw new InvalidDataException(\"S073 shell owner diagnostic selector must be an explicit boolean\");\n        bool uiShellStateOwnerDiagnosticRequested = (bool?)uiShellStateOwnerDiagnosticOption == true;\n        bool uiShellStateOwnerDiagnosticEnvironmentOptIn = string.Equals(Environment.GetEnvironmentVariable(\"WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC\"), \"1\", StringComparison.Ordinal);\n        if (uiShellStateOwnerDiagnosticRequested != uiShellStateOwnerDiagnosticEnvironmentOptIn)\n            throw new InvalidDataException(\"S073 shell owner diagnostic requires matching explicit run-profile and environment opt-ins\");\n        JObject uiShellStateOwnerInitialization = null;\n        if (uiShellStateOwnerDiagnosticRequested)\n        {\n            uiShellStateOwnerInitialization = SourceS073UiShellStateOwnerBootstrap.Initialize(em, host);\n        }"
RESULT_ANCHOR = '            ["initialization_owner"] = "CustomGameStartupSystemHelper.InitializeFromLegacyData",'
PROFILE_FIELDS = (
    '            ["source_world_profile"] = "S073UiShellStateOwnerOnCreateV1_NonNative",\n'
    '            ["ui_shell_state_owner_diagnostic_admitted"] = uiShellStateOwnerDiagnosticRequested,\n'
    '            ["ui_shell_state_owner_initialization"] = uiShellStateOwnerInitialization,\n'
)

# These are the production declarations absent from the pinned S073 host source.
# Four contracts are intentionally already supplied by the S073/UI ARIA closure:
# UiShellRootComponent, UiShellStateComponent, UiMatchHudSquadTrayStateComponent,
# and MatchIntroTransitionComponent.
COMPONENT_NAMES = (
    "UiShellCommanderProfileComponent", "UiShellMainMenuResourcesComponent",
    "UiBuildDrawerDetailComponent", "UiBuildDrawerStateComponent",
    "UiBuildDrawerActiveProductionComponent", "UiBuildPlacementConfirmationBarComponent",
    "UiResourceExchangeStateComponent", "UiResourceExchangeDetailComponent",
    "UiBuildDrawerCatalogItemComponent", "UiBuildDrawerQueueRowComponent",
    "UiResourceExchangeRecipeCardComponent", "UiResourceExchangeQueueRowComponent",
    "UiMatchHudHeaderComponent", "UiMatchHudStatusSurfacesComponent",
    "UiMatchHudMinimapComponent", "UiDiagnosticsOverlayComponent",
    "UiShellLoadingProgressComponent", "UiShellArmoryCategoryComponent",
    "UiShellActivePopupComponent", "UiMatchHudPassengerDrawerStateComponent",
    "UiMatchIdentityReadModelComponent", "UiShellArmoryCategoryRequestComponent",
    "UiActionRequestComponent", "UiShellRouteRequestComponent",
    "UiShellRouteHistoryComponent", "UiShellPopupRequestComponent",
    "UiShellPresentationCommandComponent", "UiShellTransitionCompleteComponent",
    "UiShellLoadingProgressRequestComponent", "UiShellStartupDispositionComponent",
)
COMPONENT_ENUMS = ("UiResourceExchangeTab", "UiResourceExchangeQueueState", "UiShellStartupDisposition")
CONTRACT_ENUM_SOURCES = (
    (UI_ENUMS, "ArmoryCatalogCategory"),
    (UI_ENUMS, "UiActionKind"),
    (UI_ENUMS, "UiShellRouteIntent"),
    (UI_ENUMS, "UiShellCommandKind"),
    (UI_ENUMS, "UiShellRegionId"),
    (UI_ENUMS, "UiMatchHudObjectiveIconKind"),
    (POPUP_ENUMS, "UiShellPopupKind"),
    (POPUP_ENUMS, "UiShellPopupIntent"),
)


def _read(repo: Path, relative: str) -> tuple[str, str]:
    path = (repo / relative).resolve(strict=True)
    raw = path.read_bytes()
    return raw.decode("utf-8"), hashlib.sha256(raw).hexdigest()


def _replace_once(text: str, old: str, new: str, label: str) -> str:
    if text.count(old) != 1:
        raise ValueError(f"Expected one exact {label} anchor; found {text.count(old)}")
    return text.replace(old, new, 1)


def _declaration(text: str, kind: str, name: str, source: str) -> str:
    marker = f"public {kind} {name}"
    if text.count(marker) != 1:
        raise ValueError(f"Expected one original declaration {marker} in {source}")
    start = text.index(marker)
    brace = text.index("{", start)
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise ValueError(f"Unclosed original declaration {marker} in {source}")


def generate(repo: Path, output: Path) -> dict:
    repo = Path(repo).resolve(strict=True)
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    relative_inputs = (RUNNER, OWNER, STARTUP, COMPONENTS, UI_ENUMS, POPUP_ENUMS, HOST_TYPES)
    inputs = {relative: _read(repo, relative) for relative in relative_inputs}
    source_pins = {str((repo / relative).resolve(strict=True)): digest for relative, (_, digest) in inputs.items()}

    runner = inputs[RUNNER][0]
    runner_with_call = _replace_once(runner, HOST_CONSTRUCTION, HOST_CONSTRUCTION + "\n" + OWNER_ADMISSION,
                                     "retained host-construction lifecycle")
    runner_generated = _replace_once(runner_with_call, RESULT_ANCHOR,
                                     RESULT_ANCHOR + "\n" + PROFILE_FIELDS.rstrip("\n"),
                                     "source result profile record")
    restored_runner = runner_generated.replace("\n" + OWNER_ADMISSION, "", 1)
    restored_runner = restored_runner.replace("\n" + PROFILE_FIELDS.rstrip("\n"), "", 1)
    if restored_runner != runner:
        raise ValueError("Runner generation changed bytes outside the two authorized integration insertions")
    if runner_generated.index(OWNER_ADMISSION) < runner_generated.index(HOST_CONSTRUCTION):
        raise ValueError("Shell owner call must occur after the retained host constructor")
    (output / OUTPUT_RUNNER).write_text(runner_generated, encoding="utf-8")

    owner = inputs[OWNER][0]
    owner_host = _replace_once(owner, REGISTRATION, REGISTRATION_BOUNDARY, "gateway registration boundary")
    if owner_host.replace(REGISTRATION_BOUNDARY, REGISTRATION, 1) != owner:
        raise ValueError("Shell owner source changed outside gateway registration exclusion")
    (output / OUTPUT_OWNER).write_text(owner_host, encoding="utf-8")
    (output / OUTPUT_STARTUP).write_text(inputs[STARTUP][0], encoding="utf-8")

    component_source = inputs[COMPONENTS][0]
    component_declarations = [_declaration(component_source, "struct", name, COMPONENTS)
                              for name in COMPONENT_NAMES]
    component_declarations += [_declaration(component_source, "enum", name, COMPONENTS)
                               for name in COMPONENT_ENUMS]
    enum_declarations = [_declaration(inputs[path][0], "enum", name, path)
                         for path, name in CONTRACT_ENUM_SOURCES]
    contract_extract = (
        "using Unity.Collections;\nusing Unity.Entities;\nusing Game.UI.Contracts;\n"
        "namespace Game.UI.Shell.Contracts.Ecs {\n" + "\n".join(component_declarations) + "\n}\n"
        "namespace Game.UI.Contracts {\n" + "\n".join(enum_declarations[:6]) + "\n}\n"
        "namespace Game.UI.Contracts {\n" + "\n".join(enum_declarations[6:]) + "\n}\n"
    )
    if "struct UiShellRootComponent" in contract_extract or "struct UiShellStateComponent" in contract_extract:
        raise ValueError("Contract extraction must not duplicate the pinned S073 shell root/state contracts")
    (output / OUTPUT_COMPONENTS).write_text(contract_extract, encoding="utf-8")

    host_types = inputs[HOST_TYPES][0]
    original_fixed_string = "public struct FixedString4096Bytes{string value;"
    adapted_fixed_string = (
        "public struct FixedString4096Bytes{string value;"
        "public FixedString4096Bytes(string initial){"
        "if(System.Text.Encoding.UTF8.GetByteCount(initial??\"\")>4093)throw new System.ArgumentException();"
        "value=initial;}"
    )
    host_types_generated = _replace_once(host_types, original_fixed_string, adapted_fixed_string,
                                         "S073 host fixed-string constructor boundary")
    if host_types_generated.replace(adapted_fixed_string, original_fixed_string, 1) != host_types:
        raise ValueError("S073 host adapter changed outside the fixed-string value constructor")
    (output / OUTPUT_HOST_TYPES).write_text(host_types_generated, encoding="utf-8")

    recipe = Path(__file__).resolve(strict=True)
    source_pins[str(recipe)] = hashlib.sha256(recipe.read_bytes()).hexdigest()
    outputs = {
        OUTPUT_RUNNER: hashlib.sha256(runner_generated.encode("utf-8")).hexdigest(),
        OUTPUT_OWNER: hashlib.sha256(owner_host.encode("utf-8")).hexdigest(),
        OUTPUT_STARTUP: hashlib.sha256(inputs[STARTUP][0].encode("utf-8")).hexdigest(),
        OUTPUT_COMPONENTS: hashlib.sha256(contract_extract.encode("utf-8")).hexdigest(),
        OUTPUT_HOST_TYPES: hashlib.sha256(host_types_generated.encode("utf-8")).hexdigest(),
    }
    return {
        "source_pins": source_pins,
        "outputs": outputs,
        "scope": "Opt-in non-native S073 source-host initialization only. Original owner/default methods retained; gateway registration excluded; text lookup is fallback-only; no flow update, route/completion, ARIA state, Unity runtime, or readiness claim.",
        "authorized_transformations": [
            "Insert a matching explicit run-profile and environment admission check immediately after AutonomousWorld(em, buildings); invoke original OnCreate there only when both opt-ins are true, before PrepareFreshInitialRoads.",
            "Insert one profile/readback field in the S073 result JSON.",
            "Omit only UiShellEcsGateway.RegisterAsRuntimeGateway() from original UiShellStateSystem.OnCreate.",
            "Extract selected component/enum declarations verbatim from pinned contracts.",
            "Add the constructor used by the original FixedString4096Bytes diagnostic default to a generated S073 host adapter copy.",
        ],
        "host_binding": {"source_profile": "S073UiShellStateOwnerOnCreateV1_NonNative",
                         "owner_call_position": "After retained AutonomousWorld constructor; before source road/map/session startup",
                         "source_world_entity_id_shift_expected": True},
    }
