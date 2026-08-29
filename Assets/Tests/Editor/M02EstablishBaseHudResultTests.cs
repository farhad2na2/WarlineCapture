#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
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
        "[M02EstablishBaseHudResultValidation] result=Passed tests=7";
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
            tests.ReplayResultReturnsWithoutRepeatingFirstClearRewards();
            tests.FinalVictoryButtonReturnsToMenu();
            tests.DebriefOwnerReturnsToFinalResultBeforeMenu();
            tests.ResultPopupHidesLegacyM01Identity();
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
    public void ReplayResultReturnsWithoutRepeatingFirstClearRewards()
    {
        UiMissionResultPopupModel model = ReadModel(
            firstClear: false,
            MissionPhaseKind.Result);
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
            "Assets/Game/Scripts/UI/Screens/CampaignMissionHudResultBinder.cs");
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
