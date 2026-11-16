import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
RECIPE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/generate_player_placement.py"
SPEC = importlib.util.spec_from_file_location("player_placement_recipe", RECIPE)
RECIPE_MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RECIPE_MODULE)


HARNESS = r'''
global using UnityEngine;
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using UnityEngine;

namespace Game.Components
{
    internal enum FactionConstructionResourceMutationResult : byte
    {
        Applied, InsufficientCredits, InsufficientMaterials,
        InsufficientCreditsAndMaterials, DuplicateTransaction, InvalidState
    }
}

namespace UnityEngine
{
    internal struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }
    internal static class Mathf { internal static int Max(int a, int b) => a > b ? a : b; }
}

namespace Game.Runtime
{
    internal sealed class RuntimeDefinition
    {
        internal int CreditsCost, MaterialsCost;
        internal RuntimePrefab Prefab;
    }
    internal sealed class RuntimePrefab { internal string name; }
    internal sealed class RuntimeBuildingEntity { }
    internal static class BuildingPlacementInitialPreviewState
    { internal static int HoverAxis(Vector2Int start, Vector2Int origin) => 0; }
    internal static class SkirmishConstructionLedger
    {
        internal static object Observer;
        internal static int UnsupportedCount;
        internal static void UnsupportedPlacement() { UnsupportedCount++; }
    }
    internal sealed class SourcePlayerConstructionReservation
    {
        private readonly List<string> events;
        internal FactionConstructionResourceMutationResult ReserveResult = FactionConstructionResourceMutationResult.Applied;
        internal FactionConstructionResourceMutationResult FinalizeResult = FactionConstructionResourceMutationResult.Applied;
        internal FactionConstructionResourceMutationResult RollbackResult = FactionConstructionResourceMutationResult.Applied;
        internal int ObservedRequests, ObservedOutcomes;
        internal SourcePlayerConstructionReservation(List<string> events) { this.events = events; }
        internal FactionConstructionResourceMutationResult TryReserve(int id, int credits, int materials)
        { events.Add($"reserve:{id}:{credits}:{materials}"); return ReserveResult; }
        internal FactionConstructionResourceMutationResult TryFinalize(int id)
        { events.Add("finalize:" + id); return FinalizeResult; }
        internal FactionConstructionResourceMutationResult TryRollback(int id)
        { events.Add("rollback:" + id); return RollbackResult; }
        internal void ObservePlacement(SourceHostPlacementLifecycle.PlacementState placement, int id,
            SourceHostPlacementCommit.CommitOutcome? outcome = null)
        {
            if (outcome.HasValue) { ObservedOutcomes++; events.Add("observe:outcome:" + id); }
            else { ObservedRequests++; events.Add("observe:request:" + id); }
        }
    }
}

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }

    private static SourceHostPlacementLifecycle.PlacementState Placement(bool valid = true)
    {
        return new SourceHostPlacementLifecycle.PlacementState
        {
            IsValid = valid,
            OriginCell = new Vector2Int(7, 9),
            CommittedOriginCell = new Vector2Int(1, 1),
            Definition = new RuntimeDefinition { CreditsCost = 2, MaterialsCost = 3 }
        };
    }

    private static SourceHostPlacementLifecycle.ConfirmContext Context(
        List<string> events,
        SourcePlayerConstructionReservation reservation,
        Func<SourceHostPlacementLifecycle.PlacementState, bool> validate,
        Func<SourceHostPlacementLifecycle.PlacementState, SourceHostPlacementCommit.CommitOutcome> commit)
    {
        return new SourceHostPlacementLifecycle.ConfirmContext
        {
            ValidateConfirm = p => { events.Add("validate"); return validate == null || validate(p); },
            TryReserveCost = reservation.TryReserve,
            FinalizeCost = reservation.TryFinalize,
            RollbackCost = reservation.TryRollback,
            CommitPlacement = commit == null ? null : new SourceHostPlacementLifecycle.CommitPlacementDelegate(p =>
            { events.Add("commit"); return commit(p); })
        };
    }

    private static string Events(List<string> events) => string.Join(",", events);

    private static void Main()
    {
        SkirmishConstructionLedger.Observer = new object();
        var events = new List<string>();
        var reservation = new SourcePlayerConstructionReservation(events);
        var placement = Placement();
        var lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = placement };
        var context = Context(events, reservation, p =>
        {
            p.Definition.CreditsCost = 13;
            p.Definition.MaterialsCost = 8;
            return true;
        }, p => new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity(), 1, 1));
        Check(lifecycle.Confirm(71, context, out var reason), "valid player placement should succeed");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.None, "success reason changed");
        Check(placement.CommittedOriginCell.x == 7 && placement.CommittedOriginCell.y == 9,
            "commit did not use currently validated pose");
        Check(Events(events) == "validate,reserve:71:13:8,observe:request:71,commit,observe:outcome:71,finalize:71",
            "player callback/observer order or post-validation costs changed: " + Events(events));
        Check(reservation.ObservedRequests == 1 && reservation.ObservedOutcomes == 1,
            "player observer did not see reserve and commit outcome");

        SkirmishConstructionLedger.Observer = null;
        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events);
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement(false) };
        Check(!lifecycle.Confirm(72, Context(events, reservation, null, null), out reason), "blocked placement accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.BlockedPlacement && events.Count == 0,
            "blocked placement executed callbacks");

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events);
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        Check(!lifecycle.Confirm(73, Context(events, reservation, _ => false, null), out reason), "invalid placement accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.InvalidPlacement && Events(events) == "validate",
            "invalid placement proceeded to reservation: " + Events(events));

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events) { ReserveResult = FactionConstructionResourceMutationResult.InsufficientMaterials };
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        Check(!lifecycle.Confirm(74, Context(events, reservation, null, null), out reason), "unaffordable placement accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.InsufficientMaterials,
            "resource failure mapping changed");
        Check(Events(events) == "validate,reserve:74:2:3", "commit ran after reserve rejection: " + Events(events));

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events);
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        Check(!lifecycle.Confirm(75, Context(events, reservation, null,
            _ => new SourceHostPlacementCommit.CommitOutcome(null, 0, 1)), out reason), "zero commit accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.RegistrationFailed,
            "zero commit failure mapping changed");
        Check(Events(events) == "validate,reserve:75:2:3,commit,rollback:75",
            "zero commit did not roll back: " + Events(events));

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events);
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        Check(!lifecycle.Confirm(76, Context(events, reservation, null,
            _ => new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity(), 1, 2)), out reason), "partial commit accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.RegistrationFailed,
            "partial commit failure mapping changed");
        Check(Events(events) == "validate,reserve:76:2:3,commit,finalize:76",
            "partial commit must finalize: " + Events(events));

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events) { FinalizeResult = FactionConstructionResourceMutationResult.InvalidState };
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        Check(!lifecycle.Confirm(77, Context(events, reservation, null,
            _ => new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity(), 1, 1)), out reason),
            "finalization failure accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.TransactionRejected,
            "finalization failure mapping changed");
        Check(!events.Contains("rollback:77"), "finalized full commit unexpectedly rolled back");

        events.Clear();
        reservation = new SourcePlayerConstructionReservation(events);
        lifecycle = new SourceHostPlacementLifecycle();
        Check(!lifecycle.Confirm(78, Context(events, reservation, null, null), out reason), "missing placement accepted");
        Check(reason == SourceHostPlacementLifecycle.ConfirmFailureReason.MissingActivePlacement && events.Count == 0,
            "missing placement executed callbacks");

        SkirmishConstructionLedger.Observer = new object();
        SkirmishConstructionLedger.UnsupportedCount = 0;
        events.Clear();
        reservation = null;
        lifecycle = new SourceHostPlacementLifecycle { ActivePlacement = Placement() };
        var nonPlayerOwner = new ReserveOnly(events);
        var unsupportedContext = new SourceHostPlacementLifecycle.ConfirmContext
        {
            ValidateConfirm = _ => true,
            TryReserveCost = nonPlayerOwner.Reserve,
            FinalizeCost = _ => FactionConstructionResourceMutationResult.Applied,
            CommitPlacement = _ => new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity(), 1, 1)
        };
        Check(lifecycle.Confirm(79, unsupportedContext, out reason), "observer fixture placement failed");
        Check(SkirmishConstructionLedger.UnsupportedCount == 2,
            "unsupported logical owner should be marked at reserve and commit observations");

        Console.WriteLine($"[SourceHostPlacementTransaction] checks={checks} result=Passed");
    }

    private sealed class ReserveOnly
    {
        private readonly List<string> events;
        internal ReserveOnly(List<string> events) { this.events = events; }
        internal FactionConstructionResourceMutationResult Reserve(int id, int credits, int materials)
        { events.Add("reserve:" + id); return FactionConstructionResourceMutationResult.Applied; }
    }
}
'''


class GeneratedSourceHostPlacementTransactionTest(unittest.TestCase):
    def test_generated_transaction_runs_against_real_source_host_lifecycle(self):
        with tempfile.TemporaryDirectory() as temp:
            temp_path = Path(temp)
            generated = temp_path / "generated"
            RECIPE_MODULE.generate(ROOT, generated)
            (temp_path / "Program.cs").write_text(HARNESS)
            (temp_path / "Harness.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk">\n'
                '  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>\n'
                f'  <ItemGroup><Compile Include="{temp_path / "Program.cs"}" Link="Program.cs" />'
                f'<Compile Include="{generated / "PlacementTransaction.cs"}" Link="PlacementTransaction.cs" />'
                f'<Compile Include="{generated / "PlacementBindings.cs"}" Link="PlacementBindings.cs" /></ItemGroup>\n'
                '</Project>\n'
            )
            (temp_path / "NuGet.Config").write_text(
                '<configuration><packageSources><clear /></packageSources></configuration>\n'
            )
            subprocess.run(
                ["dotnet", "restore", str(temp_path / "Harness.csproj"), "--configfile", str(temp_path / "NuGet.Config"), "--nologo", "-v:q"],
                check=True, capture_output=True, text=True, timeout=120,
            )
            result = subprocess.run(
                ["dotnet", "run", "--project", str(temp_path / "Harness.csproj"), "--configuration", "Release", "--no-restore", "--nologo"],
                check=False, capture_output=True, text=True, timeout=120,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("[SourceHostPlacementTransaction] checks=25 result=Passed", result.stdout)


if __name__ == "__main__":
    unittest.main()
