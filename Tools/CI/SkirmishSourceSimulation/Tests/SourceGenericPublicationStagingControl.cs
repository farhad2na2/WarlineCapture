using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;

// Synthetic evaluator inputs only. No catalog, evidence, launch or mission is created.
internal static class SourceGenericPublicationStagingControl
{
    static void Need(bool value, string message) { if (!value) throw new Exception(message); }

    static SkirmishPublicationEvidence Fixture(string id, bool complete) => new SkirmishPublicationEvidence
    {
        CatalogId = id, DifficultyId = SkirmishDifficultyId.Regular, SizeId = SkirmishSizeId.Standard,
        ManualWin = complete, AriaMatrix = complete, EdgeFixtures = complete, RecoveryEvidence = complete,
        DeviceEvidence = complete, RoleLifecycleEvidence = complete, AircraftCapacityEvidence = complete,
        AssetExists = complete
    };

    static bool Evaluate(string id, bool complete, out SkirmishPublicationStatus status,
        out System.Collections.Generic.List<SkirmishCompileReason> reasons)
    {
        var row = new SkirmishPublicationRowConfig { CatalogId = id };
        var evidence = Fixture(id, complete);
        return SkirmishPublicationValidator.TryEvaluate(row, null, null, in evidence, out status, out reasons);
    }

    static int Main()
    {
        try
        {
            int generic = 0;
            for (int i = 1; i <= 120; i++)
            {
                if (i >= 2 && i <= 5) continue;
                string id = "S" + i.ToString("D3");
                foreach (bool complete in new[] { false, true })
                {
                    Need(Evaluate(id, complete, out var status, out var reasons) &&
                        status == SkirmishPublicationStatus.InProgress &&
                        reasons.Any(x => x.Code == SkirmishReasonCode.MissingReadiness),
                        "Generic candidate must stay InProgress: " + id);
                }
                generic++;
            }
            foreach (string id in new[] { "S000", "S121", "s026", "S26", "S026x", "S-01" })
                Need(!Evaluate(id, false, out var invalid, out _) && invalid != SkirmishPublicationStatus.Playable,
                    "Invalid identity accepted: " + id);
            var mismatchedRow = new SkirmishPublicationRowConfig { CatalogId = "S026", DefinitionId = "skirmish.other" };
            var mismatchedEvidence = Fixture("S026", false); mismatchedEvidence.DefinitionId = "skirmish.s026";
            Need(!SkirmishPublicationValidator.TryEvaluate(mismatchedRow, null, null, in mismatchedEvidence,
                out var mismatchStatus, out var mismatchReasons) && mismatchStatus != SkirmishPublicationStatus.Playable &&
                mismatchReasons.Any(x => x.Code == SkirmishReasonCode.MatrixMismatch), "Mismatched generic identity accepted");
            var unsupportedSize = Fixture("S026", false); unsupportedSize.SizeId = SkirmishSizeId.War;
            var sizeRow = new SkirmishPublicationRowConfig { CatalogId = "S026" };
            Need(!SkirmishPublicationValidator.TryEvaluate(sizeRow, null, null, in unsupportedSize,
                out var sizeStatus, out _) && sizeStatus != SkirmishPublicationStatus.Playable,
                "Unsupported first-visit size accepted");
            foreach (string id in new[] { "S002", "S003", "S004", "S005" })
                Need(Evaluate(id, false, out var incomplete, out _) && incomplete == SkirmishPublicationStatus.InProgress,
                    "Existing incomplete-evidence policy changed: " + id);
            Need(Evaluate("S002", true, out var s002, out _) && s002 == SkirmishPublicationStatus.Playable,
                "Existing S002 synthetic policy changed");
            foreach (string id in new[] { "S003", "S004" })
                Need(Evaluate(id, true, out var guarded, out _) && guarded == SkirmishPublicationStatus.InProgress,
                    "Existing air publication guard changed: " + id);
            Need(Evaluate("S005", true, out var s005, out _) && s005 == SkirmishPublicationStatus.Playable,
                "Existing S005 synthetic policy changed");
            var incompleteCombined = Fixture("S005", true); incompleteCombined.RoleLifecycleEvidence = false;
            var combinedRow = new SkirmishPublicationRowConfig { CatalogId = "S005" };
            Need(SkirmishPublicationValidator.TryEvaluate(combinedRow, null, null, in incompleteCombined,
                out var combinedStatus, out _) && combinedStatus == SkirmishPublicationStatus.InProgress,
                "Existing S005 role lifecycle guard changed");
            var hypothetical = Fixture("S026", true);
            hypothetical.DefinitionId = "skirmish.s026"; hypothetical.ContentHash = "fixture-only"; hypothetical.SetupHash = 1;
            var flip = new SkirmishPlayableFlipRequest { CatalogId = "S026", DefinitionId = hypothetical.DefinitionId,
                ContentHash = hypothetical.ContentHash, SetupHash = hypothetical.SetupHash,
                DifficultyId = hypothetical.DifficultyId, SizeId = hypothetical.SizeId,
                EvidenceDirectory = "fixture-only", RequiredRelativeFiles = new[] { "fixture-only" }, Acceptance = hypothetical };
            var genericRow = new SkirmishPublicationRowConfig { CatalogId = "S026", DefinitionId = hypothetical.DefinitionId };
            Need(SkirmishPublicationValidator.TryEvaluatePlayableFlip(genericRow, null, null, in flip, _ => true,
                out var flipStatus, out var flipReasons) && flipStatus == SkirmishPublicationStatus.InProgress &&
                flipReasons.Any(x => x.Code == SkirmishReasonCode.MissingReadiness),
                "Synthetic generic flip must not become Playable");
            Console.WriteLine("[GenericPublicationStagingControl] result=Passed generic=" + generic +
                " flipGuard=1 fixturesOnly=1 catalogWrites=0 actualReadiness=0 mission=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
