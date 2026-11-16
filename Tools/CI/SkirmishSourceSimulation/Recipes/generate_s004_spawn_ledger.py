#!/usr/bin/env python3
"""Extract original spawn/cleanup owners for an explicitly scoped source run."""
import hashlib
import importlib.util
from pathlib import Path

SPAWN = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs"
TRANSIENT = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishNativeCheckpointTransientState.cs"
EXTRACTOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/probe_s004_spawn_owners_compile.py"
OUTPUT = "SourceS004SpawnLedger.cs"


def once(text, old, new):
    if text.count(old) != 1:
        raise ValueError("Original S004 ledger seam changed: " + old)
    return text.replace(old, new, 1)


def extract_type(source, masked, signature):
    if masked.count(signature) != 1:
        raise ValueError("Original S004 cleanup type changed")
    start = masked.index(signature)
    brace = masked.index("{", start)
    depth, end = 1, brace + 1
    while depth:
        if end >= len(masked):
            raise ValueError("Unclosed original cleanup type")
        if masked[end] == "{": depth += 1
        elif masked[end] == "}": depth -= 1
        end += 1
    return source[start:end]


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    inputs = {path: (repo / path).read_bytes() for path in (SPAWN, TRANSIENT, EXTRACTOR)}
    module_spec = importlib.util.spec_from_file_location("s004_source_owner_extractor", repo / EXTRACTOR)
    module = importlib.util.module_from_spec(module_spec)
    module_spec.loader.exec_module(module)
    spawn, transient = (inputs[name].decode("utf-8").replace("\r\n", "\n") for name in (SPAWN, TRANSIENT))
    ledger = module.extract_method(spawn, "TrySpawnLedgers")
    if ledger.count("Application.isPlaying") != 2:
        raise ValueError("Original source play-state boundary changed")
    ledger = ledger.replace("Application.isPlaying", "SourceS004OpeningContext.RequireSourceStartupExecution(em, session, setup)")
    ledger = once(ledger, "Entity spawned = CreateForceMember(", "Entity spawned = GeneratedTypedOpeningActorSource.CreateForceMember(")
    cleanup = module.extract_method(spawn, "DestroyAttemptOwned")
    cleanup = once(cleanup, "SkirmishVisualSpawnService.DestroyVisual(em, entities[i]);", "SourceS004VisualCleanup.DestroyVisual(em, entities[i]);")
    members = [ledger, module.extract_method(spawn, "BuildPrefabEntityLookup"),
               module.extract_method(spawn, "CreateStructure"), cleanup]
    owner = extract_type(transient, module.code_mask(transient), "internal struct SkirmishNativeTransientOwner")
    transient_cleanup = module.extract_method(transient, "DestroySession")
    code = ("using System;using System.Collections.Generic;using Game.Components;using Game.Configs;"
            "using Game.Skirmish.Contracts;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;"
            "namespace Game.Runtime {\n" + owner + "\ninternal static class SkirmishNativeCheckpointTransientState {\n" +
            transient_cleanup + "\n}\ninternal static class SourceS004VisualCleanup {\n"
            "internal static void DestroyVisual(EntityManager em,Entity entity) {"
            "if(em.HasComponent<SkirmishVisualInstanceRecord>(entity)) throw new NotSupportedException(\"Native visual cleanup is not available in the source host\");}\n}\n"
            "public partial struct SkirmishScenarioSpawnSystem {\n" + "\n".join(members) + "\n}\n}\n")
    for name, raw in inputs.items():
        if (repo / name).read_bytes() != raw:
            raise ValueError("S004 ledger input changed during extraction: " + name)
    output.mkdir(parents=True, exist_ok=True)
    generated = output / OUTPUT
    with generated.open("w", encoding="utf-8", newline="\n") as stream: stream.write(code)
    return {"source_pins": {name: hashlib.sha256(raw).hexdigest() for name, raw in inputs.items()},
            "outputs": {OUTPUT: hashlib.sha256(generated.read_bytes()).hexdigest()},
            "scope": "Original ledger and cleanup methods; checked source startup boundary, original actor projection, no native visual cleanup",
            "native_input_claimed": False, "mission_ready": False}
