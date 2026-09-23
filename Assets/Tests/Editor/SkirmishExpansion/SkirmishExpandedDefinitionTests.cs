using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
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
        Assert.IsTrue(setup.SharedFog);
        Assert.IsTrue(setup.DevelopmentFullVision);
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
        Assert.IsFalse(SkirmishLegacyPrototypeMap.TryGetLegacyCatalogId(5, out _));
        var established = QuickGameConfig.Defaults;
        established.ScenarioIndex = SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex;
        Assert.AreEqual(
            SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex,
            established.NormalizeForBaseAssault().ScenarioIndex);
        var airMobile = QuickGameConfig.Defaults;
        airMobile.ScenarioIndex = SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex;
        Assert.AreEqual(
            SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex,
            airMobile.NormalizeForBaseAssault().ScenarioIndex);
        var unknown = QuickGameConfig.Defaults;
        unknown.ScenarioIndex = 6;
        Assert.AreEqual(0, unknown.NormalizeForBaseAssault().ScenarioIndex);
        unknown.ScenarioIndex = 2;
        Assert.AreEqual(2, unknown.NormalizeForBaseAssault().ScenarioIndex);
    }

    [Test]
    public void S002GroundOverlaysAndProductionGate()
    {
        SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
        Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(overlays, SkirmishRoleKind.Rifle, out SkirmishRoleOverlay rifle));
        Assert.AreEqual(100, rifle.MaxHealth);
        Assert.AreEqual(SkirmishProducerKind.Barracks, rifle.Producer);
        Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(overlays, SkirmishRoleKind.Tank, out SkirmishRoleOverlay tank));
        Assert.AreEqual(420, tank.MaxHealth);
        Assert.AreEqual(SkirmishProducerKind.GroundStaging, tank.Producer);
        Assert.AreEqual(80, rifle.MaterialsCost);
        Assert.AreEqual(360, tank.MaterialsCost);
        Assert.AreNotEqual(rifle.MaxHealth, tank.MaxHealth);

        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        SkirmishProductionDecision infantry = SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = SkirmishRoleIds.Rifle,
                RoleKind = SkirmishRoleKind.Rifle,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true
            },
            authored.ArmyGround,
            SkirmishReadinessStage.Established,
            overlays);
        Assert.IsTrue(infantry.Accepted);
        Assert.AreEqual(SkirmishRoleIds.InfantrySquadMembers, infantry.MemberCount);

        SkirmishProductionDecision missingYard = SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = SkirmishRoleIds.Tank,
                RoleKind = SkirmishRoleKind.Tank,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = false
            },
            authored.ArmyGround,
            SkirmishReadinessStage.Established,
            overlays);
        Assert.IsFalse(missingYard.Accepted);
        Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingYard.Reason);

        SkirmishProductionDecision air = SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = SkirmishRoleIds.AttackHeli,
                RoleKind = SkirmishRoleKind.AttackHeli,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true
            },
            authored.ArmyGround,
            SkirmishReadinessStage.Established,
            overlays);
        Assert.IsFalse(air.Accepted);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, air.Reason);
    }

    [Test]
    public void S002SpawnProjectsRoleOverlaysAndGroundStaging()
    {
        using var world = new World(nameof(S002SpawnProjectsRoleOverlaysAndGroundStaging));
        EntityManager em = world.EntityManager;
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
        Assert.IsTrue(SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup));
        Entity session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
        Assert.GreaterOrEqual(setup.Structures.Length, 4);
        Assert.IsTrue(HasStructure(setup, SkirmishStructureIds.GroundStaging, false));
        Assert.IsTrue(HasStructure(setup, SkirmishStructureIds.Barracks, true));
        Assert.IsTrue(Game.Runtime.SkirmishScenarioSpawnSystem.TrySpawnLedgers(
            em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
        Assert.AreEqual(SkirmishReasonCode.None, reason);
        Assert.AreEqual(0, visualPending);
        Assert.IsTrue(AllStartingForcesHavePrefabKeys(setup));
        using var keyed = em.CreateEntityQuery(typeof(UnitSourcePrefabKey), typeof(SkirmishUnitRoleComponent));
        Assert.AreEqual(setup.PlayerInfantry + setup.PlayerGround + setup.EnemyInfantry + setup.EnemyGround,
            keyed.CalculateEntityCount());
        var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
        Assert.AreEqual(setup.PlayerInfantry, capacity.InfantryLive);
        Assert.AreEqual(setup.PlayerGround, capacity.GroundLive);
        Assert.AreEqual(setup.PlayerSupply, capacity.SupplyLive);

        using var units = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent));
        Assert.AreEqual(setup.PlayerInfantry + setup.PlayerGround + setup.EnemyInfantry + setup.EnemyGround,
            units.CalculateEntityCount());
        Assert.AreEqual(12, CountOwnedRole(em, 1, SkirmishRoleKind.Rifle));

        using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
        int applied = Game.Runtime.SkirmishRosterProjectionSystem.Apply(
            em, owned, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId, setup);
        Assert.Greater(applied, 0);
        Assert.AreEqual(100, FirstOverlayHealth(em, SkirmishRoleKind.Rifle));
        Assert.AreEqual(420, FirstOverlayHealth(em, SkirmishRoleKind.Tank));
        Assert.AreEqual(800, FirstDesignatedBaseHealth(em));
    }

    [Test]
    public void CompilerAcceptsTypedDesertBaseLayout()
    {
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        SkirmishMapLayoutConfig layout = authored.LayoutDbBa;
        var reasons = new List<SkirmishCompileReason>();
        Assert.IsTrue(SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(layout, reasons),
            reasons.Count == 0 ? "layout validation failed" : reasons[0].ToString());
        Assert.AreEqual("layout.skirmish.db.ba", layout.LayoutId);
        Assert.AreEqual("opmap.skirmish.desert_base_01", layout.OperationMapId);
        Assert.AreEqual(600f, layout.WorldWidthMetres);
        Assert.AreEqual(420f, layout.WorldDepthMetres);
        Assert.IsTrue(layout.TryGetAnchor("staging.player", out SkirmishLayoutAnchorConfig staging));
        Assert.AreEqual(-192f, staging.WorldX, 0.05f);
        Assert.AreEqual(0f, staging.WorldZ, 0.05f);
        Assert.IsTrue(layout.TryGetPad(1, SkirmishLegalPadKind.GroundStaging, out _));
        Assert.IsTrue(layout.TryGetPad(1, SkirmishLegalPadKind.InfantrySpawn, out _));
        Assert.IsTrue(layout.TryGetRoute(SkirmishMeasuredRouteKind.MainHighway, out SkirmishLayoutRouteConfig highway));
        Assert.IsTrue(layout.TryGetRoute(SkirmishMeasuredRouteKind.FlankNorthRuins, out SkirmishLayoutRouteConfig north));
        Assert.IsTrue(layout.TryGetRoute(SkirmishMeasuredRouteKind.FlankSouthSweep, out SkirmishLayoutRouteConfig south));
        Assert.IsTrue(highway.ProvisionalTimes);
        Assert.GreaterOrEqual(highway.InfantryFirstContactSeconds, 45f);
        Assert.LessOrEqual(highway.InfantryFirstContactSeconds, 75f);
        Assert.GreaterOrEqual(north.InfantryFirstContactSeconds, 75f);
        Assert.LessOrEqual(north.InfantryFirstContactSeconds, 105f);
        Assert.GreaterOrEqual(south.InfantryFirstContactSeconds, 75f);
        Assert.AreEqual("route.skirmish.db.main", highway.RouteId);
    }

    [Test]
    public void RegularStandardBindsMeasuredPadsAndRoutes()
    {
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
        Assert.IsTrue(setup.MeasuredLayoutBound);
        Assert.IsFalse(payload.IsCustom);
        Assert.IsFalse(payload.IsLegacy);
        Assert.AreEqual("layout.skirmish.db.ba", setup.LayoutId);
        Assert.AreEqual("route.skirmish.db.main", setup.DefaultRouteId);
        // Map-relative deployment: pads deploy through the pinned world binding onto the
        // operation map's authored deployment anchors, not the raw authoring envelope.
        Assert.AreEqual(949f, setup.PlayerBaseWorldX, 0.5f);
        Assert.AreEqual(344.7f, setup.PlayerBaseWorldZ, 0.5f);
        Assert.AreEqual(1007.21f, setup.PlayerStagingWorldX, 0.5f);
        Assert.AreEqual(326.01f, setup.PlayerStagingWorldZ, 0.5f);
        Assert.AreEqual(1627.84f, setup.EnemyStagingWorldX, 0.5f);
        Assert.AreEqual(126.69f, setup.EnemyStagingWorldZ, 0.5f);
        Assert.AreNotEqual(setup.PlayerStagingWorldX, setup.PlayerSpawnPadX);
        Assert.AreNotEqual(setup.PlayerSpawnPadX, setup.PlayerRallyPadX);

        Assert.IsTrue(TryFindStructure(setup, 1, SkirmishStructureIds.GroundStaging, out SkirmishResolvedStructureEntry staging));
        Assert.AreEqual(setup.PlayerStagingWorldX, staging.SpawnWorldX, 0.05f);
        Assert.AreEqual(setup.PlayerStagingWorldZ, staging.SpawnWorldZ, 0.05f);
        Assert.IsTrue(TryFindStructure(setup, 1, SkirmishStructureIds.Barracks, out SkirmishResolvedStructureEntry barracks));
        Assert.AreEqual(setup.PlayerBaseWorldX, barracks.SpawnWorldX, 0.05f);

        Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.Rifle, out SkirmishResolvedForceEntry rifle));
        Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.Tank, out SkirmishResolvedForceEntry tank));
        Assert.AreNotEqual(rifle.SpawnWorldX, tank.SpawnWorldX);
        Assert.AreEqual("anchor.skirmish.db.staging_player", rifle.SpawnAnchorId);

        Assert.IsTrue(SkirmishVisualSpawnService.TryResolveMeasuredWorld(
            setup, 1, true, SkirmishStructureIds.GroundStaging, SkirmishRoleKind.None, 0, out Vector3 stagingWorld));
        Assert.AreEqual(setup.PlayerStagingWorldX, stagingWorld.x, 0.05f);
        Assert.IsTrue(SkirmishVisualSpawnService.TryResolveMeasuredWorld(
            setup, 1, false, string.Empty, SkirmishRoleKind.Tank, 0, out Vector3 tankWorld));
        Assert.AreEqual(tank.SpawnWorldX, tankWorld.x, 0.05f);
    }

    [Test]
    public void CustomAndLegacyDoNotBindMeasuredLayout()
    {
        CompileS002(SkirmishSizeId.War, 393243, out SkirmishResolvedSetup war, out _);
        Assert.IsFalse(war.MeasuredLayoutBound);
        Assert.AreEqual(0f, war.PlayerStagingWorldX);
        Assert.AreEqual(0f, war.Forces[0].SpawnWorldX);

        var custom = new SkirmishResolvedSetup
        {
            CatalogId = "S002",
            DifficultyId = SkirmishDifficultyId.Regular,
            SizeId = SkirmishSizeId.Standard
        };
        Assert.IsFalse(custom.MeasuredLayoutBound);
        Assert.IsFalse(SkirmishVisualSpawnService.TryResolveMeasuredWorld(
            custom, 1, true, SkirmishStructureIds.GroundStaging, SkirmishRoleKind.None, 0, out _));

        var legacyPayload = SkirmishLaunchPayload.CreateLegacy(0, 0);
        Assert.IsTrue(legacyPayload.IsLegacy);
        Assert.IsFalse(legacyPayload.IsCustom);
        Assert.AreNotEqual("layout.skirmish.db.ba", legacyPayload.LayoutId);
    }

    [Test]
    public void ExpandedLaunchResolverCompilesS002()
    {
        using var world = new World(nameof(ExpandedLaunchResolverCompilesS002));
        EntityManager em = world.EntityManager;
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
        Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
            em,
            "S002",
            SkirmishDifficultyId.Regular,
            SkirmishSizeId.Standard,
            104731,
            authored,
            matrix,
            manifest,
            out SkirmishResolvedSetup setup,
            out SkirmishLaunchPayload payload,
            out List<SkirmishCompileReason> reasons),
            reasons.Count == 0 ? "resolver failed" : reasons[0].ToString());
        Assert.AreEqual("S002", setup.CatalogId);
        Assert.AreEqual(104731, payload.Seed);
        Assert.IsFalse(payload.IsLegacy);
        Assert.AreEqual(0, em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingleton<SkirmishExpandedSessionComponent>().IsLegacy);
    }

    [Test]
    public void S003RegularStandardCompilesFirstVisitSeeds()
    {
        uint previous = 0;
        for (int i = 0; i < SkirmishS003FirstVisit.RegularStandardSeeds.Length; i++)
        {
            int seed = SkirmishS003FirstVisit.RegularStandardSeeds[i];
            CompileS003(SkirmishSizeId.Standard, seed, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
            Assert.AreEqual("S003", setup.CatalogId);
            Assert.AreEqual("skirmish.s003", setup.DefinitionId);
            Assert.AreEqual("scenario.skirmish.s003", setup.ScenarioSetupId);
            Assert.AreEqual("opmap.skirmish.desert_base_01", setup.OperationMapId);
            Assert.AreEqual("layout.skirmish.db.ba", setup.LayoutId);
            Assert.AreEqual(SkirmishArmyProfileId.AirMobile, setup.ArmyProfileId);
            Assert.AreEqual(SkirmishStartPackageId.FieldBase, setup.StartPackageId);
            Assert.AreEqual(SkirmishObjectiveKind.BaseAssault, setup.ObjectiveKind);
            Assert.AreEqual(1, (int)setup.Readiness);
            Assert.AreEqual(8, Count(setup, 1, SkirmishRoleKind.Rifle));
            Assert.AreEqual(0, Count(setup, 1, SkirmishRoleKind.Gunner));
            Assert.AreEqual(4, Count(setup, 1, SkirmishRoleKind.Rocketeer));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.Car));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.ApcArmored));
            Assert.AreEqual(0, Count(setup, 1, SkirmishRoleKind.Tank));
            Assert.AreEqual(0, Count(setup, 1, SkirmishRoleKind.AttackHeli));
            Assert.AreEqual(12, setup.PlayerInfantry);
            Assert.AreEqual(2, setup.PlayerGround);
            Assert.AreEqual(0, setup.PlayerAir);
            Assert.AreEqual(14, setup.PlayerCombat);
            Assert.AreEqual(20, setup.PlayerSupply);
            Assert.AreEqual(450, setup.MaterialsEach);
            Assert.AreEqual(120, setup.OilEach);
            Assert.AreEqual(350, setup.UsableFuelEach);
            Assert.AreEqual(1080, setup.DeadlineSeconds);
            Assert.AreEqual(7, setup.PlayerStartingStructures);
            Assert.AreEqual(seed, payload.Seed);
            Assert.IsTrue(setup.MeasuredLayoutBound);
            Assert.AreEqual(949f, setup.PlayerBaseWorldX, 0.5f);
            Assert.AreEqual(344.7f, setup.PlayerBaseWorldZ, 0.5f);
            Assert.AreEqual(1007.21f, setup.PlayerStagingWorldX, 0.5f);
            Assert.AreEqual(326.01f, setup.PlayerStagingWorldZ, 0.5f);
            Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.Rifle, out SkirmishResolvedForceEntry rifle));
            Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.Car, out SkirmishResolvedForceEntry car));
            Assert.AreNotEqual(rifle.SpawnWorldX, car.SpawnWorldX);
            Assert.AreNotEqual(previous, setup.SetupHash);
            previous = setup.SetupHash;
        }

        CompileS003(SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA, out SkirmishResolvedSetup again, out _);
        CompileS003(SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA, out SkirmishResolvedSetup twin, out _);
        Assert.AreEqual(again.SetupHash, twin.SetupHash);
    }

    [Test]
    public void S003FieldSizesMatchMatrixWithoutStartingAir()
    {
        CompileS003(SkirmishSizeId.War, 393244, out SkirmishResolvedSetup war, out _);
        CompileS003(SkirmishSizeId.LargeWar, 458882, out SkirmishResolvedSetup large, out _);
        Assert.AreEqual(1500, war.DeadlineSeconds);
        Assert.AreEqual(675, war.MaterialsEach);
        Assert.AreEqual(2, Count(war, 1, SkirmishRoleKind.ApcArmored));
        Assert.AreEqual(0, Count(war, 1, SkirmishRoleKind.Tank));
        Assert.AreEqual(1800, large.DeadlineSeconds);
        Assert.AreEqual(900, large.MaterialsEach);
        Assert.AreEqual(0, large.PlayerAir);
        Assert.IsFalse(war.MeasuredLayoutBound);
        Assert.IsFalse(large.MeasuredLayoutBound);
    }

    [Test]
    public void S003AirMobileRejectsArmorAndGatesOffensiveAir()
    {
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        Assert.IsTrue(authored.ArmyAir.AllowsOffensiveAir);
        Assert.IsTrue(authored.ArmyAir.AllowsAdvancedAir);
        Assert.IsFalse(authored.ArmyGround.AllowsOffensiveAir);
        Assert.IsFalse(authored.ArmyAir.Allows(SkirmishRoleIds.Tank));
        Assert.IsFalse(authored.ArmyAir.Allows(SkirmishRoleIds.ApcHeavy));
        Assert.IsFalse(authored.ArmyAir.Allows(SkirmishRoleIds.Siege));
        Assert.IsTrue(authored.ArmyAir.Allows(SkirmishRoleIds.AntiAir));
        SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();

        SkirmishProductionDecision aa = EvaluateS003(
            authored, overlays, SkirmishRoleIds.AntiAir, SkirmishRoleKind.AntiAir, SkirmishReadinessStage.Field, false, false);
        Assert.IsTrue(aa.Accepted);
        Assert.AreEqual(220, aa.MaterialsCost);
        Assert.AreEqual(SkirmishProducerKind.GroundStaging, aa.Producer);

        SkirmishProductionDecision tank = EvaluateS003(
            authored, overlays, SkirmishRoleIds.Tank, SkirmishRoleKind.Tank, SkirmishReadinessStage.Established, false, false);
        Assert.IsFalse(tank.Accepted);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);

        SkirmishProductionDecision heavy = EvaluateS003(
            authored, overlays, SkirmishRoleIds.ApcHeavy, SkirmishRoleKind.ApcHeavy, SkirmishReadinessStage.Established, false, false);
        Assert.IsFalse(heavy.Accepted);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, heavy.Reason);

        SkirmishProductionDecision siege = EvaluateS003(
            authored, overlays, SkirmishRoleIds.Siege, SkirmishRoleKind.Siege, SkirmishReadinessStage.FullArsenal, false, false);
        Assert.IsFalse(siege.Accepted);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, siege.Reason);

        SkirmishProductionDecision earlyAir = EvaluateS003(
            authored, overlays, SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, SkirmishReadinessStage.Field, false, false);
        Assert.IsFalse(earlyAir.Accepted);
        Assert.AreEqual(SkirmishReasonCode.MissingReadiness, earlyAir.Reason);

        SkirmishProductionDecision missingPad = EvaluateS003(
            authored, overlays, SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, SkirmishReadinessStage.Established, false, false);
        Assert.IsFalse(missingPad.Accepted);
        Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingPad.Reason);
        Assert.AreEqual("producer.helipad", missingPad.Field);

        SkirmishProductionDecision heli = EvaluateS003(
            authored, overlays, SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, SkirmishReadinessStage.Established, true, false);
        Assert.IsTrue(heli.Accepted);
        Assert.AreEqual(420, heli.MaterialsCost);

        SkirmishProductionDecision jet = EvaluateS003(
            authored, overlays, SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter, SkirmishReadinessStage.FullArsenal, false, false);
        Assert.IsFalse(jet.Accepted);
        Assert.AreEqual("producer.airport", jet.Field);

        SkirmishProductionDecision advanced = EvaluateS003(
            authored, overlays, SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter, SkirmishReadinessStage.FullArsenal, false, true);
        Assert.IsTrue(advanced.Accepted);
        Assert.AreEqual(480, advanced.MaterialsCost);

        SkirmishProductionDecision groundAir = SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = SkirmishRoleIds.AttackHeli,
                RoleKind = SkirmishRoleKind.AttackHeli,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true,
                HelipadPresent = true
            },
            authored.ArmyGround,
            SkirmishReadinessStage.Established,
            overlays);
        Assert.IsFalse(groundAir.Accepted);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, groundAir.Reason);
    }

    [Test]
    public void S003AssetsReuseDesertBaseLayoutAndStayPlayable()
    {
        var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset");
        var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/ScenarioSetup_S003.asset");
        var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishLayout_S003.asset");
        var shared = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishLayout_DB_BA.asset");
        var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
        Assert.IsNotNull(definition);
        Assert.AreEqual("S003", definition.CatalogId);
        Assert.AreEqual(SkirmishArmyProfileId.AirMobile, definition.ArmyProfileConfig.Kind);
        Assert.AreEqual(SkirmishStartPackageId.FieldBase, definition.StartPackageConfig.Kind);
        Assert.AreEqual(SkirmishSizeId.Standard, definition.FirstVisitSize);
        Assert.AreEqual(SkirmishSizeId.War, definition.RecommendedSize);
        Assert.AreEqual("layout.skirmish.db.ba", definition.MapLayoutId);
        Assert.AreEqual(shared.LayoutId, layout.LayoutId);
        Assert.AreEqual(shared.WorldWidthMetres, layout.WorldWidthMetres);
        Assert.AreEqual(shared.WorldDepthMetres, layout.WorldDepthMetres);
        Assert.IsTrue(layout.TryGetAnchor("staging.player", out SkirmishLayoutAnchorConfig staging));
        Assert.AreEqual(-192f, staging.WorldX, 0.05f);
        Assert.AreEqual("scenario.skirmish.s003", scenario.ScenarioId);
        Assert.AreEqual(SkirmishS003FirstVisit.SeedA, scenario.DeterministicSeed);
        Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig s002));
        Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
        Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig s003));
        Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
    }

    [Test]
    public void S004RegularStandardCompilesFirstVisitSeeds()
    {
        uint previous = 0;
        for (int i = 0; i < SkirmishS004FirstVisit.RegularStandardSeeds.Length; i++)
        {
            int seed = SkirmishS004FirstVisit.RegularStandardSeeds[i];
            CompileS004(SkirmishSizeId.Standard, seed, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
            Assert.AreEqual("S004", setup.CatalogId);
            Assert.AreEqual("skirmish.s004", setup.DefinitionId);
            Assert.AreEqual("scenario.skirmish.s004", setup.ScenarioSetupId);
            Assert.AreEqual("opmap.skirmish.desert_base_01", setup.OperationMapId);
            Assert.AreEqual("layout.skirmish.db.ba", setup.LayoutId);
            Assert.AreEqual(SkirmishArmyProfileId.AirMobile, setup.ArmyProfileId);
            Assert.AreEqual(SkirmishStartPackageId.EstablishedBase, setup.StartPackageId);
            Assert.AreEqual(SkirmishObjectiveKind.BaseAssault, setup.ObjectiveKind);
            Assert.AreEqual(2, (int)setup.Readiness);
            Assert.AreEqual(12, Count(setup, 1, SkirmishRoleKind.Rifle));
            Assert.AreEqual(4, Count(setup, 1, SkirmishRoleKind.Gunner));
            Assert.AreEqual(4, Count(setup, 1, SkirmishRoleKind.Rocketeer));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.Car));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.ApcArmored));
            Assert.AreEqual(0, Count(setup, 1, SkirmishRoleKind.Tank));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.AntiAir));
            Assert.AreEqual(1, Count(setup, 1, SkirmishRoleKind.TransportHeli));
            Assert.AreEqual(1, Count(setup, 2, SkirmishRoleKind.AntiAir));
            Assert.AreEqual(1, Count(setup, 2, SkirmishRoleKind.TransportHeli));
            Assert.AreEqual(20, setup.PlayerInfantry);
            Assert.AreEqual(3, setup.PlayerGround);
            Assert.AreEqual(1, setup.PlayerAir);
            Assert.AreEqual(24, setup.PlayerCombat);
            Assert.AreEqual(42, setup.PlayerSupply);
            Assert.AreEqual(20, setup.EnemyInfantry);
            Assert.AreEqual(1, setup.EnemyAir);
            Assert.AreEqual(900, setup.MaterialsEach);
            Assert.AreEqual(240, setup.OilEach);
            Assert.AreEqual(700, setup.UsableFuelEach);
            Assert.AreEqual(1080, setup.DeadlineSeconds);
            Assert.AreEqual(10, setup.PlayerStartingStructures);
            Assert.AreEqual(2, setup.InfantryQueuesEach);
            Assert.AreEqual(1, setup.VehicleQueuesEach);
            Assert.AreEqual(seed, payload.Seed);
            Assert.IsTrue(setup.MeasuredLayoutBound);
            Assert.AreEqual(949f, setup.PlayerBaseWorldX, 0.5f);
            Assert.AreEqual(344.7f, setup.PlayerBaseWorldZ, 0.5f);
            Assert.AreEqual(1007.21f, setup.PlayerStagingWorldX, 0.5f);
            Assert.AreEqual(326.01f, setup.PlayerStagingWorldZ, 0.5f);
            Assert.IsTrue(TryFindStructure(setup, 1, SkirmishStructureIds.Helipad, out SkirmishResolvedStructureEntry pad));
            Assert.IsFalse(pad.DesignatedBase);
            Assert.AreEqual(926.22f, pad.SpawnWorldX, 0.5f);
            Assert.AreEqual(440.25f, pad.SpawnWorldZ, 0.5f);
            Assert.IsTrue(TryFindStructure(setup, 2, SkirmishStructureIds.Helipad, out _));
            Assert.IsFalse(HasStructure(setup, SkirmishStructureIds.Airport, false));
            Assert.AreEqual(20, setup.Structures.Length);
            Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.TransportHeli, out SkirmishResolvedForceEntry transport));
            Assert.AreEqual("Unit_Veh_Helicopter_Transport", transport.RuntimePrefabKey);
            Assert.AreEqual(pad.SpawnWorldX, transport.SpawnWorldX, 0.2f);
            Assert.IsTrue(TryFindForce(setup, 1, SkirmishRoleKind.AntiAir, out SkirmishResolvedForceEntry aa));
            Assert.AreEqual("Unit_Veh_Missle_Launcher_Air", aa.RuntimePrefabKey);
            Assert.AreNotEqual(previous, setup.SetupHash);
            previous = setup.SetupHash;
        }

        CompileS004(SkirmishSizeId.Standard, SkirmishS004FirstVisit.SeedA, out SkirmishResolvedSetup again, out _);
        CompileS004(SkirmishSizeId.Standard, SkirmishS004FirstVisit.SeedA, out SkirmishResolvedSetup twin, out _);
        Assert.AreEqual(again.SetupHash, twin.SetupHash);

        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup ground, out _);
        Assert.IsFalse(HasStructure(ground, SkirmishStructureIds.Helipad, false));
        Assert.AreEqual(18, ground.Structures.Length);
        CompileS003(SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA, out SkirmishResolvedSetup field, out _);
        Assert.IsFalse(HasStructure(field, SkirmishStructureIds.Helipad, false));
        Assert.AreEqual(0, field.PlayerAir);
        Assert.AreEqual(1, (int)field.Readiness);
    }

    [Test]
    public void FieldAndEstablishedBackboneMatchesMatrix()
    {
        CompileS003(SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA, out SkirmishResolvedSetup field, out _);
        Assert.AreEqual(7, field.PlayerStartingStructures);
        Assert.AreEqual(14, field.Structures.Length);
        Assert.AreEqual(7, CountStructures(field, 1));
        Assert.AreEqual(7, CountStructures(field, 2));
        Assert.AreEqual(1, CountDesignated(field, 1));
        Assert.IsTrue(HasStructure(field, SkirmishStructureIds.OilPump, false));
        Assert.IsTrue(HasStructure(field, SkirmishStructureIds.Refinery, false));
        Assert.IsTrue(HasStructure(field, SkirmishStructureIds.FuelBladder, false));
        Assert.IsTrue(HasStructure(field, SkirmishStructureIds.FabricationDepot, false));
        Assert.IsTrue(HasStructure(field, SkirmishStructureIds.Watchtower, false));
        Assert.IsFalse(HasStructure(field, SkirmishStructureIds.WatchtowerApproach, false));
        Assert.IsFalse(HasStructure(field, SkirmishStructureIds.SatelliteDish, false));
        Assert.IsFalse(HasStructure(field, SkirmishStructureIds.Helipad, false));
        Assert.IsFalse(HasStructure(field, SkirmishStructureIds.RefineryModule, false));
        Assert.IsTrue(TryFindStructure(field, 1, SkirmishStructureIds.OilPump, out SkirmishResolvedStructureEntry pump));
        Assert.AreEqual(894.5f, pump.SpawnWorldX, 1f);
        Assert.AreEqual(267.7f, pump.SpawnWorldZ, 1f);
        AssertSeparated(field);

        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup ground, out _);
        Assert.AreEqual(10, ground.PlayerStartingStructures);
        Assert.AreEqual(18, ground.Structures.Length);
        Assert.IsTrue(HasStructure(ground, SkirmishStructureIds.SatelliteDish, false));
        Assert.IsTrue(HasStructure(ground, SkirmishStructureIds.WatchtowerApproach, false));
        Assert.IsFalse(HasStructure(ground, SkirmishStructureIds.Helipad, false));
        Assert.IsFalse(HasStructure(ground, SkirmishStructureIds.RefineryModule, false));

        CompileS004(SkirmishSizeId.Standard, SkirmishS004FirstVisit.SeedA, out SkirmishResolvedSetup established, out _);
        Assert.AreEqual(10, established.PlayerStartingStructures);
        Assert.AreEqual(20, established.Structures.Length);
        Assert.IsTrue(HasStructure(established, SkirmishStructureIds.Helipad, false));
        Assert.IsTrue(HasStructure(established, SkirmishStructureIds.SatelliteDish, false));
        Assert.IsFalse(HasStructure(established, SkirmishStructureIds.RefineryModule, false));
        Assert.AreEqual(1, CountDesignated(established, 1));

        CompileS004(SkirmishSizeId.War, 393245, out SkirmishResolvedSetup war, out _);
        Assert.AreEqual(11, war.PlayerStartingStructures);
        Assert.AreEqual(22, war.Structures.Length);
        Assert.IsTrue(HasStructure(war, SkirmishStructureIds.RefineryModule, false));
        Assert.IsTrue(HasStructure(war, SkirmishStructureIds.Helipad, false));
    }

    [Test]
    public void S004EstablishedSizesMatchMatrixWithStartingAir()
    {
        CompileS004(SkirmishSizeId.War, 393245, out SkirmishResolvedSetup war, out _);
        CompileS004(SkirmishSizeId.LargeWar, 458883, out SkirmishResolvedSetup large, out _);
        Assert.AreEqual(1500, war.DeadlineSeconds);
        Assert.AreEqual(1350, war.MaterialsEach);
        Assert.AreEqual(2, Count(war, 1, SkirmishRoleKind.AntiAir));
        Assert.AreEqual(2, Count(war, 1, SkirmishRoleKind.TransportHeli));
        Assert.AreEqual(2, war.PlayerAir);
        Assert.AreEqual(0, Count(war, 1, SkirmishRoleKind.Tank));
        Assert.AreEqual(11, war.PlayerStartingStructures);
        Assert.AreEqual(1800, large.DeadlineSeconds);
        Assert.AreEqual(1800, large.MaterialsEach);
        Assert.AreEqual(3, Count(large, 1, SkirmishRoleKind.AntiAir));
        Assert.AreEqual(2, Count(large, 1, SkirmishRoleKind.TransportHeli));
        Assert.AreEqual(2, large.PlayerAir);
        Assert.AreEqual(49, large.PlayerCombat);
        Assert.AreEqual(90, large.PlayerSupply);
        Assert.IsFalse(war.MeasuredLayoutBound);
        Assert.IsFalse(large.MeasuredLayoutBound);
        Assert.IsTrue(HasStructure(war, SkirmishStructureIds.Helipad, false));
        Assert.IsFalse(HasStructure(war, SkirmishStructureIds.Airport, false));
        Assert.IsFalse(HasStructure(large, SkirmishStructureIds.Airport, false));
    }

    [Test]
    public void S004EstablishedAirRejectsArmorAndKeepsGrantedPad()
    {
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        Assert.AreEqual(SkirmishStartPackageId.EstablishedBase, authored.DefinitionS004.StartPackageConfig.Kind);
        Assert.AreEqual(SkirmishReadinessStage.Established, authored.DefinitionS004.StartPackageConfig.Readiness);
        SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
        SkirmishProductionDecision aa = EvaluateAir(
            authored, overlays, SkirmishRoleIds.AntiAir, SkirmishRoleKind.AntiAir, SkirmishReadinessStage.Field, false, false);
        Assert.IsTrue(aa.Accepted);
        Assert.AreEqual(220, aa.MaterialsCost);
        SkirmishProductionDecision tank = EvaluateAir(
            authored, overlays, SkirmishRoleIds.Tank, SkirmishRoleKind.Tank, SkirmishReadinessStage.Established, true, false);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);
        SkirmishProductionDecision heavy = EvaluateAir(
            authored, overlays, SkirmishRoleIds.ApcHeavy, SkirmishRoleKind.ApcHeavy, SkirmishReadinessStage.Established, true, false);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, heavy.Reason);
        SkirmishProductionDecision siege = EvaluateAir(
            authored, overlays, SkirmishRoleIds.Siege, SkirmishRoleKind.Siege, SkirmishReadinessStage.FullArsenal, true, true);
        Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, siege.Reason);
        SkirmishProductionDecision missingPad = EvaluateAir(
            authored, overlays, SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, SkirmishReadinessStage.Established, false, false);
        Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingPad.Reason);
        SkirmishProductionDecision heli = EvaluateAir(
            authored, overlays, SkirmishRoleIds.AttackHeli, SkirmishRoleKind.AttackHeli, SkirmishReadinessStage.Established, true, false);
        Assert.IsTrue(heli.Accepted);
        Assert.AreEqual(420, heli.MaterialsCost);
        SkirmishProductionDecision earlyJet = EvaluateAir(
            authored, overlays, SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter, SkirmishReadinessStage.Established, true, false);
        Assert.AreEqual(SkirmishReasonCode.MissingReadiness, earlyJet.Reason);
        SkirmishProductionDecision jet = EvaluateAir(
            authored, overlays, SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter, SkirmishReadinessStage.FullArsenal, true, false);
        Assert.AreEqual(SkirmishReasonCode.MissingProducer, jet.Reason);
        Assert.AreEqual("producer.airport", jet.Field);
    }

    [Test]
    public void S004AssetsReuseDesertBaseLayoutAndStayInProgress()
    {
        var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset");
        var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/ScenarioSetup_S004.asset");
        var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset");
        var shared = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishLayout_DB_BA.asset");
        var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
            "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
        Assert.IsNotNull(definition);
        Assert.AreEqual("S004", definition.CatalogId);
        Assert.AreEqual("skirmish.s004", definition.DefinitionId);
        Assert.AreEqual(SkirmishArmyProfileId.AirMobile, definition.ArmyProfileConfig.Kind);
        Assert.AreEqual(SkirmishStartPackageId.EstablishedBase, definition.StartPackageConfig.Kind);
        Assert.AreEqual(SkirmishSizeId.Standard, definition.FirstVisitSize);
        Assert.AreEqual(SkirmishSizeId.War, definition.RecommendedSize);
        Assert.AreEqual("layout.skirmish.db.ba", definition.MapLayoutId);
        Assert.AreEqual("skirmish.s004.title", definition.TitleKey);
        Assert.AreEqual("skirmish.s004.brief", definition.BriefingKey);
        Assert.AreEqual("skirmish.s004.objective", definition.ObjectiveKey);
        Assert.AreEqual(shared.LayoutId, layout.LayoutId);
        Assert.AreEqual(shared.WorldWidthMetres, layout.WorldWidthMetres);
        Assert.AreEqual(shared.WorldDepthMetres, layout.WorldDepthMetres);
        Assert.IsTrue(layout.TryGetAnchor("staging.player", out SkirmishLayoutAnchorConfig staging));
        Assert.AreEqual(-192f, staging.WorldX, 0.05f);
        Assert.IsTrue(layout.TryGetAnchor("air.player", out SkirmishLayoutAnchorConfig air));
        Assert.AreEqual(-258f, air.WorldX, 0.2f);
        Assert.AreEqual("scenario.skirmish.s004", scenario.ScenarioId);
        Assert.AreEqual(SkirmishS004FirstVisit.SeedA, scenario.DeterministicSeed);
        Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig s002));
        Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
        Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig s003));
        Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
        Assert.IsTrue(publication.TryGet("S004", out SkirmishPublicationRowConfig s004));
        Assert.AreEqual(SkirmishPublicationStatus.InProgress, s004.Status);
    }

    [Test]
    public void S004CatalogWalkCompilesAirRowsAndLeavesS002OnItsOwnManifest()
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = SkirmishS004FirstVisit.RequiredFeatureIds };
        Assert.IsTrue(SkirmishSetupCompiler.TryCompileCatalog(
            authored, matrix, manifest, out List<SkirmishResolvedSetup> compiled, out List<SkirmishCompileReason> reasons));
        int s003 = 0;
        int s004 = 0;
        for (int i = 0; i < compiled.Count; i++)
        {
            if (compiled[i].CatalogId == "S002")
                Assert.Fail("S002 must not compile against the S004 air manifest.");
            if (compiled[i].CatalogId == "S003")
                s003++;
            if (compiled[i].CatalogId == "S004")
                s004++;
        }

        Assert.AreEqual(3, s003);
        Assert.AreEqual(3, s004);
        bool missingGround = false;
        for (int i = 0; i < reasons.Count; i++)
        {
            if (reasons[i].Code == SkirmishReasonCode.UnsupportedCapability &&
                reasons[i].Detail == "advanced_ground")
                missingGround = true;
        }

        Assert.IsTrue(missingGround);
    }

    [Test]
    public void SessionInitializationProjectsRolesOutsideLiveQuery()
    {
        using var world = new World(nameof(SessionInitializationProjectsRolesOutsideLiveQuery));
        EntityManager em = world.EntityManager;
        CompileS002(SkirmishSizeId.Standard, 104731, out SkirmishResolvedSetup setup, out SkirmishLaunchPayload payload);
        Assert.IsTrue(SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup));
        Entity session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
        Assert.AreEqual(0, em.GetComponentData<SkirmishExpandedSessionComponent>(session).InitializationComplete);

        world.GetOrCreateSystem<SkirmishSessionInitializationSystem>().Update(world.Unmanaged);
        SkirmishExpandedSessionComponent afterInit = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
        Assert.AreEqual(0, afterInit.IsLegacy);
        Assert.AreEqual(1, afterInit.InitializationComplete);
        Assert.AreEqual(SkirmishSessionPhase.Spawning, afterInit.Phase);
        Assert.IsTrue(em.HasComponent<SkirmishObjectiveStateComponent>(session));
        Assert.IsTrue(em.HasComponent<SkirmishCapacityComponent>(session));
        Assert.IsTrue(em.HasComponent<SkirmishEconomyStockComponent>(session));
        Assert.IsTrue(em.HasBuffer<SkirmishProductionReservation>(session));
        Assert.AreEqual(setup.MaterialsEach, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

        world.GetOrCreateSystem<SkirmishScenarioSpawnSystem>().Update(world.Unmanaged);
        SkirmishExpandedSessionComponent afterSpawn = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
        Assert.AreEqual(1, afterSpawn.SpawnComplete);
        Assert.AreEqual(SkirmishSessionPhase.Playing, afterSpawn.Phase);
        Assert.IsTrue(em.HasComponent<SkirmishObjectiveClockComponent>(session));
        Assert.AreEqual(setup.DeadlineSeconds, em.GetComponentData<SkirmishObjectiveClockComponent>(session).DeadlineSeconds);
    }

    [Test]
    public void LibraryDispatchIndexQueuesEstablishedS002()
    {
        using var world = new World(nameof(LibraryDispatchIndexQueuesEstablishedS002));
        EntityManager em = world.EntityManager;
        var config = QuickGameConfig.Defaults;
        config.ScenarioIndex = SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex;
        config.MapSeed = SkirmishAcceptanceCensusCapture.FirstVisitSeed;
        Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em, config));
        Entity sessionEntity = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
        SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
        Assert.AreEqual(0, session.IsLegacy);
        Assert.AreEqual(SkirmishBattleCatalogConfig.DesertBaseEstablishedScenarioId, session.CatalogId.ToString());
        Assert.AreEqual(SkirmishBattleCatalogConfig.DesertBaseEstablishedDefinitionId, session.DefinitionId.ToString());
        Assert.AreEqual(SkirmishDifficultyId.Regular, session.DifficultyId);
        Assert.AreEqual(SkirmishSizeId.Standard, session.SizeId);
        SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(sessionEntity);
        Assert.AreEqual(SkirmishPresetConfig.DesertBaseScenarioIndex, match.ScenarioIndex);
        Assert.AreEqual(SkirmishAcceptanceCensusCapture.FirstVisitSeed, match.Seed);
        SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(sessionEntity).Setup;
        Assert.AreEqual(SkirmishStartPackageId.EstablishedBase, setup.StartPackageId);
        Assert.AreEqual(SkirmishArmyProfileId.GroundManeuver, setup.ArmyProfileId);
        Assert.AreEqual("opmap.skirmish.desert_base_01", setup.OperationMapId);
    }

    [Test]
    public void LibraryDispatchIndexQueuesAirMobileS003()
    {
        using var world = new World(nameof(LibraryDispatchIndexQueuesAirMobileS003));
        EntityManager em = world.EntityManager;
        var config = QuickGameConfig.Defaults;
        config.ScenarioIndex = SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex;
        config.MapSeed = SkirmishS003FirstVisit.SeedA;
        Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em, config));
        Entity sessionEntity = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
        SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
        Assert.AreEqual(0, session.IsLegacy);
        Assert.AreEqual(SkirmishBattleCatalogConfig.DesertBaseAirMobileFieldScenarioId, session.CatalogId.ToString());
        Assert.AreEqual(SkirmishBattleCatalogConfig.DesertBaseAirMobileFieldDefinitionId, session.DefinitionId.ToString());
        Assert.AreEqual(SkirmishDifficultyId.Regular, session.DifficultyId);
        Assert.AreEqual(SkirmishSizeId.Standard, session.SizeId);
        SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(sessionEntity);
        Assert.AreEqual(SkirmishPresetConfig.DesertBaseScenarioIndex, match.ScenarioIndex);
        Assert.AreEqual(SkirmishS003FirstVisit.SeedA, match.Seed);
        SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(sessionEntity).Setup;
        Assert.AreEqual(SkirmishStartPackageId.FieldBase, setup.StartPackageId);
        Assert.AreEqual(SkirmishArmyProfileId.AirMobile, setup.ArmyProfileId);
        Assert.AreEqual("opmap.skirmish.desert_base_01", setup.OperationMapId);
        Assert.AreEqual("skirmish.s003.title", setup.TitleKey);
        Assert.AreEqual("skirmish.s003.objective", setup.ObjectiveKey);
    }

    [Test]
    public void PackagedMatrixMatchesDesignCsv()
    {
        Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out List<SkirmishSetupMatrixRow> packaged, out string packagedError), packagedError);
        LoadMatrix(out List<SkirmishSetupMatrixRow> design);
        Assert.AreEqual(design.Count, packaged.Count, "Packaged matrix must mirror the design CSV row count.");
        for (int i = 0; i < design.Count; i++)
        {
            Assert.AreEqual(design[i].CatalogId, packaged[i].CatalogId);
            Assert.AreEqual(design[i].SizeId, packaged[i].SizeId);
            Assert.AreEqual(design[i].DeadlineSeconds, packaged[i].DeadlineSeconds);
            Assert.AreEqual(design[i].MaterialsEach, packaged[i].MaterialsEach);
            Assert.AreEqual(design[i].InfantryCapEach, packaged[i].InfantryCapEach);
        }
    }

    [Test]
    public void PackagedCatalogResolvesAuthoredDefinitions()
    {
        SkirmishExpansionCatalogConfig catalog = SkirmishExpansionCatalogConfig.Load();
        Assert.IsNotNull(catalog, "Packaged expansion catalog must load from Resources.");
        Assert.IsTrue(catalog.TryValidate(out string error), error);
        Assert.IsTrue(catalog.TryGetDefinition("S002", out SkirmishScenarioDefinitionConfig s002));
        Assert.IsTrue(catalog.TryGetDefinition("S003", out SkirmishScenarioDefinitionConfig s003));
        Assert.IsTrue(catalog.TryGetDefinition("S004", out SkirmishScenarioDefinitionConfig s004));
        Assert.AreEqual("skirmish.s002", s002.DefinitionId);
        Assert.AreEqual("skirmish.s003", s003.DefinitionId);
        Assert.AreEqual("skirmish.s004", s004.DefinitionId);
        SkirmishExpansionAuthoredSet set = catalog.ToAuthoredSet();
        Assert.IsNotNull(set.DefinitionS002);
        Assert.IsNotNull(set.DefinitionS003);
        Assert.IsNotNull(set.DefinitionS004);
        Assert.IsNotNull(set.LayoutDbBa);
        Assert.IsTrue(set.LayoutDbBa.HasWorldBinding, "Packaged layout must carry the pinned world binding.");
    }

    [Test]
    public void WorldBindingProjectsDeploymentOntoMapAnchors()
    {
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        SkirmishMapLayoutConfig layout = authored.LayoutDbBa;
        Assert.IsTrue(layout.HasWorldBinding);
        Assert.IsTrue(layout.TryGetAnchor("base.player", out SkirmishLayoutAnchorConfig player));
        Assert.IsTrue(layout.TryProjectToMap(player.NormalizedU, player.NormalizedV, out float px, out float pz));
        Assert.AreEqual(949f, px, 0.5f);
        Assert.AreEqual(344.7f, pz, 0.5f);
        Assert.IsTrue(layout.TryGetAnchor("base.enemy", out SkirmishLayoutAnchorConfig enemy));
        Assert.IsTrue(layout.TryProjectToMap(enemy.NormalizedU, enemy.NormalizedV, out float ex, out float ez));
        Assert.AreEqual(1686f, ex, 0.5f);
        Assert.AreEqual(108f, ez, 0.5f);

        foreach (SkirmishLayoutAnchorConfig anchor in layout.Anchors)
        {
            Assert.IsTrue(layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z));
            Assert.GreaterOrEqual(x, 0f, anchor.AnchorId);
            Assert.LessOrEqual(x, 2048f, anchor.AnchorId);
            Assert.GreaterOrEqual(z, 0f, anchor.AnchorId);
            Assert.LessOrEqual(z, 1024f, anchor.AnchorId);
        }

        foreach (SkirmishLegalPadConfig pad in layout.Pads)
        {
            Assert.IsTrue(layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float x, out float z));
            Assert.GreaterOrEqual(x - pad.WidthMetres * 0.5f, 0f, pad.PadId);
            Assert.LessOrEqual(x + pad.WidthMetres * 0.5f, 2048f, pad.PadId);
            Assert.GreaterOrEqual(z - pad.DepthMetres * 0.5f, 0f, pad.PadId);
            Assert.LessOrEqual(z + pad.DepthMetres * 0.5f, 1024f, pad.PadId);
        }
    }

    [Test]
    public void WorldBindingMissingFailsValidation()
    {
        // A layout without a pinned world binding must not certify: envelope
        // coordinates alone deploy outside the loaded map's bounds.
        SkirmishMapLayoutConfig layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
        var reasons = new List<SkirmishCompileReason>();
        Assert.IsFalse(SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(layout, reasons));
        Assert.IsTrue(reasons.Exists(r => r.Field == "worldBinding"));
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
            suite.LibraryDispatchIndexQueuesEstablishedS002();
            suite.LibraryDispatchIndexQueuesAirMobileS003();
            suite.PackagedMatrixMatchesDesignCsv();
            suite.PackagedCatalogResolvesAuthoredDefinitions();
            suite.WorldBindingProjectsDeploymentOntoMapAnchors();
            suite.WorldBindingMissingFailsValidation();
            suite.S002GroundOverlaysAndProductionGate();
            suite.S002SpawnProjectsRoleOverlaysAndGroundStaging();
            suite.CompilerAcceptsTypedDesertBaseLayout();
            suite.RegularStandardBindsMeasuredPadsAndRoutes();
            suite.CustomAndLegacyDoNotBindMeasuredLayout();
            suite.ExpandedLaunchResolverCompilesS002();
            suite.S003RegularStandardCompilesFirstVisitSeeds();
            suite.S003FieldSizesMatchMatrixWithoutStartingAir();
            suite.S003AirMobileRejectsArmorAndGatesOffensiveAir();
            suite.S003AssetsReuseDesertBaseLayoutAndStayPlayable();
            suite.S004RegularStandardCompilesFirstVisitSeeds();
            suite.FieldAndEstablishedBackboneMatchesMatrix();
            suite.S004EstablishedSizesMatchMatrixWithStartingAir();
            suite.S004EstablishedAirRejectsArmorAndKeepsGrantedPad();
            suite.S004AssetsReuseDesertBaseLayoutAndStayInProgress();
            suite.S004CatalogWalkCompilesAirRowsAndLeavesS002OnItsOwnManifest();
            suite.SessionInitializationProjectsRolesOutsideLiveQuery();
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

    private static void CompileS003(
        SkirmishSizeId size,
        int seed,
        out SkirmishResolvedSetup setup,
        out SkirmishLaunchPayload payload)
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS003.RequiredFeatureIds };
        Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
            authored.DefinitionS003,
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
            SessionId = "test-s003",
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

    private static void CompileS004(
        SkirmishSizeId size,
        int seed,
        out SkirmishResolvedSetup setup,
        out SkirmishLaunchPayload payload)
    {
        LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
        SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS004.RequiredFeatureIds };
        Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
            authored.DefinitionS004,
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
            SessionId = "test-s004",
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

    private static SkirmishProductionDecision EvaluateAir(
        SkirmishExpansionAuthoredSet authored,
        SkirmishRoleOverlay[] overlays,
        string roleId,
        SkirmishRoleKind kind,
        SkirmishReadinessStage readiness,
        bool helipad,
        bool airport)
    {
        return SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = kind,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true,
                HelipadPresent = helipad,
                AirportPresent = airport
            },
            authored.ArmyAir,
            readiness,
            overlays);
    }

    private static SkirmishProductionDecision EvaluateS003(
        SkirmishExpansionAuthoredSet authored,
        SkirmishRoleOverlay[] overlays,
        string roleId,
        SkirmishRoleKind kind,
        SkirmishReadinessStage readiness,
        bool helipad,
        bool airport)
    {
        return SkirmishProductionEligibility.Evaluate(
            new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = kind,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true,
                HelipadPresent = helipad,
                AirportPresent = airport
            },
            authored.ArmyAir,
            readiness,
            overlays);
    }

    private static void LoadMatrix(out List<SkirmishSetupMatrixRow> matrix)
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out matrix, out string error), error);
    }

    private static int CountStructures(SkirmishResolvedSetup setup, byte faction)
    {
        int total = 0;
        for (int i = 0; i < setup.Structures.Length; i++)
        {
            if (setup.Structures[i].FactionId == faction)
                total++;
        }

        return total;
    }

    private static int CountDesignated(SkirmishResolvedSetup setup, byte faction)
    {
        int total = 0;
        for (int i = 0; i < setup.Structures.Length; i++)
        {
            if (setup.Structures[i].FactionId == faction && setup.Structures[i].DesignatedBase)
                total++;
        }

        return total;
    }

    private static void AssertSeparated(SkirmishResolvedSetup setup)
    {
        for (int i = 0; i < setup.Structures.Length; i++)
        {
            SkirmishResolvedStructureEntry left = setup.Structures[i];
            Assert.Greater(left.SpawnWorldX, 8f);
            Assert.Less(left.SpawnWorldX, 2040f);
            Assert.Greater(left.SpawnWorldZ, 8f);
            Assert.Less(left.SpawnWorldZ, 1016f);
            for (int j = i + 1; j < setup.Structures.Length; j++)
            {
                SkirmishResolvedStructureEntry right = setup.Structures[j];
                if (left.FactionId != right.FactionId)
                    continue;
                float dx = left.SpawnWorldX - right.SpawnWorldX;
                float dz = left.SpawnWorldZ - right.SpawnWorldZ;
                Assert.Greater(dx * dx + dz * dz, 64f);
            }
        }
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

    private static bool AllStartingForcesHavePrefabKeys(SkirmishResolvedSetup setup)
    {
        for (int i = 0; i < setup.Forces.Length; i++)
        {
            if (string.IsNullOrEmpty(setup.Forces[i].RuntimePrefabKey))
                return false;
        }

        return true;
    }

    private static bool TryFindStructure(
        SkirmishResolvedSetup setup,
        byte faction,
        string structureId,
        out SkirmishResolvedStructureEntry structure)
    {
        structure = default;
        for (int i = 0; i < setup.Structures.Length; i++)
        {
            if (setup.Structures[i].FactionId != faction || setup.Structures[i].StructureId != structureId)
                continue;
            structure = setup.Structures[i];
            return true;
        }

        return false;
    }

    private static bool TryFindForce(
        SkirmishResolvedSetup setup,
        byte faction,
        SkirmishRoleKind role,
        out SkirmishResolvedForceEntry force)
    {
        force = default;
        for (int i = 0; i < setup.Forces.Length; i++)
        {
            if (setup.Forces[i].FactionId != faction || setup.Forces[i].RoleKind != role)
                continue;
            force = setup.Forces[i];
            return true;
        }

        return false;
    }

    private static bool HasStructure(SkirmishResolvedSetup setup, string structureId, bool designated)
    {
        for (int i = 0; i < setup.Structures.Length; i++)
        {
            if (setup.Structures[i].StructureId == structureId && setup.Structures[i].DesignatedBase == designated)
                return true;
        }

        return false;
    }

    private static int CountOwnedRole(EntityManager em, byte faction, SkirmishRoleKind role)
    {
        using var query = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent));
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        int total = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction &&
                em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                total++;
        }

        return total;
    }

    private static int FirstOverlayHealth(EntityManager em, SkirmishRoleKind role)
    {
        using var query = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                return em.GetComponentData<UnitHealth>(entities[i]).Current;
        }

        return -1;
    }

    private static int FirstDesignatedBaseHealth(EntityManager em)
    {
        using var query = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(UnitHealth));
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        return entities.Length == 0 ? -1 : em.GetComponentData<UnitHealth>(entities[0]).Current;
    }
}
}
