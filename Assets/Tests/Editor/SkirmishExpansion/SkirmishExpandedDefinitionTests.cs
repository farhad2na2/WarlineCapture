using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
public sealed class SkirmishExpandedDefinitionTests
{
    [Test]
    public void S002StandardRegularMatchesSetupMatrix()
    {
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup setup, out _);
        Assert.AreEqual("S002", setup.CatalogId);
        Assert.AreEqual("skirmish.s002", setup.DefinitionId);
        Assert.AreEqual("scenario.skirmish.s002", setup.ScenarioSetupId);
        Assert.AreEqual("opmap.skirmish.desert_base_01", setup.OperationMapId);
        Assert.AreEqual(SkirmishArmyProfileId.GroundManeuver, setup.ArmyProfileId);
        Assert.AreEqual(SkirmishStartPackageId.EstablishedBase, setup.StartPackageId);
        Assert.AreEqual(SkirmishObjectiveKind.BaseAssault, setup.ObjectiveKind);
        Assert.AreEqual(12, Count(setup, 1, SkirmishRoleKind.Rifle));
        Assert.AreEqual(4, Count(setup, 1, SkirmishRoleKind.Gunner));
        Assert.AreEqual(4, Count(setup, 1, SkirmishRoleKind.Rocketeer));
        Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.Car));
        Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.ApcArmored));
        Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.Tank));
        Assert.AreEqual(20, setup.PlayerInfantry);
        Assert.AreEqual(3, setup.PlayerGround);
        Assert.AreEqual(23, setup.PlayerCombat);
        Assert.AreEqual(34, setup.PlayerSupply);
        Assert.AreEqual(900, setup.MaterialsEach);
        Assert.AreEqual(240, setup.OilEach);
        Assert.AreEqual(700, setup.UsableFuelEach);
        Assert.AreEqual(1080, setup.DeadlineSeconds);
        Assert.AreEqual(2, (int)setup.Readiness);
        Assert.AreEqual("obj.s002.base.player", setup.PlayerBaseObjectId);
        Assert.AreEqual("obj.s002.base.enemy", setup.EnemyBaseObjectId);
    }

    [Test]
    public void S002AllSizesCompileAgainstMatrix()
    {
        CompileS002(SkirmishSizeId.Standard, 104731, out _, out _);
        CompileS002(SkirmishSizeId.War, 393243, out SkirmishResolvedSetup war, out _);
        CompileS002(SkirmishSizeId.LargeWar, 458881, out SkirmishResolvedSetup large, out _);
        Assert.AreEqual(1500, war.DeadlineSeconds);
        Assert.AreEqual(1800, large.DeadlineSeconds);
        Assert.AreEqual(2, Count(war, 1, SkirmishRoleKind.Tank));
        Assert.AreEqual(3, Count(large, 1, SkirmishRoleKind.Tank));
    }

    [Test]
    public void MissingDefinitionReportsFieldSpecificReason()
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        bool compiled = SkirmishSetupCompiler.TryCompile(
            null,
            SkirmishDifficultyId.Regular,
            SkirmishSizeId.Standard,
            104731,
            new SkirmishContentManifest(),
            matrix,
            out _,
            out List<SkirmishCompileReason> reasons);
        Assert.IsFalse(compiled);
        Assert.AreEqual(SkirmishReasonCode.MissingDefinition, reasons[0].Code);
        Assert.AreEqual("definition", reasons[0].Field);
    }

    [Test]
    public void SameSeedProducesStableSetupHash()
    {
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup a, out _);
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup b, out _);
        Assert.AreEqual(a.SetupHash, b.SetupHash);
        CompileS002(SkirmishSizeId.Standard, 130365, out SkirmishResolvedSetup c, out _);
        Assert.AreNotEqual(a.SetupHash, c.SetupHash);
    }

    [Test]
    public void CatalogWalkReportsMissingDefinitionsAndKeepsS002()
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
        Assert.IsTrue(SkirmishSetupCompiler.TryCompileCatalog(
            authored, matrix, manifest, out List<SkirmishResolvedSetup> compiled, out List<SkirmishCompileReason> reasons));
        Assert.AreEqual(3, compiled.Count);
        Assert.Greater(reasons.Count, 0);
        Assert.AreEqual(SkirmishReasonCode.MissingDefinition, reasons[0].Code);
    }

    [Test]
    public void BaseAssaultReducerKeepsReplacementAndWipeNonTerminal()
    {
        Assert.IsFalse(Game.Runtime.SkirmishBaseAssaultObjectiveSystem.TryEvaluate(
            true, true, 10f, 1080, false, out _, out _));
        Assert.IsTrue(Game.Runtime.SkirmishBaseAssaultObjectiveSystem.TryEvaluate(
            true, false, 10f, 1080, false, out var victory, out var victoryReason));
        Assert.AreEqual(SkirmishOutcomeKind.Victory, victory);
        Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, victoryReason);
        Assert.IsTrue(Game.Runtime.SkirmishBaseAssaultObjectiveSystem.TryEvaluate(
            false, false, 10f, 1080, false, out var draw, out var drawReason));
        Assert.AreEqual(SkirmishOutcomeKind.Draw, draw);
        Assert.AreEqual(SkirmishEndReasonKind.BothBasesDestroyed, drawReason);
    }

    [Test]
    public void ExpandedLaunchDoesNotNormalizeLegacyQuickGame()
    {
        using var world = new World(nameof(ExpandedLaunchDoesNotNormalizeLegacyQuickGame));
        EntityManager em = world.EntityManager;
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
        Assert.IsTrue(SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup));
        Assert.IsTrue(em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).CalculateEntityCount() == 1);
        var session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingleton<SkirmishExpandedSessionComponent>();
        Assert.AreEqual(0, session.IsLegacy);
        Assert.AreEqual(SkirmishSizeId.Standard, session.SizeId);
        var match = em.CreateEntityQuery(typeof(SkirmishMatchState)).GetSingleton<SkirmishMatchState>();
        Assert.AreEqual(0, match.ScenarioIndex);
    }

    [Test]
    public void LegacyPrototypeIndicesRemainReserved()
    {
        Assert.IsTrue(SkirmishLegacyPrototypeMap.TryGetLegacyCatalogId(0, out string desert));
        Assert.AreEqual("S001", desert);
        Assert.IsTrue(SkirmishLegacyPrototypeMap.TryGetLegacyCatalogId(1, out string city));
        Assert.AreEqual("S025", city);
        Assert.IsTrue(SkirmishLegacyPrototypeMap.TryGetLegacyCatalogId(3, out string basin));
        Assert.AreEqual("S073", basin);
        Assert.IsTrue(SkirmishLegacyPrototypeMap.IsEditorStressIndex(2));
        Assert.IsFalse(SkirmishLegacyPrototypeMap.TryGetLegacyCatalogId(4, out _));
        var unknown = QuickGameConfig.Defaults;
        unknown.ScenarioIndex = 4;
        Assert.AreEqual(0, unknown.NormalizeForBaseAssault().ScenarioIndex);
        unknown.ScenarioIndex = 2;
        Assert.AreEqual(2, unknown.NormalizeForBaseAssault().ScenarioIndex);
    }

    public static void RunFocusedValidation()
    {
        try
        {
            var suite = new SkirmishExpandedDefinitionTests();
            suite.S002StandardRegularMatchesSetupMatrix();
            suite.S002AllSizesCompileAgainstMatrix();
            suite.MissingDefinitionReportsFieldSpecificReason();
            suite.SameSeedProducesStableSetupHash();
            suite.CatalogWalkReportsMissingDefinitionsAndKeepsS002();
            suite.BaseAssaultReducerKeepsReplacementAndWipeNonTerminal();
            suite.ExpandedLaunchDoesNotNormalizeLegacyQuickGame();
            suite.LegacyPrototypeIndicesRemainReserved();
            Debug.Log("[SkirmishExpandedDefinitionTests] result=Passed");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SkirmishExpandedDefinitionTests] result=Failed\n" + exception);
            throw;
        }
    }

    private static void CompileS002(
        SkirmishSizeId size,
        int seed,
        out SkirmishResolvedSetup setup,
        out SkirmishLaunchPayload payload)
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
        Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
            authored.DefinitionS002,
            SkirmishDifficultyId.Regular,
            size,
            seed,
            manifest,
            matrix,
            out setup,
            out List<SkirmishCompileReason> reasons),
            reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
        payload = new SkirmishLaunchPayload
        {
            SessionId = "test-s002",
            CatalogId = setup.CatalogId,
            DefinitionId = setup.DefinitionId,
            ContentVersion = setup.ContentVersion,
            AllConfigHashes = setup.SetupHash.ToString("X8"),
            MapId = setup.OperationMapId,
            LayoutId = setup.LayoutId,
            DifficultyId = SkirmishDifficultyId.Regular,
            SizeId = size,
            Seed = seed,
            IsCustom = false,
            IsLegacy = false
        };
    }

    private static void LoadMatrix(out List<SkirmishSetupMatrixRow> matrix)
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out matrix, out string error), error);
    }

    private static int Count(SkirmishResolvedSetup setup, byte faction, SkirmishRoleKind role)
    {
        int total = 0;
        for (int i = 0; i < setup.Forces.Length; i++)
        {
            if (setup.Forces[i].FactionId == faction && setup.Forces[i].RoleKind == role)
                total += setup.Forces[i].Quantity;
        }

        return total;
    }
}
}
