#!/usr/bin/env python3
"""Fault-inject the exact actor-creation exception boundary from the runtime owner."""

from __future__ import annotations

import pathlib
import shutil
import subprocess
import tempfile


ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs"
METHOD_MARKER = "        private static void RunActorCreation("
LAMBDA_MARKER = "                    CreateActorsAndFoundation = (buildingId, pathBlocking) =>"


def extract_braced(text: str, marker: str) -> str:
    start = text.index(marker)
    brace = text.index("{", start)
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace:index + 1]
    raise ValueError(f"Incomplete source block: {marker}")


def extract_cleanup_methods(text: str) -> str:
    start = text.index(METHOD_MARKER)
    end = text.index("        public bool RollbackRuntimeBuildingRegistration(", start)
    return text[start:end]


def main() -> int:
    source = SOURCE.read_text()
    methods = extract_cleanup_methods(source)
    lambda_body = extract_braced(source, LAMBDA_MARKER)
    lambda_expression = "Action<int, bool> actualCreateActorsAndFoundation = (buildingId, pathBlocking) => " + lambda_body + ";"
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise RuntimeError("dotnet is required")
    with tempfile.TemporaryDirectory(prefix="warline-actor-cleanup-") as temporary:
        project = pathlib.Path(temporary)
        (project / "ActorCleanup.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
            "<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"
            "<ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>"
            "</PropertyGroup></Project>\n"
        )
        (project / "NuGet.Config").write_text(
            "<configuration><packageSources><clear /></packageSources></configuration>\n"
        )
        harness = r'''using System;
using System.Collections.Generic;

struct Entity
{
    public static readonly Entity Null = new Entity(-1);
    public int Index;
    public Entity(int index) { Index = index; }
}
struct Vector2Int { public int x; public int y; }
struct Quaternion { }
struct RuntimeActorBirthContext { }
sealed class BuildingDefinition { public Vector2Int FootprintCells; }
sealed class TransformStub { public Quaternion rotation; }
sealed class InstanceStub { public TransformStub transform = new(); }
sealed class EntityManager { }
sealed class SurfaceResult { }
sealed class SurfacePlacement { }
class FoundationStub
{
    public virtual void ApplyCombatEntityFoundation(EntityManager manager, Entity entity, SurfaceResult result, SurfacePlacement placement) { }
}
delegate Entity OwnedBlockerCreator(BuildingDefinition definition, Vector2Int origin, Vector2Int footprint, RuntimeActorBirthContext birth, out Action cleanup);
delegate Entity OwnedCombatCreator(int id, Vector2Int origin, BuildingDefinition definition, byte faction, Quaternion rotation, RuntimeActorBirthContext birth, out Action cleanup);
delegate Entity LegacyBlockerCreator(BuildingDefinition definition, Vector2Int origin, Vector2Int footprint, RuntimeActorBirthContext birth);
delegate Entity LegacyCombatCreator(int id, Vector2Int origin, BuildingDefinition definition, byte faction, Quaternion rotation, RuntimeActorBirthContext birth);
sealed class ContextStub
{
    public OwnedBlockerCreator CreateBlockerEntityWithCleanup;
    public OwnedCombatCreator CreateCombatEntityWithCleanup;
    public LegacyBlockerCreator CreateBlockerEntity;
    public LegacyCombatCreator CreateCombatEntity;
}

static class Harness
{
METHODS
    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    static int Main()
    {
        CaseCombatFactoryFailure();
        CaseFoundationFailureCleansOwnedPair();
        CaseCleanupFailurePreservesOriginal();
        CaseLegacyCallbacksFailClosed();
        Console.WriteLine("[BuildingRuntimeActorCleanup] result=Passed cases=4 actual-owner-lambda=true");
        return 0;
    }

    static Action<int, bool> BuildOwner(ContextStub context,
        bool hasEntityManager, bool hasSurfaceResult, bool preserveAuthoredTransform,
        FoundationStub FoundationVisuals, EntityManager entityManager, SurfaceResult surfaceResult,
        SurfacePlacement _surfacePlacementSystem, InstanceStub instance)
    {
        Entity blockerEntity = Entity.Null;
        Entity combatEntity = Entity.Null;
        BuildingDefinition definition = new();
        Vector2Int originCell = default;
        RuntimeActorBirthContext birth = default;
        ACTION
        return actualCreateActorsAndFoundation;
    }

    static void CaseCombatFactoryFailure()
    {
        var ownerWorld = new List<int> { 41 };
        var otherWorld = new List<int> { 41 };
        var expected = new InvalidOperationException("combat creation failed");
        var context = new ContextStub
        {
            CreateBlockerEntityWithCleanup = (BuildingDefinition d, Vector2Int o, Vector2Int f, RuntimeActorBirthContext b, out Action cleanup) =>
            { cleanup = () => ownerWorld.Remove(41); return new Entity(41); },
            CreateCombatEntityWithCleanup = (int id, Vector2Int o, BuildingDefinition d, byte faction, Quaternion rotation, RuntimeActorBirthContext b, out Action cleanup) =>
            { cleanup = null; throw expected; }
        };
        var invoke = BuildOwner(context, false, false, false, null, null, null, null, new InstanceStub());
        Exception observed = null;
        try { invoke(1, true); } catch (Exception error) { observed = error; }
        Require(ReferenceEquals(observed, expected), "factory exception was not preserved");
        Require(ownerWorld.Count == 0 && otherWorld.Count == 1, "cleanup did not remain bound to the creator world");
    }

    static void CaseFoundationFailureCleansOwnedPair()
    {
        var order = new List<string>();
        var expected = new InvalidOperationException("foundation failed");
        var context = new ContextStub
        {
            CreateBlockerEntityWithCleanup = (BuildingDefinition d, Vector2Int o, Vector2Int f, RuntimeActorBirthContext b, out Action cleanup) =>
            { order.Add("blocker-created"); cleanup = () => order.Add("blocker-cleaned"); return new Entity(3); },
            CreateCombatEntityWithCleanup = (int id, Vector2Int o, BuildingDefinition d, byte faction, Quaternion rotation, RuntimeActorBirthContext b, out Action cleanup) =>
            { order.Add("combat-created"); cleanup = () => order.Add("combat-cleaned"); return new Entity(4); }
        };
        var invoke = BuildOwner(context, true, true, false,
            new ThrowingFoundation(expected), new EntityManager(), new SurfaceResult(), new SurfacePlacement(), new InstanceStub());
        Exception observed = null;
        try { invoke(2, true); } catch (Exception error) { observed = error; }
        Require(ReferenceEquals(observed, expected), "foundation exception was replaced");
        Require(string.Join(",", order) == "blocker-created,combat-created,combat-cleaned,blocker-cleaned",
            "owner lambda did not clean both actors in reverse creation order");
    }

    static void CaseCleanupFailurePreservesOriginal()
    {
        var order = new List<string>();
        var expected = new InvalidOperationException("foundation failed");
        var cleanupFailure = new InvalidOperationException("combat cleanup failed");
        var context = new ContextStub
        {
            CreateBlockerEntityWithCleanup = (BuildingDefinition d, Vector2Int o, Vector2Int f, RuntimeActorBirthContext b, out Action cleanup) =>
            { cleanup = () => order.Add("blocker-cleaned"); return new Entity(3); },
            CreateCombatEntityWithCleanup = (int id, Vector2Int o, BuildingDefinition d, byte faction, Quaternion rotation, RuntimeActorBirthContext b, out Action cleanup) =>
            { cleanup = () => throw cleanupFailure; return new Entity(4); }
        };
        var invoke = BuildOwner(context, true, true, false,
            new ThrowingFoundation(expected), new EntityManager(), new SurfaceResult(), new SurfacePlacement(), new InstanceStub());
        Exception observed = null;
        try { invoke(3, true); } catch (Exception error) { observed = error; }
        Require(ReferenceEquals(observed, expected) && string.Join(",", order) == "blocker-cleaned",
            "cleanup failure replaced the original or prevented remaining cleanup");
    }

    static void CaseLegacyCallbacksFailClosed()
    {
        var order = new List<string>();
        var expected = new InvalidOperationException("foundation failed");
        var context = new ContextStub
        {
            CreateBlockerEntity = (d, o, f, b) => new Entity(9),
            CreateCombatEntity = (id, o, d, faction, rotation, b) => new Entity(10)
        };
        var invoke = BuildOwner(context, true, true, false,
            new ThrowingFoundation(expected), new EntityManager(), new SurfaceResult(), new SurfacePlacement(), new InstanceStub());
        Exception observed = null;
        try { invoke(4, true); } catch (Exception error) { observed = error; }
        Require(ReferenceEquals(observed, expected) && order.Count == 0,
            "legacy callback results without proven ownership were cleaned");
    }
}
sealed class ThrowingFoundation : FoundationStub
{
    private readonly Exception _failure;
    public ThrowingFoundation(Exception failure) { _failure = failure; }
    public override void ApplyCombatEntityFoundation(EntityManager manager, Entity entity, SurfaceResult result, SurfacePlacement placement) => throw _failure;
}
'''.replace("METHODS", methods).replace("ACTION", lambda_expression)
        (project / "Program.cs").write_text(harness)
        result = subprocess.run(
            [dotnet, "run", "--project", str(project / "ActorCleanup.csproj"), "--configuration", "Release", "--nologo", "--configfile", str(project / "NuGet.Config")],
            cwd=ROOT, text=True, capture_output=True, timeout=120,
        )
        output = result.stdout + result.stderr
        print(output, end="")
        return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
