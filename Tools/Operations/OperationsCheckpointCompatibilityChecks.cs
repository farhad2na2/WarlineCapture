using System;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

public static class OperationsCheckpointCompatibilityChecks
{
    public const string PassMarker = "[OperationsCheckpointScope] result=Passed legacy-full,current-intro,scope-tamper,result-scope-conflict";
    public static void Run()
    {
        if (!OperationsAuthoredMissions.TryCompile("operation.o001", out var definition, out string hash, out string error))
            throw new InvalidOperationException(error);
        var launch = new OperationsLaunchPayload(OperationsIdentityRules.CurrentSchemaVersion,
            "run.operations.abcd1234", "district.operations.d01", "offer.operations.abcd1234", "operation.o001",
            "scenario.operations.o001", "opmap.operations.old_quarter", 1, 1, hash,
            OperationsDifficultyKind.Regular, 1102, "txn.operations.abcd1234", "session.operations.abcd1234",
            0, "snapshot.baseline", false);
        var mission = new OperationsTacticalSession(definition, launch);
        string legacy = OperationsCheckpointCodec.Write(mission, definition, Array.Empty<OperationsLoopOrder>(),
            launch.SessionId, hash, 0, OperationsContentScope.Full, 1);
        Require(legacy.StartsWith("schema=1\nsession=") && !legacy.Contains("\nscope="), "legacy format preserved");
        Require(OperationsCheckpointCodec.TryRead(legacy, out var old, out _) && old.SchemaVersion == 1 &&
            old.ScopeId == OperationsContentScope.Full, "legacy full checkpoint");
        string intro = OperationsCheckpointCodec.Write(mission, definition, Array.Empty<OperationsLoopOrder>(),
            launch.SessionId, hash, 0, OperationsContentScope.Intro);
        Require(OperationsCheckpointCodec.TryRead(intro, out var current, out _) && current.SchemaVersion == 2 &&
            current.ScopeId == OperationsContentScope.Intro, "current intro checkpoint");
        Require(!OperationsCheckpointCodec.TryRead(intro.Replace(OperationsContentScope.Intro, OperationsContentScope.Full),
            out _, out _), "scope is checksum bound");
        foreach (string scope in new[] { OperationsContentScope.Intro, OperationsContentScope.Full })
        {
            var loop = OperationsLoopSession.Create(9444, null, null, scope);
            string wrong = scope == OperationsContentScope.Intro ? OperationsContentScope.Full : OperationsContentScope.Intro;
            var mismatch = new OperationsMissionResult(OperationsIdentityRules.CurrentSchemaVersion,
                launch.RunId, launch.OfferId, launch.MissionId, launch.SessionId, 0, 1,
                OperationsOutcomeKind.Victory, "fixture", 0, null, null, 0, null, null, null, 0, 0, "scope-fixture", wrong);
            var rejected = loop.ProbeSettlement("cmd.operations.ffff1111", mismatch);
            Require(!rejected.Accepted && rejected.ReasonCode == OperationsReasonCode.Conflict &&
                loop.ActionPoints == 3 && loop.Credits == 0 && loop.CommanderXp == 0, "cross-scope result rejected without tactical/account mutation");
        }
        Console.WriteLine(PassMarker);
    }
    static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
