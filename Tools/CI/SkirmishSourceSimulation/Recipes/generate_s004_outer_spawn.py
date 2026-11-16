#!/usr/bin/env python3
"""Extract the original S004 outer spawn and opening-view source owners."""
import hashlib
import importlib.util
from pathlib import Path

SPAWN = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs"
VIEW = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishS005OpeningViewPolicy.cs"
EXTRACTOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/probe_s004_spawn_owners_compile.py"
OUTPUT = "SourceS004OuterSpawn.cs"


def once(text, old, new):
    if text.count(old) != 1: raise ValueError("Original S004 outer source seam changed: " + old)
    return text.replace(old, new, 1)


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    raw = {name: (repo / name).read_bytes() for name in (SPAWN, VIEW, EXTRACTOR)}
    spec = importlib.util.spec_from_file_location("s004_outer_original_extractor", repo / EXTRACTOR)
    extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
    spawn, view = (raw[name].decode("utf-8").replace("\r\n", "\n") for name in (SPAWN, VIEW))
    outer = extractor.extract_method(spawn, "TrySpawn")
    outer = once(outer, "private static void TrySpawn(", "internal static void TrySpawn(")
    if outer.count("Application.isPlaying") != 2: raise ValueError("Original outer play-state boundary changed")
    outer = outer.replace("Application.isPlaying", "SourceS004OpeningContext.RequireSourceStartupExecution(em, entity, em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup)")
    checkpoint = """            if (em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(entity))
            {
                SkirmishNativeCheckpointRecoveryRunner.Step(em, entity, setup);
                return;
            }"""
    outer = once(outer, checkpoint, '            if (em.HasComponentNamed(entity, "Game.Runtime.SkirmishNativeCheckpointRecoveryRecord")) throw new NotSupportedException("Native checkpoint recovery is unsupported in this source diagnostic");')
    outer = once(outer, "SkirmishMissionLogicalBindingProvider.TryAttachAfterActualSpawn", "SourceS004OuterBoundaries.RejectLogicalBinding")
    outer = once(outer, "SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn", "SourceS004OuterBoundaries.DeferNativePresentation")
    logical = extractor.extract_method(spawn, "CommitLogicalSpawn")
    text = ("using System;using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;"
            "using Unity.Collections;using Unity.Entities;namespace Game.Runtime {\n"
            "public partial struct SkirmishScenarioSpawnSystem {\n" + outer + "\n" + logical + "\n}\n}\n")
    for name, data in raw.items():
        if (repo / name).read_bytes() != data: raise ValueError("Original outer source changed: " + name)
    output.mkdir(parents=True, exist_ok=True)
    generated = output / OUTPUT
    with generated.open("w", encoding="utf-8", newline="\n") as stream: stream.write(text)
    return {"source_pins": {name: hashlib.sha256(data).hexdigest() for name, data in raw.items()},
            "outputs": {OUTPUT: hashlib.sha256(generated.read_bytes()).hexdigest()},
            "scope": "Original logical spawn transition with checked source execution; native presentation deferred and unsupported logical branches fail closed",
            "native_input_claimed": False, "mission_ready": False}
