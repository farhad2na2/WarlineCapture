using System;
using System.IO;
using NUnit.Framework;
using Game.Configs;
using Game.Runtime;

public sealed class BalanceHarnessContractTests
{
    [Test]
    [Category("Balance")]
    public void BalanceOutcomeClassifier_ClassifiesGoodRuntimeSnapshot()
    {
        var metrics = new BalanceMetrics
        {
            SampledDurationSeconds = 10f * 60f,
            OilExtracted = 320,
            FuelProduced = 145,
            SoldiersOrdered = 6,
            BuildingsBuilt = 2,
            OwnSoldiersDead = 1,
            EnemySoldiersDead = 4
        };

        BalanceOutcomeClassifier.Classify(metrics);

        Assert.AreEqual(BalanceOutcomeClassifier.Good, metrics.MatchDurationClassification);
        Assert.AreEqual(BalanceOutcomeClassifier.Good, metrics.EconomyActivityClassification);
        Assert.AreEqual(BalanceOutcomeClassifier.Good, metrics.CasualtyClassification);
        Assert.AreEqual(BalanceOutcomeClassifier.Good, metrics.OverallClassification);
    }

    [Test]
    [Category("Balance")]
    public void BalanceOutcomeClassifier_ProblemClassificationDoesNotRepresentHarnessFailure()
    {
        var metrics = new BalanceMetrics
        {
            SampledDurationSeconds = 3f * 60f,
            OilExtracted = 10,
            FuelProduced = 0,
            OwnSoldiersDead = 12,
            EnemySoldiersDead = 2
        };

        Assert.DoesNotThrow(() => BalanceOutcomeClassifier.Classify(metrics));
        Assert.AreEqual(BalanceOutcomeClassifier.Problem, metrics.OverallClassification);
    }

    [Test]
    [Category("Balance")]
    public void BalanceMetrics_FromQuickGameConfig_UsesCanonicalStartingCreditsField()
    {
        QuickGameConfig config = QuickGameConfig.Defaults;
        config.StartingMoney = AIStartingMoneySetting.High;

        BalanceMetrics metrics = BalanceMetrics.FromQuickGameConfig(
            "QuickCustom_Test",
            "quick_custom_test",
            config,
            sampledDurationSeconds: 10f * 60f,
            new GameRuntimeStats.Snapshot());

        Assert.AreEqual("High", metrics.StartingCredits);
        Assert.AreEqual(BalanceOutcomeClassifier.Watch, metrics.EconomyActivityClassification);
    }

    [Test]
    [Category("Balance")]
    public void BalanceMetrics_FromProbeDefinition_CarriesProbeMetadataAndSample()
    {
        BalanceProbeDefinition definition = QuickCustomBalanceProbe.CreateHardSwarmDefinition();

        BalanceMetrics metrics = BalanceMetrics.FromProbeDefinition(definition);

        Assert.AreEqual(QuickCustomBalanceProbe.HardSwarmProbeId, metrics.ProbeId);
        Assert.AreEqual("Quick Custom Hard Swarm", metrics.ProbeDisplayName);
        StringAssert.Contains("High-pressure", metrics.ProbeDescription);
        Assert.AreEqual("Swarm", metrics.EnemyType);
        Assert.AreEqual(3, metrics.EnemyCount);
        Assert.AreEqual("Hard", metrics.Difficulty);
        Assert.AreEqual("Frequent", metrics.AttackFrequency);
        Assert.AreEqual(9, metrics.SoldiersOrdered);
        Assert.AreEqual(14, metrics.EnemySoldiersDead);
    }

    [Test]
    [Category("Balance")]
    public void QuickCustomBalanceProbe_DefinitionsHaveUniqueProbeIds()
    {
        BalanceProbeDefinition defaultMedium = QuickCustomBalanceProbe.CreateDefaultMediumDefinition();
        BalanceProbeDefinition hardSwarm = QuickCustomBalanceProbe.CreateHardSwarmDefinition();

        Assert.AreNotEqual(defaultMedium.ProbeId, hardSwarm.ProbeId);
        Assert.AreNotEqual(defaultMedium.ScenarioId, hardSwarm.ScenarioId);
        Assert.Greater(hardSwarm.SampledDurationSeconds, 0f);
    }

    [Test]
    [Category("Balance")]
    public void BalanceReportWriter_WritesReportsOutsideAssetsWithNonValidationNotice()
    {
        string reportDirectory = Path.Combine(
            Path.GetTempPath(),
            "BalanceReportTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var metrics = new BalanceMetrics
            {
                ProbeId = "QuickCustom_ReportWriter_Test",
                ScenarioId = "quick_custom_report_writer_test",
                Seed = 104729,
                EnemyType = "Balanced",
                EnemyCount = 1,
                Difficulty = "Normal",
                StartingCredits = "Normal",
                SampledDurationSeconds = 10f * 60f,
                Winner = "Unresolved",
                ResultReason = "Harness contract test.",
                OilExtracted = 100,
                FuelProduced = 40,
                SoldiersOrdered = 3,
                BuildingsBuilt = 1,
                OwnSoldiersDead = 1,
                EnemySoldiersDead = 2
            };
            BalanceOutcomeClassifier.Classify(metrics);

            BalanceReportWriter.ReportPaths paths = BalanceReportWriter.WriteReport(metrics, reportDirectory);

            Assert.IsTrue(File.Exists(paths.JsonPath), paths.JsonPath);
            Assert.IsTrue(File.Exists(paths.MarkdownPath), paths.MarkdownPath);
            Assert.IsFalse(paths.JsonPath.Contains($"{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}"));

            string markdown = File.ReadAllText(paths.MarkdownPath);
            StringAssert.Contains("not a build-validation gate", markdown);
            StringAssert.Contains("Starting Credits", markdown);
            StringAssert.Contains("Probe id", markdown);
        }
        finally
        {
            if (Directory.Exists(reportDirectory))
                Directory.Delete(reportDirectory, true);
        }
    }

    [Test]
    [Category("Balance")]
    public void BalanceProbeRunner_ExposesDocumentedRunAllEntryPoint()
    {
        Assert.NotNull(typeof(BalanceProbeRunner).GetMethod(nameof(BalanceProbeRunner.RunAllBalanceProbes)));
    }
}
