from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
KERNEL = ROOT / "Assets/Game/Scripts/Simulation/ConstructionTransactionKernel.cs"


HARNESS = r'''
using System;
using System.Collections.Generic;
using Game.Simulation;

internal sealed class Host : IConstructionTransactionHost<string>
{
    public readonly List<string> Calls = new List<string>();
    public bool Valid = true;
    public int Credits = 3;
    public int Materials = 5;
    public int CreditsAfterValidation = 3;
    public int MaterialsAfterValidation = 5;
    public ConstructionMutationResult ReserveResult = ConstructionMutationResult.Applied;
    public ConstructionMutationResult FinalizeResult = ConstructionMutationResult.Applied;
    public ConstructionMutationResult RollbackResult = ConstructionMutationResult.Applied;
    public ConstructionCommitResult CommitResult = new ConstructionCommitResult(1, 1);

    public bool Validate(string placement)
    { Calls.Add("validate:" + placement); Credits = CreditsAfterValidation; Materials = MaterialsAfterValidation; return Valid; }
    public void GetCosts(string placement, out int credits, out int materials)
    { Calls.Add("costs:" + placement); credits = Credits; materials = Materials; }
    public ConstructionMutationResult TryReserve(int id, int credits, int materials)
    { Calls.Add($"reserve:{id}:{credits}:{materials}"); return ReserveResult; }
    public ConstructionCommitResult Commit(string placement)
    { Calls.Add("commit:" + placement); return CommitResult; }
    public ConstructionMutationResult TryFinalize(int id)
    { Calls.Add("finalize:" + id); return FinalizeResult; }
    public ConstructionMutationResult TryRollback(int id)
    { Calls.Add("rollback:" + id); return RollbackResult; }
}

internal static class Program
{
    private const int Id = 41;
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static ConstructionTransactionFailure Run(Host host, bool hasPlacement = true, bool isValid = true)
    {
        host.Credits = 3;
        host.Materials = 5;
        return ConstructionTransactionKernel.Execute<string, Host>(hasPlacement, isValid, "site-A", Id,
            ref host);
    }

    private static string Calls(Host host) => string.Join(",", host.Calls);

    private static void Main()
    {
        var success = new Host();
        success.CreditsAfterValidation = -4;
        Check(Run(success) == ConstructionTransactionFailure.None, "full commit should succeed");
        Check(Calls(success) == "validate:site-A,costs:site-A,reserve:41:0:5,commit:site-A,finalize:41",
            "success order and cost clamp changed: " + Calls(success));

        var mutatedCosts = new Host { Credits = 1, Materials = 1, CreditsAfterValidation = 9, MaterialsAfterValidation = 11 };
        Check(Run(mutatedCosts) == ConstructionTransactionFailure.None, "post-validation cost mutation should succeed");
        Check(Calls(mutatedCosts) == "validate:site-A,costs:site-A,reserve:41:9:11,commit:site-A,finalize:41",
            "costs must be read after confirm validation: " + Calls(mutatedCosts));

        var rejectedReserve = new Host { ReserveResult = ConstructionMutationResult.InsufficientCredits };
        Check(Run(rejectedReserve) == ConstructionTransactionFailure.InsufficientCredits,
            "reserve rejection mapping changed");
        Check(Calls(rejectedReserve) == "validate:site-A,costs:site-A,reserve:41:3:5",
            "commit ran after rejected reserve: " + Calls(rejectedReserve));

        var invalid = new Host { Valid = false };
        Check(Run(invalid) == ConstructionTransactionFailure.InvalidPlacement, "validation rejection changed");
        Check(Calls(invalid) == "validate:site-A", "cost or reserve ran after validation rejection");

        var partial = new Host { CommitResult = new ConstructionCommitResult(1, 2) };
        Check(Run(partial) == ConstructionTransactionFailure.RegistrationFailed,
            "partial commit failure changed");
        Check(Calls(partial) == "validate:site-A,costs:site-A,reserve:41:3:5,commit:site-A,finalize:41",
            "partial commit must finalize: " + Calls(partial));

        var zero = new Host { CommitResult = new ConstructionCommitResult(0, 1) };
        Check(Run(zero) == ConstructionTransactionFailure.RegistrationFailed, "zero commit failure changed");
        Check(Calls(zero) == "validate:site-A,costs:site-A,reserve:41:3:5,commit:site-A,rollback:41",
            "zero commit must roll back: " + Calls(zero));

        var partialSettlementFailure = new Host {
            CommitResult = new ConstructionCommitResult(1, 2),
            FinalizeResult = ConstructionMutationResult.Rejected
        };
        Check(Run(partialSettlementFailure) == ConstructionTransactionFailure.TransactionRejected,
            "partial commit settlement failure mapping changed");

        var rollbackFailure = new Host {
            CommitResult = new ConstructionCommitResult(0, 1),
            RollbackResult = ConstructionMutationResult.Rejected
        };
        Check(Run(rollbackFailure) == ConstructionTransactionFailure.TransactionRejected,
            "rollback failure mapping changed");

        var finalizationFailure = new Host { FinalizeResult = ConstructionMutationResult.Rejected };
        Check(Run(finalizationFailure) == ConstructionTransactionFailure.TransactionRejected,
            "full commit finalization failure mapping changed");
        Check(!finalizationFailure.Calls.Contains("rollback:41"),
            "full commit finalization failure unexpectedly rolled back");

        var missing = new Host();
        Check(Run(missing, hasPlacement: false) == ConstructionTransactionFailure.MissingPlacement,
            "missing placement result changed");
        Check(Calls(missing) == string.Empty, "host invoked for missing placement");

        var blocked = new Host();
        Check(Run(blocked, isValid: false) == ConstructionTransactionFailure.BlockedPlacement,
            "blocked placement result changed");
        Check(Calls(blocked) == string.Empty, "host invoked for blocked placement");

        Console.WriteLine($"[ConstructionTransactionKernel] checks={checks} result=Passed");
    }
}
'''


class ConstructionTransactionKernelTest(unittest.TestCase):
    def test_exact_kernel_source_compiles_and_preserves_order(self):
        with tempfile.TemporaryDirectory() as temp:
            project = Path(temp)
            (project / "Program.cs").write_text(HARNESS)
            (project / "Harness.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk">\n'
                '  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>'
                '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>\n'
                f'  <ItemGroup><Compile Include="{KERNEL}" Link="ConstructionTransactionKernel.cs" /></ItemGroup>\n'
                '</Project>\n'
            )
            (project / "NuGet.Config").write_text(
                '<configuration><packageSources><clear /></packageSources></configuration>\n'
            )
            subprocess.run(
                ["dotnet", "restore", str(project / "Harness.csproj"), "--configfile", str(project / "NuGet.Config"), "--nologo", "-v:q"],
                check=True,
                capture_output=True,
                text=True,
                timeout=120,
            )
            result = subprocess.run(
                ["dotnet", "run", "--project", str(project / "Harness.csproj"), "--configuration", "Release", "--no-restore", "--nologo"],
                check=True,
                capture_output=True,
                text=True,
                timeout=120,
            )
            self.assertIn("[ConstructionTransactionKernel] checks=20 result=Passed", result.stdout)


if __name__ == "__main__":
    unittest.main()
