#!/usr/bin/env python3
"""Read the S073 preset's exact referenced AI source assets, without a world."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

PRESET = "Assets/Game/Resources/SkirmishIndustrialBasin.asset"
CONTROLLERS = ["Assets/Game/Configs/Skirmish/Enemy.asset", "Assets/Game/Configs/Skirmish/Player.asset"]
PLAN = "Assets/Game/Configs/Skirmish/Plan.asset"
PARSER = "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
PROVENANCE = "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json"
SCRIPTS = {"AIControllerConfig": "Assets/Game/Scripts/Configs/GameplayConfigModels.cs.meta",
           "AIPlanEntryStartupConfig": "Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs.meta"}
PRESET_SCRIPT = "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs.meta"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path):
    rows = [line.split(":", 1)[1].strip() for line in path.read_text().splitlines() if line.startswith("guid:")]
    if len(rows) != 1 or len(rows[0]) != 32:
        raise ValueError("Exact unique asset GUID required: " + str(path))
    return rows[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    out = args.output.absolute()
    if out.exists():
        raise ValueError("Fresh output directory required")
    paths = [PRESET] + CONTROLLERS + [PLAN]
    inputs = [root / p for p in paths] + [root / (p + ".meta") for p in paths]
    inputs += [root / PARSER, root / PROVENANCE, root / PRESET_SCRIPT] + [root / p for p in SCRIPTS.values()]
    pins = {str(p): sha(p) for p in inputs}
    retained = json.loads((root / PROVENANCE).read_text())
    if retained.get("readback_parser") != {"path": PARSER, "sha256": pins[str(root / PARSER)]}:
        raise ValueError("Parser differs from retained source provenance")
    process = subprocess.run(["ruby", str(root / PARSER)] + [str(root / p) for p in paths],
                             check=True, capture_output=True, text=True, timeout=30)
    parsed = json.loads(process.stdout)
    rows = {p: parsed[str(root / p)]["MonoBehaviour"] for p in paths}
    preset = rows[PRESET]
    if (preset.get("m_Name") != "SkirmishIndustrialBasin" or
            preset.get("m_EditorClassIdentifier") != "Game.Configs::Game.Configs.SkirmishPresetConfig" or
            preset.get("m_Script") != {"fileID": 11500000, "guid": guid(root / PRESET_SCRIPT), "type": 3}):
        raise ValueError("S073 preset identity changed")
    expected = lambda p: {"fileID": 11400000, "guid": guid(root / (p + ".meta")), "type": 2}
    if preset.get("aiControllers") != [expected(p) for p in CONTROLLERS] or preset.get("aiPlan") != expected(PLAN):
        raise ValueError("Preset AI references do not match the exact source assets")
    nodes = []
    for path in CONTROLLERS + [PLAN]:
        kind = "AIPlanEntryStartupConfig" if path == PLAN else "AIControllerConfig"
        source = rows[path]
        if source.get("m_Script") != {"fileID": 11500000, "guid": guid(root / SCRIPTS[kind]), "type": 3}:
            raise ValueError("AI asset script identity changed: " + path)
        nodes.append({"asset_guid": guid(root / (path + ".meta")), "source_type": kind,
                      "source_path": path, "source_sha256": pins[str(root / path)],
                      "fields": {k: v for k, v in source.items() if not k.startswith("m_")}})
    if any(sha(Path(path)) != digest for path, digest in pins.items()):
        raise ValueError("AI source changed during readback")
    out.mkdir(parents=True)
    graph = {"schema_version": 1, "authority": "AuthoredConfigAssets", "nodes": nodes,
             "source_pins": pins, "compiled_setup": False, "world_initialized": False,
             "native_world_state_used": False, "preset_path": PRESET,
             "preset_ai_controllers": preset["aiControllers"], "preset_ai_plan": preset["aiPlan"],
             "scope": "Authored source readback only; no native or mission readiness"}
    (out / "source-ai-config-graph.json").write_text(json.dumps(graph, indent=2) + "\n")
    print(json.dumps({"status": "Passed", "nodes": len(nodes), "graph_sha256": sha(out / "source-ai-config-graph.json")}))


if __name__ == "__main__":
    main()
