"""Generate a thin S073 host adapter over the shared original scalar predicate."""
from pathlib import Path
import hashlib


def generate(repo, output):
    root = Path(repo)
    source = root / "Assets/Game/Scripts/Systems/GameplayStartCompletionPredicate.cs"
    raw = source.read_bytes()
    code = ("using Game.Components;using Unity.Entities;namespace Game.Runtime{\n"
            "internal static class SourceS073LoadingGate{\n"
            "// Null means the optional native helper is absent. Source-host unknown facts must be false.\n"
            "internal static bool Evaluate(EntityManager em,bool gameplayInitialized,bool playRequested,"
            "bool buildingStartupComplete,bool? citySpawned,bool? blockersSpawned,bool? decorationsSpawned)\n"
            "{bool cityReady=!citySpawned.HasValue||citySpawned.Value;"
            "bool blockersReady=!blockersSpawned.HasValue||blockersSpawned.Value;"
            "bool decorationsReady=!decorationsSpawned.HasValue||decorationsSpawned.Value;"
            "if(!GameplayStartCompletionPredicate.GameplayStartRequested(gameplayInitialized,playRequested))return false;"
            "if(!GameplayStartCompletionPredicate.BuildingStartupComplete(buildingStartupComplete))return false;"
            "if(!GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(citySpawned.HasValue,cityReady))return false;"
            "if(!GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(blockersSpawned.HasValue,blockersReady))return false;"
            "if(!GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(decorationsSpawned.HasValue,decorationsReady))return false;"
            "if(!GameplayStartCompletionPredicate.RuntimeWorldAvailable(em!=null&&em.World!=null,em!=null&&em.World!=null&&em.World.IsCreated))return false;"
            "using var configs=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));"
            "using var initialized=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig),typeof(InitialUnitsSpawnInitialized));"
            "return GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(configs.CalculateEntityCount(),initialized.CalculateEntityCount());}\n}\n}\n")
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    (out / "SourceS073LoadingGate.cs").write_text(code)
    if source.read_bytes() != raw:
        raise ValueError("Shared loading-gate predicate changed during adapter generation")
    return {"kind": "S073OriginalLoadingGateProjection", "source_sha256": hashlib.sha256(raw).hexdigest(),
            "scope": "Shared scalar predicate over explicit facts; no automatic admission or scene claim"}
