#!/usr/bin/env python3
"""Extract the original MatchSceneView Skirmish map identity decision."""
import hashlib
import re
from pathlib import Path

MATCH = "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs"
LAUNCH = "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs"
OUTPUT = "SourceS004OriginalMapIdentity.cs"


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    raw = {name: (repo / name).read_bytes() for name in (MATCH, LAUNCH)}
    match, launch = (raw[name].decode("utf-8").replace("\r\n", "\n") for name in (MATCH, LAUNCH))
    constants = []
    for name in ("MissionId", "ScenarioId"):
        matches = re.findall(r'public const string ' + name + r' = "[^"]+";', launch)
        if len(matches) != 1: raise ValueError("Original Skirmish launch identity constant changed: " + name)
        constants.append(matches[0])
    start = "                        string missionId = skirmish.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex"
    end = "                        selection = new OperationMapLaunchSelection("
    if match.count(start) != 1: raise ValueError("Original MatchSceneView map identity branch changed")
    end_at = match.find(end, match.index(start))
    if end_at < 0: raise ValueError("Original MatchSceneView map selection missing")
    block = match[match.index(start):end_at]
    if block.count("SkirmishLaunchProjection.MissionId") != 1 or block.count("SkirmishLaunchProjection.ScenarioId") != 1:
        raise ValueError("Original MatchSceneView fallback identity changed")
    block = block.replace("SkirmishLaunchProjection.MissionId", "SourceS004OriginalMapIdentity.MissionId")
    block = block.replace("SkirmishLaunchProjection.ScenarioId", "SourceS004OriginalMapIdentity.ScenarioId")
    text = ("using Game.Components;using Game.Configs;using Unity.Collections;namespace Game.Runtime {\n"
            "internal static class SourceS004OriginalMapIdentity {\n" + "\n".join(constants) +
            "\ninternal static void Resolve(SkirmishMatchState skirmish,out FixedString64Bytes scenario,out FixedString64Bytes mission) {\n" +
            block + "scenario=new FixedString64Bytes(scenarioId);mission=new FixedString64Bytes(missionId);\n}\n}\n}\n")
    for name, data in raw.items():
        if (repo / name).read_bytes() != data: raise ValueError("Original map identity source changed: " + name)
    output.mkdir(parents=True, exist_ok=True)
    generated = output / OUTPUT
    with generated.open("w", encoding="utf-8", newline="\n") as stream: stream.write(text)
    return {"source_pins": {name: hashlib.sha256(data).hexdigest() for name, data in raw.items()},
            "outputs": {OUTPUT: hashlib.sha256(generated.read_bytes()).hexdigest()},
            "scope": "Original MatchSceneView Skirmish identity expressions and launch constants only; no native scene or player admission",
            "native_input_claimed": False, "mission_ready": False}
