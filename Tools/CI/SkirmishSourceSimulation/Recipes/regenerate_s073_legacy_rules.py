"""Project current legacy rule owners into the source host without policy copies."""
from pathlib import Path
import hashlib
import types


def generate(repo, output):
    root = Path(repo)
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    base = root / "Assets/Game/Scripts/Systems"
    names = ("SkirmishRulesSystem.cs", "SkirmishWorldSetup.cs", "SkirmishCombatPolicy.cs")
    raw = {name: (base / name).read_bytes() for name in names}
    texts = {name: value.decode("utf-8").replace("\r\n", "\n") for name, value in raw.items()}
    helper = Path(__file__).parent / "operation_map_publication/regenerate_config_metadata.py"
    helper_bytes = helper.read_bytes()
    lex = types.ModuleType("legacy_rules_lexical")
    lex.__file__ = str(helper)
    exec(compile(helper_bytes, str(helper), "exec"), lex.__dict__)

    rules = texts["SkirmishRulesSystem.cs"]
    rules = ("using Game.Components;using Game.Configs;using Unity.Entities;using Unity.Mathematics;"
             "using Unity.Collections;namespace Game.Runtime{public partial struct SkirmishRulesSystem : ISystem{\n"
             + lex.member(rules, "OnCreate") + "\n" + lex.member(rules, "OnUpdate") + "\n}}\n")
    world = texts["SkirmishWorldSetup.cs"]
    native_projection = lex.member(world, "ProjectNativePreset")
    if world.count(native_projection) != 1:
        raise ValueError("Exact native preset projection required")
    world = world.replace(native_projection, "", 1)
    original = "var preset=SkirmishPresetResolver.Load(em);\n            SkirmishPresetRuntimeValues values = ProjectNativePreset(preset);"
    if world.count(original) != 2:
        raise ValueError("Exact native world-setup preset consumers required")
    world = world.replace(original, "SkirmishPresetRuntimeValues values = SkirmishPresetRuntimeProjection.ResolveBoundSourceValues(em);")
    combat = texts["SkirmishCombatPolicy.cs"]
    original_combat = "SkirmishPresetResolver.Load(em)"
    if combat.count(original_combat) != 3:
        raise ValueError("Exact native combat preset consumers required")
    combat = combat.replace(original_combat, "SourceS073LegacyCombatPresetProvider.Resolve(em)")
    launch_path = root / "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs"
    launch_bytes = launch_path.read_bytes()
    launch = launch_bytes.decode("utf-8").replace("\r\n", "\n")
    prepare = ("using Game.Components;using Unity.Collections;using Unity.Entities;"
               "namespace Game.Composition{internal static class SkirmishLaunchProjection{\n"
               + lex.member(launch, "TryGet") + "\n" + lex.member(launch, "PrepareMap") + "\n}}\n")
    files = {"SourceS073LegacyRules.cs": rules, "SourceS073LegacyWorldSetup.cs": world,
             "SourceS073LegacyCombatPolicy.cs": combat, "SourceS073LegacyPrepareMap.cs": prepare}
    for name, value in files.items():
        (out / name).write_text(value)
    if any((base / name).read_bytes() != value for name, value in raw.items()) or helper.read_bytes() != helper_bytes or launch_path.read_bytes() != launch_bytes:
        raise ValueError("Legacy rule source changed during extraction")
    return {"kind": "S073CurrentLegacyRulesSourceProjection",
            "source_pins": {**{str(base / name): hashlib.sha256(value).hexdigest() for name, value in raw.items()}, str(launch_path): hashlib.sha256(launch_bytes).hexdigest()},
            "bindings": ["Original rule OnCreate and OnUpdate with shared existing TrackLosses",
                         "Original world-setup policy with source-bound preset values",
                         "Original combat policy with pinned source-bound combat fields",
                         "Original launch TryGet and PrepareMap, with no scene-loading claim"],
            "scope": "Source owner compile only; no scheduler or mission admission"}
