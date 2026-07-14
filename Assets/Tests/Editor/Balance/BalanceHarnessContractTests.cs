using System;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Runtime;

public sealed class BalanceHarnessContractTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(
                nameof(BalanceMetrics_ResourceExchangeTelemetrySummarizesReportFields),
                test => test.BalanceMetrics_ResourceExchangeTelemetrySummarizesReportFields(),
                ref passed);
            RunCase(
                nameof(BalanceReportWriter_IncludesResourceExchangeFields),
                test => test.BalanceReportWriter_IncludesResourceExchangeFields(),
                ref passed);
            RunCase(
                nameof(BalanceMetrics_FieldFabricationTelemetrySummarizesTypedCounters),
                test => test.BalanceMetrics_FieldFabricationTelemetrySummarizesTypedCounters(),
                ref passed);
            RunCase(
                nameof(BalanceReportWriter_IncludesFieldFabricationFields),
                test => test.BalanceReportWriter_IncludesFieldFabricationFields(),
                ref passed);

            Debug.Log($"[BalanceHarnessContractValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BalanceHarnessContractValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

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
    public void BalanceMetrics_ResourceExchangeTelemetrySummarizesReportFields()
    {
        var metrics = new BalanceMetrics();
        metrics.ApplyResourceExchangeTelemetry(
            "Skirmish",
            new[]
            {
                CreateExchangeQueueItem(
                    queueItemId: 1,
                    routeType: ResourceExchangeRouteType.Export,
                    inputAmount: 200,
                    outputAmount: 110,
                    durationSeconds: 45f,
                    state: ResourceExchangeQueueState.Completed),
                CreateExchangeQueueItem(
                    queueItemId: 2,
                    routeType: ResourceExchangeRouteType.Import,
                    inputAmount: 300,
                    outputAmount: 150,
                    durationSeconds: 45f,
                    state: ResourceExchangeQueueState.Blocked)
            },
            new[]
            {
                CreateExchangeEconomyEvent(
                    queueItemId: 1,
                    ResourceExchangeResultKind.QueueStarted,
                    ResourceExchangeResourceKind.Oil,
                    -200),
                CreateExchangeEconomyEvent(
                    queueItemId: 1,
                    ResourceExchangeResultKind.QueueCompleted,
                    ResourceExchangeResourceKind.Credits,
                    110),
                CreateExchangeEconomyEvent(
                    queueItemId: 2,
                    ResourceExchangeResultKind.QueueStarted,
                    ResourceExchangeResourceKind.Credits,
                    -300),
                CreateExchangeEconomyEvent(
                    queueItemId: 2,
                    ResourceExchangeResultKind.QueueBlocked,
                    ResourceExchangeResourceKind.Fuel,
                    0),
                CreateExchangeEconomyEvent(
                    queueItemId: 2,
                    ResourceExchangeResultKind.QueueCancelled,
                    ResourceExchangeResourceKind.Credits,
                    0),
                CreateExchangeEconomyEvent(
                    queueItemId: 2,
                    ResourceExchangeResultKind.RushAccepted,
                    ResourceExchangeResourceKind.RushTickets,
                    -2)
            });

        Assert.AreEqual("Skirmish", metrics.ResourceExchangeSourceMode);
        Assert.AreEqual("Export:1 Import:1", metrics.ResourceExchangeRouteSummary);
        Assert.AreEqual(2, metrics.ResourceExchangeStartedCount);
        Assert.AreEqual(1, metrics.ResourceExchangeCompletedCount);
        Assert.AreEqual(1, metrics.ResourceExchangeCancelledCount);
        Assert.AreEqual(1, metrics.ResourceExchangeBlockedCount);
        Assert.AreEqual(1, metrics.ResourceExchangeRushCount);
        Assert.AreEqual(500, metrics.ResourceExchangeInputAmount);
        Assert.AreEqual(260, metrics.ResourceExchangeOutputAmount);
        Assert.AreEqual(90f, metrics.ResourceExchangeDurationSeconds);
        Assert.AreEqual(50f, metrics.ResourceExchangeCompletionRatePercent);
        Assert.AreEqual(-190, metrics.ResourceExchangeCreditsDelta);
        Assert.AreEqual(-200, metrics.ResourceExchangeOilDelta);
        Assert.AreEqual(0, metrics.ResourceExchangeFuelDelta);
        Assert.AreEqual(-2, metrics.ResourceExchangeRushTicketsDelta);
        Assert.AreEqual(-392, metrics.ResourceExchangeNetResourceDelta);
    }

    [Test]
    [Category("Balance")]
    public void BalanceReportWriter_IncludesResourceExchangeFields()
    {
        string reportDirectory = Path.Combine(
            Path.GetTempPath(),
            "BalanceReportTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var metrics = new BalanceMetrics
            {
                ProbeId = "QuickCustom_ResourceExchange_Report_Test",
                ScenarioId = "quick_custom_resource_exchange_report_test",
                ProbeDisplayName = "Resource Exchange Report Test",
                Seed = 104729,
                EnemyType = "Balanced",
                EnemyCount = 1,
                Difficulty = "Normal",
                StartingCredits = "Normal",
                SampledDurationSeconds = 10f * 60f,
                Winner = "Unresolved",
                ResultReason = "Harness contract test."
            };
            metrics.ApplyResourceExchangeTelemetry(
                "Mission",
                new[]
                {
                    CreateExchangeQueueItem(
                        queueItemId: 1,
                        routeType: ResourceExchangeRouteType.Export,
                        inputAmount: 100,
                        outputAmount: 55,
                        durationSeconds: 32f,
                        state: ResourceExchangeQueueState.Completed)
                },
                new[]
                {
                    CreateExchangeEconomyEvent(
                        queueItemId: 1,
                        ResourceExchangeResultKind.QueueStarted,
                        ResourceExchangeResourceKind.Oil,
                        -100),
                    CreateExchangeEconomyEvent(
                        queueItemId: 1,
                        ResourceExchangeResultKind.QueueCompleted,
                        ResourceExchangeResourceKind.Credits,
                        55)
                });

            BalanceReportWriter.ReportPaths paths = BalanceReportWriter.WriteReport(metrics, reportDirectory);

            string markdown = File.ReadAllText(paths.MarkdownPath);
            StringAssert.Contains("## Resource Exchange", markdown);
            StringAssert.Contains("Source mode", markdown);
            StringAssert.Contains("Route summary", markdown);
            StringAssert.Contains("Completion rate", markdown);
            StringAssert.Contains("Credits delta", markdown);
            StringAssert.Contains("Oil delta", markdown);
            StringAssert.Contains("`Mission`", markdown);
            StringAssert.Contains("`Export:1`", markdown);

            string json = File.ReadAllText(paths.JsonPath);
            StringAssert.Contains("ResourceExchangeSourceMode", json);
            StringAssert.Contains("ResourceExchangeRouteSummary", json);
            StringAssert.Contains("ResourceExchangeCompletionRatePercent", json);
            StringAssert.Contains("ResourceExchangeCreditsDelta", json);
        }
        finally
        {
            if (Directory.Exists(reportDirectory))
                Directory.Delete(reportDirectory, true);
        }
    }

    [Test]
    [Category("Balance")]
    public void BalanceMetrics_FieldFabricationTelemetrySummarizesTypedCounters()
    {
        var metrics = new BalanceMetrics();
        metrics.ApplyFieldFabricationTelemetry(
            new FactionTacticalMaterialsComponent
            {
                Current = 75,
                Capacity = 200,
                LifetimeFabricated = 120,
                LifetimeImported = 30,
                LifetimeRewarded = 10,
                LifetimeExported = 15,
                LifetimeSpent = 80,
                LifetimeConstructionSpent = 40,
                LifetimeRepairSpent = 10,
                LifetimeInfrastructureSpent = 8,
                LifetimeUpgradeSpent = 7
            },
            new FactionMaterialFabricationTelemetryComponent
            {
                ActiveSeconds = 90f,
                NoOilInputBlockedSeconds = 12f,
                MaterialsCapacityFullBlockedSeconds = 8f,
                NoOilRouteBlockedSeconds = 4f,
                ProductionDisabledSeconds = 3f,
                BuildingDisabledSeconds = 2f
            });

        Assert.AreEqual(120, metrics.MaterialsFabricated);
        Assert.AreEqual(30, metrics.MaterialsImported);
        Assert.AreEqual(10, metrics.MaterialsRewarded);
        Assert.AreEqual(15, metrics.MaterialsExported);
        Assert.AreEqual(80, metrics.MaterialsGrossSpent);
        Assert.AreEqual(40, metrics.MaterialsConstructionSpent);
        Assert.AreEqual(90f, metrics.FabricationActiveSeconds);
        Assert.AreEqual(29f, metrics.FabricationBlockedSeconds);
    }

    [Test]
    [Category("Balance")]
    public void BalanceReportWriter_IncludesFieldFabricationFields()
    {
        string reportDirectory = Path.Combine(
            Path.GetTempPath(),
            "BalanceReportTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var metrics = new BalanceMetrics
            {
                ProbeId = "Field_Fabrication_Report_Test",
                ProbeDisplayName = "Field Fabrication Report Test"
            };
            metrics.ApplyFieldFabricationTelemetry(
                new FactionTacticalMaterialsComponent
                {
                    Current = 50,
                    Capacity = 100,
                    LifetimeFabricated = 80,
                    LifetimeSpent = 30,
                    LifetimeConstructionSpent = 30
                },
                new FactionMaterialFabricationTelemetryComponent
                {
                    ActiveSeconds = 60f,
                    NoOilInputBlockedSeconds = 15f
                });

            BalanceReportWriter.ReportPaths paths = BalanceReportWriter.WriteReport(metrics, reportDirectory);
            string markdown = File.ReadAllText(paths.MarkdownPath);
            StringAssert.Contains("## Materials And Field Fabrication", markdown);
            StringAssert.Contains("Gross spent including exports", markdown);
            StringAssert.Contains("Blocked, no Oil input", markdown);

            string json = File.ReadAllText(paths.JsonPath);
            StringAssert.Contains("MaterialsFabricated", json);
            StringAssert.Contains("MaterialsConstructionSpent", json);
            StringAssert.Contains("FabricationNoOilInputBlockedSeconds", json);
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

    private static ResourceExchangeQueueComponent CreateExchangeQueueItem(
        int queueItemId,
        ResourceExchangeRouteType routeType,
        int inputAmount,
        int outputAmount,
        float durationSeconds,
        ResourceExchangeQueueState state)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.balance.test"),
            RouteType = routeType,
            InputResource = routeType == ResourceExchangeRouteType.Export
                ? ResourceExchangeResourceKind.Oil
                : ResourceExchangeResourceKind.Credits,
            OutputResource = routeType == ResourceExchangeRouteType.Export
                ? ResourceExchangeResourceKind.Credits
                : ResourceExchangeResourceKind.Fuel,
            InputAmount = inputAmount,
            OutputAmount = outputAmount,
            State = state,
            DurationSeconds = durationSeconds
        };
    }

    private static ResourceExchangeEconomyEventComponent CreateExchangeEconomyEvent(
        int queueItemId,
        ResourceExchangeResultKind resultKind,
        ResourceExchangeResourceKind resourceKind,
        int amount)
    {
        return new ResourceExchangeEconomyEventComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            ResultKind = resultKind,
            ResourceKind = resourceKind,
            Amount = amount,
            RecipeId = new FixedString128Bytes("exchange.balance.test")
        };
    }

    private static void RunCase(
        string name,
        Action<BalanceHarnessContractTests> action,
        ref int passed)
    {
        var test = new BalanceHarnessContractTests();
        action(test);
        passed++;
    }
}
