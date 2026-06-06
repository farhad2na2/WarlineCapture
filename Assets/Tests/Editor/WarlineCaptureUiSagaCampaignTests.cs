using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiSagaCampaignTests
{
    [SetUp]
    public void SetUp()
    {
        WarlineCaptureMissionSession.Clear();
        ClearChapterOneProgress();
    }

    [TearDown]
    public void TearDown()
    {
        WarlineCaptureMissionSession.Clear();
        ClearChapterOneProgress();
    }

    [Test]
    public void SagaMap_HasChapterOneMissionFlowAndRoutes()
    {
        GameObject prefab = LoadPrefab("Screen_SagaMap");
        AssertRoute(prefab, WarlineCaptureRoute.SagaMap);
        AssertText(prefab, "HeaderBar/TitleText", "SAGA MAP");
        AssertText(prefab, "ChapterLabelText", "CHAPTER 01");
        AssertText(prefab, "ChapterTitleText", "FIRST RESPONSE");
        AssertText(prefab, "MapViewport/MissionNodeContainer/Node_1_1/CodeText", "1-1");
        AssertText(prefab, "MapViewport/MissionNodeContainer/Node_1_2/TitleText", ChapterOneMissionCatalog.All[1].DisplayName);
        AssertChapterOneNodeMetadata(prefab);
        Assert.NotNull(prefab.GetComponent<SagaMapScreenSystem>());
        AssertText(prefab, "NodeInfoPanel/SelectedTitleText", "SELECTED: 1-2 ESTABLISH THE BASE");
        AssertText(prefab, "NodeInfoPanel/SelectedStatusText", "AVAILABLE");
        AssertText(prefab, "NodeInfoPanel/SelectedStarsText", "0 / 3 STARS");
        AssertButtonRoute(prefab, "MapViewport/MissionNodeContainer/Node_1_2", WarlineCaptureRoute.MissionBriefing);
        AssertMissionSessionButton(prefab, "MapViewport/MissionNodeContainer/Node_1_2", "saga.ch01.m02.establish_base", false);
        AssertImagePath(prefab, "MapViewport/MapArt", "Assets/Game/Art/UI/Generated/SagaMap/LayeredOneGo/map_viewport_art.png");
    }

    [Test]
    public void SagaMapController_BindsSelectedNodeInfoFromMissionProgress()
    {
        SagaProgressStore.ApplyMissionResult(new MissionResultData(
            ChapterOneMissionCatalog.All[0].MissionId,
            ChapterOneMissionCatalog.All[0].DisplayName,
            true,
            2,
            6,
            0,
            0,
            120,
            System.Array.Empty<ObjectiveRuntimeState>()));

        GameObject instance = Object.Instantiate(LoadPrefab("Screen_SagaMap"));
        try
        {
            SagaMapScreenSystem controller = instance.GetComponent<SagaMapScreenSystem>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            AssertText(instance, "NodeInfoPanel/SelectedTitleText", "SELECTED: 1-2 ESTABLISH THE BASE");
            AssertText(instance, "NodeInfoPanel/SelectedBodyText", "Primary: Build the first operations outpost");
            AssertText(instance, "NodeInfoPanel/SelectedStatusText", "AVAILABLE");
            AssertText(instance, "NodeInfoPanel/SelectedStarsText", "0 / 3 STARS");

            controller.SelectMissionForTests(ChapterOneMissionCatalog.All[0].MissionId);
            AssertText(instance, "NodeInfoPanel/SelectedTitleText", "SELECTED: 1-1 FIRST CONTACT");
            AssertText(instance, "NodeInfoPanel/SelectedBodyText", "Primary: Destroy the forward patrol");
            AssertText(instance, "NodeInfoPanel/SelectedStatusText", "COMPLETED");
            AssertText(instance, "NodeInfoPanel/SelectedStarsText", "2 / 3 STARS");

            controller.SelectMissionForTests(ChapterOneMissionCatalog.All[2].MissionId);
            AssertText(instance, "NodeInfoPanel/SelectedTitleText", "SELECTED: 1-3 RADAR WARNING");
            AssertText(instance, "NodeInfoPanel/SelectedStatusText", "Complete 1-2 to unlock.");
            AssertText(instance, "NodeInfoPanel/SelectedStarsText", "0 / 3 STARS");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void SagaMapController_UnlocksNextNodeFromSagaProgressAndStartsMission()
    {
        SagaProgressStore.ApplyMissionResult(new MissionResultData(
            ChapterOneMissionCatalog.All[1].MissionId,
            ChapterOneMissionCatalog.All[1].DisplayName,
            true,
            3,
            8,
            0,
            2,
            160,
            System.Array.Empty<ObjectiveRuntimeState>()));

        GameObject instance = Object.Instantiate(LoadPrefab("Screen_SagaMap"));
        try
        {
            SagaMapScreenSystem controller = instance.GetComponent<SagaMapScreenSystem>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            Transform node13 = instance.transform.Find("MapViewport/MissionNodeContainer/Node_1_3");
            Assert.NotNull(node13);
            Assert.IsTrue(node13.GetComponent<Button>().interactable);
            Assert.IsFalse(node13.Find("LockIcon").gameObject.activeSelf);
            Assert.IsTrue(node13.Find("StarIcon").gameObject.activeSelf);

            Transform node14 = instance.transform.Find("MapViewport/MissionNodeContainer/Node_1_4");
            Assert.NotNull(node14);
            Assert.IsTrue(node14.Find("LockIcon").gameObject.activeSelf);
            Assert.IsFalse(node14.Find("StarIcon").gameObject.activeSelf);

            node13.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(WarlineCaptureMissionSession.HasActiveMission);
            Assert.AreEqual(ChapterOneMissionCatalog.All[2].MissionId, WarlineCaptureMissionSession.ActiveMission.MissionId);
            AssertText(instance, "NodeInfoPanel/SelectedTitleText", "SELECTED: 1-3 RADAR WARNING");
            AssertText(instance, "NodeInfoPanel/SelectedStatusText", "AVAILABLE");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MissionBriefing_HasBreachAssaultContentAndStartRoute()
    {
        GameObject prefab = LoadPrefab("Screen_MissionBriefing");
        AssertRoute(prefab, WarlineCaptureRoute.MissionBriefing);
        AssertText(prefab, "HeaderBar/TitleText", "MISSION BRIEFING");
        AssertText(prefab, "MissionTitleText", "1-5 BREACH ASSAULT");
        AssertText(prefab, "ScenarioText", "SCENARIO: PORT BREACH  |  MAP: FG-L02 PORT BREACH");
        AssertText(prefab, "ObjectivePanel/TitleText", "PRIMARY OBJECTIVES");
        AssertText(prefab, "StarGoalsPanel/TitleText", "STAR GOALS");
        AssertText(prefab, "EnemyIntelPanel/TitleText", "ENEMY INTEL");
        AssertText(prefab, "RewardPanel/CreditsReward/ValueText", "+1,200");
        AssertButtonRoute(prefab, "StartMissionButton", WarlineCaptureRoute.LoadoutSquadPrep);
        AssertMissionSessionButton(prefab, "StartMissionButton", "saga.ch01.m05.breach_assault", false);
        AssertImagePath(prefab, "MissionImagePanel/MissionKeyArt", "Assets/Game/Art/UI/Generated/MissionBriefing/LayeredOneGo/mission_key_art.png");
    }

    [Test]
    public void MissionBriefing_BindsSelectedMissionObjectivesAndRewardConfigPreview()
    {
        WarlineCaptureMissionSession.BeginMission("saga.ch01.m02.establish_base", WarlineCaptureRoute.SagaMap);
        GameObject instance = Object.Instantiate(LoadPrefab("Screen_MissionBriefing"));
        try
        {
            MissionBriefingScreenSystem controller = instance.GetComponent<MissionBriefingScreenSystem>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            AssertText(instance, "MissionTitleText", "1-2 ESTABLISH THE BASE");
            AssertText(instance, "ObjectivePanel/Row_1_Text", "Build the first operations outpost");
            AssertText(instance, "ObjectivePanel/Row_2_Text", "Defeat the first attack group");
            AssertText(instance, "StarGoalsPanel/Row_1_Text", "Build two support structures");
            AssertText(instance, "RewardPanel/CommanderXpReward/ValueText", "+220");
            AssertText(instance, "RewardPanel/CreditsReward/ValueText", "+1,200");
            AssertText(instance, "RewardPanel/GearReward/LabelText", "BUILDING UNLOCK");
            AssertText(instance, "RewardPanel/GearReward/ValueText", "BARRACK");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MissionBriefing_BindsOperationRewardPreviewLabels()
    {
        MissionConfig mission = new MissionConfig(
            "test.operation.reward.preview",
            "Operation Reward Preview",
            System.Array.Empty<ObjectiveConfig>(),
            System.Array.Empty<StarGoalConfig>(),
            new[]
            {
                new RewardConfig(
                    "operation_preview",
                    "Operation Preview",
                    new[]
                    {
                        new RewardItemConfig(RewardType.OperationSupply, 2),
                        new RewardItemConfig(RewardType.OperationTrust, 3, "old_market"),
                        new RewardItemConfig(RewardType.OperationInfrastructure, 4, "port_breach")
                    })
            });
        WarlineCaptureMissionSession.BeginMissionForTests(mission, WarlineCaptureRoute.OperationDashboard);

        GameObject instance = Object.Instantiate(LoadPrefab("Screen_MissionBriefing"));
        try
        {
            MissionBriefingScreenSystem controller = instance.GetComponent<MissionBriefingScreenSystem>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            AssertText(instance, "MissionTitleText", "1-? OPERATION REWARD PREVIEW");
            AssertText(instance, "RewardPanel/CommanderXpReward/LabelText", "OPERATION SUPPLY");
            AssertText(instance, "RewardPanel/CommanderXpReward/ValueText", "+2");
            AssertText(instance, "RewardPanel/CreditsReward/LabelText", "TRUST");
            AssertText(instance, "RewardPanel/CreditsReward/ValueText", "+3 OLD MARKET");
            AssertText(instance, "RewardPanel/GearReward/LabelText", "INFRASTRUCTURE");
            AssertText(instance, "RewardPanel/GearReward/ValueText", "+4 PORT BREACH");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MissionBriefing_PrioritizesOperationRewardsForOperationLaunch()
    {
        WarlineCaptureMissionSession.BeginMission("saga.ch01.m05.breach_assault", WarlineCaptureRoute.OperationDashboard);
        GameObject instance = Object.Instantiate(LoadPrefab("Screen_MissionBriefing"));
        try
        {
            MissionBriefingScreenSystem controller = instance.GetComponent<MissionBriefingScreenSystem>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            AssertText(instance, "MissionTitleText", "1-5 BREACH ASSAULT");
            AssertText(instance, "RewardPanel/CommanderXpReward/LabelText", "OPERATION SUPPLY");
            AssertText(instance, "RewardPanel/CommanderXpReward/ValueText", "+1");
            AssertText(instance, "RewardPanel/CreditsReward/LabelText", "SECURITY");
            AssertText(instance, "RewardPanel/CreditsReward/ValueText", "+4 PORT BREACH");
            AssertText(instance, "RewardPanel/GearReward/LabelText", "INFRASTRUCTURE");
            AssertText(instance, "RewardPanel/GearReward/ValueText", "+5 PORT BREACH");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void LoadoutSquadPrep_HasSelectedUnitsSupportGearAndDeployRoute()
    {
        GameObject prefab = LoadPrefab("Screen_LoadoutSquadPrep");
        AssertRoute(prefab, WarlineCaptureRoute.LoadoutSquadPrep);
        AssertText(prefab, "HeaderBar/TitleText", "LOADOUT / SQUAD PREP");
        AssertText(prefab, "HeaderBar/PowerRecommended/ValueText", "55,000");
        AssertText(prefab, "SelectedUnitsPanel/SectionTitleText", "SELECTED UNITS");
        AssertText(prefab, "SelectedUnitsPanel/RifleSquadCard/TitleText", "RIFLE SQUAD");
        AssertText(prefab, "SelectedUnitsPanel/ApcCard/PowerText", "12,600");
        AssertText(prefab, "SupportSlotsPanel/SectionTitleText", "SUPPORT SLOTS");
        AssertText(prefab, "SupportSlotsPanel/AirstrikeSlot/TitleText", "AIRSTRIKE");
        AssertText(prefab, "RecommendedGearPanel/SectionTitleText", "RECOMMENDED GEAR");
        AssertText(prefab, "RecommendedGearPanel/ArmorPlateCard/ValueText", "+12%");
        AssertText(prefab, "MissionSummaryPanel/SectionTitleText", "MISSION SUMMARY");
        AssertText(prefab, "MissionSummaryPanel/EnemyRatingPanel/EnemyPowerValueText", "58,200");
        AssertText(prefab, "DeployButton/LabelText", "DEPLOY  10");
        AssertButtonRoute(prefab, "DeployButton", WarlineCaptureRoute.Match);
        AssertMissionSessionButton(prefab, "DeployButton", "saga.ch01.m05.breach_assault", true);
        AssertImagePath(prefab, "SelectedUnitsPanel/RifleSquadCard/ArtImage", "Assets/Game/Art/UI/Generated/Loadout/LayeredOneGo/art_rifle_squad.png");
        AssertImagePath(prefab, "DeployButton", "Assets/Game/Art/UI/Generated/Loadout/LayeredOneGo/deploy_button_background.png");
    }

    private static GameObject LoadPrefab(string name)
    {
        string path = $"Assets/Game/Prefabs/UI/Screens/{name}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, path);
        return prefab;
    }

    private static void AssertRoute(GameObject prefab, WarlineCaptureRoute expected)
    {
        WarlineCaptureScreenSystem controller = prefab.GetComponent<WarlineCaptureScreenSystem>();
        Assert.NotNull(controller);
        Assert.AreEqual(expected, controller.Route);
    }

    private static void AssertText(GameObject prefab, string path, string expected)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expected, text.text, path);
    }

    private static void AssertButtonRoute(GameObject prefab, string path, WarlineCaptureRoute expected)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Button button = target.GetComponent<Button>();
        Assert.NotNull(button, path);
        ScreenRouteSystem route = target.GetComponent<ScreenRouteSystem>();
        Assert.NotNull(route, path);
        var serialized = new SerializedObject(route);
        Assert.AreEqual((int)expected, serialized.FindProperty("route").enumValueIndex, path);
    }

    private static void AssertMissionSessionButton(GameObject prefab, string path, string expectedMissionId, bool expectedLaunch)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        WarlineCaptureMissionSessionSystem sessionButton = target.GetComponent<WarlineCaptureMissionSessionSystem>();
        Assert.NotNull(sessionButton, path);
        var serialized = new SerializedObject(sessionButton);
        Assert.AreEqual(expectedMissionId, serialized.FindProperty("missionId").stringValue, path);
        Assert.AreEqual((int)WarlineCaptureRoute.SagaMap, serialized.FindProperty("returnRoute").enumValueIndex, path);
        Assert.AreEqual(expectedLaunch, serialized.FindProperty("launchExistingGameplay").boolValue, path);
    }

    private static void AssertChapterOneNodeMetadata(GameObject prefab)
    {
        for (int i = 0; i < ChapterOneMissionCatalog.All.Count; i++)
        {
            int missionNumber = i + 1;
            string path = $"MapViewport/MissionNodeContainer/Node_1_{missionNumber}";
            MissionConfig mission = ChapterOneMissionCatalog.All[i];
            Transform target = prefab.transform.Find(path);
            Assert.NotNull(target, path);

            AssertText(prefab, $"{path}/TitleText", mission.DisplayName);
            WarlineCaptureSagaMissionNodeMetadata metadata = target.GetComponent<WarlineCaptureSagaMissionNodeMetadata>();
            Assert.NotNull(metadata, path);
            Assert.AreEqual(mission.MissionId, metadata.MissionId, path);
            Assert.AreEqual(1, metadata.ChapterIndex, path);
            Assert.AreEqual(missionNumber, metadata.MissionIndex, path);
            Assert.AreEqual(missionNumber >= 3, metadata.Locked, path);
            if (metadata.Locked)
                Assert.IsNotEmpty(metadata.LockedReason, path);
        }
    }

    private static void AssertImagePath(GameObject prefab, string path, string expected)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual(expected, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void ClearChapterOneProgress()
    {
        foreach (MissionConfig mission in ChapterOneMissionCatalog.All)
            SagaProgressStore.ClearMission(mission.MissionId);
    }
}
