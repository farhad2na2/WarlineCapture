using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Editor;
using Game.Operations.Contracts;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class MainMenuModeMedalPersistenceTests
{
    private string root;private SaveService saves;private OperationsProfileCommandService commands;
    [SetUp]public void Setup()
    {
        root=Path.Combine(Path.GetTempPath(),"warline-home-medal-"+Guid.NewGuid().ToString("N"));
        saves=new SaveService(new JsonSaveRepository(root));commands=new OperationsProfileCommandService(saves);
    }
    [TearDown]public void Cleanup(){if(Directory.Exists(root))Directory.Delete(root,true);}
    [Test]public void VictoryMedalPersistsWithResultAcrossRestartAndRetry()
    {
        var result=ReservedResult(OperationsOutcomeKind.Victory);
        var state=commands.Read();var command=new OperationsCommand("cmd.operations.homesettle",state.profileRevision,OperationsCommandKind.Conclude,"","","");
        Assert.That(commands.TrySettle(command,result,"result",out var committed,out string error),Is.True,error);
        Assert.That(committed.Accepted,Is.True,committed.ReasonCode.ToString());
        var after=new SaveService(new JsonSaveRepository(root)).LoadProfile();
        Assert.That(after.operations.firstClearRewardIds,Does.Contain("operation.o001"));
        Assert.That(after.operationsMissionMedals.Length,Is.EqualTo(1));
        Assert.That(after.operationsMissionMedals[0].difficulty,Is.EqualTo((byte)OperationsDifficultyKind.Regular));
        string bytes=File.ReadAllText(Path.Combine(root,SaveService.ProfileFileName));
        Assert.That(commands.TrySettle(command,result,"result",out var retried,out error),Is.True,error);
        Assert.That(retried.Accepted,Is.True);
        Assert.That(File.ReadAllText(Path.Combine(root,SaveService.ProfileFileName)),Is.EqualTo(bytes));
    }
    [Test]public void PartialOutcomeCannotAwardDifficultyMedal()
    {
        var result=ReservedResult(OperationsOutcomeKind.Partial);var state=commands.Read();
        Assert.That(commands.TrySettle(new OperationsCommand("cmd.operations.homepartial",state.profileRevision,OperationsCommandKind.Conclude,"","",""),
            result,"partial",out var committed,out string error),Is.True,error);
        Assert.That(committed.Accepted,Is.True);
        Assert.That(saves.LoadProfile().operationsMissionMedals,Is.Empty);
    }
    [Test]public void LegacyClearDoesNotInventDifficultyMedal()
    {
        var profile=saves.LoadProfile();profile.operations.firstClearRewardIds=new[]{"operation.o001"};saves.SaveProfile(profile);
        var loaded=new SaveService(new JsonSaveRepository(root)).LoadProfile();
        Assert.That(loaded.operations.firstClearRewardIds,Has.Length.EqualTo(1));
        Assert.That(loaded.operationsMissionMedals??Array.Empty<OperationsMissionMedalSaveData>(),Is.Empty);
    }
    [Test]public void StaleAtomicWriterCannotPublishMedal()
    {
        var profile=saves.LoadProfile();saves.SaveProfile(profile);
        var transaction=new OperationsSaveData{profileRevision=1,firstClearRewardIds=new[]{"operation.o001"}};
        var medal=new OperationsMissionMedalSaveData{missionId="operation.o001",difficulty=1,sessionId="fixture"};
        Assert.That(saves.TryCommitOperations(0,0,transaction,"result",out _,out var reason,medal),Is.False);
        Assert.That(reason,Is.EqualTo("stale_profile"));
        Assert.That(saves.LoadProfile().operationsMissionMedals,Is.Empty);
    }
    private OperationsMissionResult ReservedResult(OperationsOutcomeKind outcome)
    {
        Assert.That(commands.TryNewRun(new OperationsCommand("cmd.operations.homecreate",0,OperationsCommandKind.NewRun,"","",""),
            1102,OperationsDifficultyKind.Regular,out var created,out string error),Is.True,error);
        Assert.That(created.Accepted,Is.True);
        var state=commands.Read();var offer=Array.Find(state.activeRun.offers,x=>x.missionId=="operation.o001");
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.homedeploy",state.profileRevision,OperationsCommandKind.Deploy,offer.districtId,offer.offerId,""),
            out var deployed,out error),Is.True,error);Assert.That(deployed.Accepted,Is.True);
        state=commands.Read();var attempt=Array.Find(state.activeRun.attempts,a=>a.sessionId==state.pendingDeployment.sessionId);
        return new OperationsMissionResult(1,state.activeRun.runId,attempt.offerId,attempt.missionId,attempt.sessionId,attempt.attemptOrdinal,
            1,outcome,"terminal",1,Array.Empty<OperationsObjectiveFact>(),Array.Empty<OperationsObjectiveFact>(),0,
            Array.Empty<OperationsObjectiveFact>(),Array.Empty<OperationsObjectiveFact>(),Array.Empty<string>(),0,16,"home_result_hash");
    }
}
public static class MainMenuModeCardsRegression
{
    public static void RunFocusedValidation()
    {
        int passed=0;
        foreach(var type in new[]{typeof(OperationsProfilePersistenceTests),typeof(MainMenuModeMedalPersistenceTests)})
        {
            var methods=type.GetMethods(BindingFlags.Public|BindingFlags.Instance);
            if(methods.Any(m=>m.IsDefined(typeof(TestCaseAttribute),true)||m.IsDefined(typeof(TestCaseSourceAttribute),true)))
                throw new InvalidOperationException("Fixture requires full NUnit runner: "+type.Name);
            foreach(var test in methods.Where(m=>m.IsDefined(typeof(TestAttribute),true)))
            {
                object instance=Activator.CreateInstance(type);
                try
                {
                    foreach(var setup in methods.Where(m=>m.IsDefined(typeof(SetUpAttribute),true)))setup.Invoke(instance,null);
                    test.Invoke(instance,null);passed++;
                }
                catch(TargetInvocationException error){throw error.InnerException??error;}
                finally{foreach(var teardown in methods.Where(m=>m.IsDefined(typeof(TearDownAttribute),true)))teardown.Invoke(instance,null);}
            }
        }
        Debug.Log("[MainMenuModeCardsRegression] result=Passed tests="+passed+" atomicMedals=true legacyPreserved=true fixtures=true");
    }
}
