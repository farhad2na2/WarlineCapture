#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseHudResultTests
{
    private const string Marker =
        "[M02EstablishBaseHudResultValidation] result=Passed tests=8";
    private const string MissionId = "saga.ch01.m02.establish_base";

    [MenuItem("Game/Validation/Run M02 Establish Base HUD Result Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseHudResultTests tests = new();
            tests.UnsettledVictoryDoesNotFlashBeforeDebrief();
            tests.FirstClearResultWaitsForDebriefBeforeVictory();
            tests.FirstClearFinalResultCarriesM03RevealTruth();
            tests.ReplayDebriefPrecedesVictoryWithoutRepeatingFirstClearRewards();
            tests.FinalVictoryButtonReturnsToMenu();
            tests.DebriefOwnerReturnsToFinalResultBeforeMenu();
            tests.ResultPopupHidesLegacyM01Identity();
            tests.ConstructionResultsUseMissionFactsAndRestoreCombatLabels();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseHudResultValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base HUD Result Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M02EstablishBaseNarrativeTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseResultSettlementTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseCampaignUiTests.RunFocusedValidation);
            RunValidation(M01FirstContactHudResultTests.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log(
                "[M02EstablishBaseHudResultRegressionValidation] result=Passed suites=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[M02EstablishBaseHudResultRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ConstructionResultsUseMissionFactsAndRestoreCombatLabels()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab");
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var view = instance.GetComponent<MissionResultPopupView>();
            var patrol = Find(instance, "Objective_DestroyHostilePatrol", "Label");
            var squad = Find(instance, "Objective_KeepCommandSquadAlive", "Label");
            string originalPatrol = patrol.text, originalSquad = squad.text;
            var model = new UiMissionResultPopupModel(1, MissionId, UiMissionResultOutcome.Loss,
                "MISSION FAILED", "ESTABLISH THE BASE", "", 0, "00:20", "0", "0/0", "", "RETRY", true, true,
                construction: new UiMissionConstructionResultDetails(1, 0, 2));
            view.Apply(in model);
            Assert.AreNotEqual(originalPatrol, patrol.text);
            Assert.AreNotEqual(originalSquad, squad.text);
            Assert.AreEqual("MISSION FAILED", instance.GetComponentsInChildren<TMP_Text>(true)
                .First(t => t.name == "MissionStatusText").text);
            Assert.AreEqual("2", Find(instance, "CiviliansLostCard", "ValueText").text);
            Assert.AreEqual("0", Find(instance, "EnemiesDefeatedCard", "ValueText").text);
            var combat = new UiMissionResultPopupModel(2, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Victory,
                "VICTORY", "FIRST CONTACT", "", 3, "00:40", "0", "3/3", "", "CONTINUE", true, false);
            view.Apply(in combat);
            Assert.AreEqual(originalPatrol, patrol.text, "M2 must not leak its objectives into another mission.");
            Assert.AreEqual(originalSquad, squad.text);
            Assert.AreEqual("3/3", Find(instance, "EnemiesDefeatedCard", "ValueText").text);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    private static TMP_Text Find(GameObject root, string row, string name)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == name && text.transform.parent.name == row) return text;
        Assert.Fail("Missing result row " + row + "/" + name); return null;
    }

    public static void ValidateConstructionResults()
    {
        new M02EstablishBaseHudResultTests().ConstructionResultsUseMissionFactsAndRestoreCombatLabels();
        Debug.Log("[M02ConstructionResults] result=Passed partial construction facts and combat-label restoration");
    }

    public static void CaptureConstructionResults()
    {
        string oldLocale = Game.Configs.GameLocalization.CurrentLocaleCode;
        try
        {
            foreach (string locale in new[] { "en", "fa-IR" })
            {
                Game.Configs.GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<Game.Configs.GameLocalizationCatalog>(
                    Game.Editor.V3UiLocalizationCatalogBuilder.CatalogPath), locale, false);
                Game.Editor.V3UiLocalizationCatalogBuilder.ValidateConfiguredCatalogEntries(
                    AssetDatabase.LoadAssetAtPath<Game.Configs.GameLocalizationCatalog>(Game.Editor.V3UiLocalizationCatalogBuilder.CatalogPath));
                foreach (bool victory in new[] { true, false })
                {
                    var model = new UiMissionResultPopupModel(1, MissionId,
                        victory ? UiMissionResultOutcome.Victory : UiMissionResultOutcome.Loss,
                        victory ? "VICTORY" : "MISSION FAILED", "ESTABLISH THE BASE • FORWARD POST",
                        Game.Configs.GameText.Get(victory ? "mission.m02.result.operational" : "mission.m02.result.incomplete"), victory ? (byte)3 : (byte)0,
                        "00:49", "0", "0/0", victory ? "320 COMMANDER XP  ·  1500 CREDITS" : "NO REWARD",
                        victory ? "CONTINUE" : "RETRY", true, !victory,
                        construction: new UiMissionConstructionResultDetails(1, victory ? 1 : 0, 0));
                    typeof(Game.Editor.MissionResultV3PrefabBuilder).GetMethod("Capture",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(null, new object[] { $"/private/tmp/readiness-m02-result-{locale}-{victory}.png", 1920, 1080, model });
                }
            }
            Debug.Log("[M02ConstructionResultCaptures] result=Passed languages=EN,FA victory and partial failure");
        }
        finally { Game.Configs.GameLocalization.SetLocale(oldLocale, false); }
    }

    [Test]
    public void UnsettledVictoryDoesNotFlashBeforeDebrief()
    {
        Assert.IsFalse(TryReadModel(
            firstClear: true,
            MissionPhaseKind.Result,
            out _,
            settlementAccepted: false));
    }

    [Test]
    public void FirstClearResultWaitsForDebriefBeforeVictory()
    {
        Assert.IsFalse(TryReadModel(
            firstClear: true,
            MissionPhaseKind.Result,
            out _));
    }

    [Test]
    public void FirstClearFinalResultCarriesM03RevealTruth()
    {
        UiMissionResultPopupModel model = ReadModel(
            firstClear: true,
            MissionPhaseKind.ResultAfterDebrief);
        Assert.AreEqual("ESTABLISH THE BASE • FORWARD POST", model.Subtitle);
        StringAssert.Contains("Dalia Rahim accepts field-lead duty", model.SummaryBody);
        StringAssert.Contains("warning sector has gone dark", model.SummaryBody);
        StringAssert.Contains("BARRACKS UNLOCK", model.RewardsText);
        Assert.AreEqual("CONTINUE", model.PrimaryActionLabel);
        Assert.IsTrue(model.FirstClear);
        Assert.IsFalse(model.DebriefRequired);
    }

    [Test]
    public void ReplayDebriefPrecedesVictoryWithoutRepeatingFirstClearRewards()
    {
        Assert.IsFalse(TryReadModel(
            firstClear: false,
            MissionPhaseKind.Result,
            out _));
        UiMissionResultPopupModel model = ReadModel(
            firstClear: false,
            MissionPhaseKind.ResultAfterDebrief);
        Assert.AreEqual("CONTINUE", model.PrimaryActionLabel);
        Assert.AreEqual("300 CREDITS", model.RewardsText);
        StringAssert.DoesNotContain("field-lead", model.SummaryBody);
        Assert.IsFalse(model.FirstClear);
        Assert.IsFalse(model.DebriefRequired);
    }

    [Test]
    public void FinalVictoryButtonReturnsToMenu()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/UI/Screens/CampaignMissionHudResultBinderView.cs");
        StringAssert.Contains("!activeModel.DebriefRequired", source);
    }

    [Test]
    public void DebriefOwnerReturnsToFinalResultBeforeMenu()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/Composition/Narrative/CampaignMissionDebriefCompositionSystemHelper.cs");
        StringAssert.Contains("MissionPhaseKind.DebriefFirstClear", source);
        StringAssert.Contains("TryCompleteDebrief", source);
        StringAssert.Contains("ResultAfterDebrief", File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeProgressUtility.cs"));
        StringAssert.DoesNotContain("UiShellRouteIntent.ReturnToMainMenu", source);
        StringAssert.Contains("DebriefSequenceId", source);
    }

    [Test]
    public void ResultPopupHidesLegacyM01Identity()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab");
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            UiMissionResultPopupModel model = ReadModel(
                firstClear: true,
                MissionPhaseKind.ResultAfterDebrief);
            MissionResultPopupView view = instance.GetComponent<MissionResultPopupView>();
            Assert.NotNull(view);
            view.Apply(in model);

            Transform legacyIdentity = Find(instance.transform, "MissionIdentityBlock");
            Assert.NotNull(legacyIdentity);
            Assert.IsFalse(legacyIdentity.gameObject.activeSelf,
                "The shared result popup must not expose its authored M01 placeholder during M02.");

            TMP_Text missionName = Find(instance.transform, "MissionNameText")?.GetComponent<TMP_Text>();
            Assert.NotNull(missionName);
            Assert.AreEqual("ESTABLISH THE BASE • FORWARD POST", missionName.text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static UiMissionResultPopupModel ReadModel(
        bool firstClear,
        MissionPhaseKind phase)
    {
        Assert.IsTrue(TryReadModel(firstClear, phase, out UiMissionResultPopupModel model));
        return model;
    }

    private static bool TryReadModel(
        bool firstClear,
        MissionPhaseKind phase,
        out UiMissionResultPopupModel model,
        bool settlementAccepted = true)
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = new("M02 HUD result");
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = CreateCatalog();
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity root = entityManager.CreateEntity(typeof(CampaignMissionRootComponent));
            FixedString64Bytes missionId = new(MissionId);
            FixedString64Bytes session = new("m02-hud-result");
            entityManager.AddComponentData(root, new CampaignMissionRuntimeComponent
            {
                MissionId = missionId,
                ScenarioId = new FixedString64Bytes("scenario.ch01.m02.establish_base"),
                OperationMapId = new FixedString64Bytes("opmap.ch01.forward_post_01"),
                SessionToken = session,
                Phase = phase,
                Outcome = MissionOutcomeKind.Victory,
                LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
                RunKind = firstClear ? MissionRunKind.FirstClear : MissionRunKind.Replay,
                ReturnDestination = MissionReturnDestinationKind.CampaignOperations,
                Version = 8,
                SourceVersion = 3,
                AttemptOrdinal = 1,
                DeterministicSeed = 2002001
            });
            entityManager.AddComponentData(root, new CampaignMissionResultComponent
            {
                MissionId = missionId,
                SessionToken = session,
                AttemptOrdinal = 1,
                SourceVersion = 8,
                Outcome = MissionOutcomeKind.Victory,
                ReturnDestination = MissionReturnDestinationKind.CampaignOperations,
                Stars = 3,
                ElapsedMilliseconds = 210000
            });
            entityManager.AddComponentData(root, new CampaignMissionAttemptFactsComponent
            {
                HostileTotalCount = 0,
                HostileDefeatedCount = 0
            });
            entityManager.AddComponentData(root, new CampaignMissionCatalogComponent
            {
                Blob = blob,
                SourceVersion = 3,
                OwnsBlob = 0
            });
            DynamicBuffer<CampaignMissionSettlementResultElement> settlements =
                entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
            if (settlementAccepted)
            {
                settlements.Add(new()
                {
                    SourceVersion = 8,
                    SessionToken = session,
                    Accepted = 1,
                    FirstClear = firstClear ? (byte)1 : (byte)0
                });
            }
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            return UiShellEcsGateway.TryReadMissionResult(out model);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateCatalog()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions =
            builder.Allocate(ref catalog.Missions, 1);
        ref CampaignMissionDefinitionBlob definition = ref missions[0];
        definition.MissionId = new FixedString64Bytes(MissionId);
        definition.ScenarioId = new FixedString64Bytes("scenario.ch01.m02.establish_base");
        definition.OperationMapId = new FixedString64Bytes("opmap.ch01.forward_post_01");
        definition.DebriefSequenceId = new FixedString64Bytes("seq.ch01.m02.debrief");
        BlobBuilderArray<CampaignMissionRewardBlob> first =
            builder.Allocate(ref definition.FirstClearRewards, 3);
        first[0] = Reward(MissionRewardKind.None, "reward.commander_xp", 320);
        first[1] = Reward(MissionRewardKind.Credits, string.Empty, 1500);
        first[2] = Reward(MissionRewardKind.None, "reward.ch01.m02.production_unlock", 1);
        BlobBuilderArray<CampaignMissionRewardBlob> replay =
            builder.Allocate(ref definition.ReplayRewards, 1);
        replay[0] = Reward(MissionRewardKind.Credits, string.Empty, 300);
        BlobAssetReference<CampaignMissionCatalogBlob> blob =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return blob;
    }

    private static CampaignMissionRewardBlob Reward(
        MissionRewardKind kind,
        string id,
        int amount) => new()
    {
        Kind = kind,
        RewardConfigId = new FixedString64Bytes(id),
        Amount = amount
    };

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            if (candidate.name == name)
                return candidate;
        return null;
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }
}
#endif
