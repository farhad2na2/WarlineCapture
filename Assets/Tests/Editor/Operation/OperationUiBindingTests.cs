using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OperationUiBindingTests
{
    private const string DashboardPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_OperationDashboard.prefab";
    private const string DistrictPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_DistrictDetail.prefab";
    private const string InboxPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Inbox.prefab";
    private const string EventsPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Events.prefab";
    private const string CommandFeedPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_CommandFeed.prefab";
    private const string ShellPrefabPath = "Assets/Game/Prefabs/UI/Shell/WarlineCaptureAppCanvas.prefab";

    [SetUp]
    public void SetUp()
    {
        WarlineCaptureOperationRuntime.ResetForTests();
        new ActiveMissionSession().Clear();
    }

    [TearDown]
    public void TearDown()
    {
        new ActiveMissionSession().Clear();
        WarlineCaptureOperationRuntime.ResetForTests();
    }

    [Test]
    public void OperationDashboard_BindsServiceStateAndSelectsDistrictCards()
    {
        GameObject root = InstantiatePrefab(DashboardPrefabPath);
        try
        {
            OperationDashboardScreenSystem controller = root.GetComponent<OperationDashboardScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();

            Assert.AreEqual("DAY 1 CITY PRESSURE", Text(root, "HeroPanel/HeroTitleText"));
            Assert.AreEqual("NORTH BRIDGE", Text(root, "StatusCard_1/TitleText"));
            Assert.AreEqual("THREAT 68 / HEAT 42 / RISK 52", Text(root, "StatusCard_1/StatusText"));
            StringAssert.Contains("Trust 54", Text(root, "StatusCard_1/BodyText"));
            StringAssert.Contains("Infra 57", Text(root, "StatusCard_1/BodyText"));
            StringAssert.Contains("Influence 68", Text(root, "StatusCard_1/BodyText"));
            StringAssert.Contains("Civilian Risk 52", Text(root, "StatusCard_1/BodyText"));

            WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Scan);
            controller.RefreshForTests();

            StringAssert.Contains("Supplies 3", Text(root, "HeroPanel/BodyText"));
            Assert.AreEqual("EVENT", Text(root, "FeedRow_1/TagText"));
            StringAssert.Contains("Drone Scan", Text(root, "FeedRow_1/BodyText"));

            root.transform.Find("StatusCard_3").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual("port_breach", WarlineCaptureOperationRuntime.SelectedDistrictId);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DistrictDetail_ActionsMutateOperationStateAndRaidSeedsMissionBriefing()
    {
        WarlineCaptureOperationRuntime.SelectDistrict("north_bridge");
        GameObject root = InstantiatePrefab(DistrictPrefabPath);
        try
        {
            DistrictDetailScreenSystem controller = root.GetComponent<DistrictDetailScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();
            Assert.AreEqual("NORTH BRIDGE", Text(root, "HeroPanel/HeroTitleText"));

            root.transform.Find("StatusCard_2").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(44, WarlineCaptureOperationRuntime.SelectedDistrict.intel);
            StringAssert.Contains("Heat 43", Text(root, "HeroPanel/BodyText"));
            StringAssert.Contains("Civilian Risk 52", Text(root, "HeroPanel/BodyText"));

            root.transform.Find("StatusCard_3").GetComponent<Button>().onClick.Invoke();

            Assert.IsTrue(new ActiveMissionSession().HasActiveMission);
            Assert.AreEqual("saga.ch01.m05.breach_assault", new ActiveMissionSession().ActiveMission.MissionId);
            Assert.AreEqual(WarlineCaptureRoute.OperationDashboard, new ActiveMissionSession().ReturnRoute);
            Assert.AreEqual(54, WarlineCaptureOperationRuntime.SelectedDistrict.threat);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DistrictDetail_ExpandedSupportActionsMutateSecondaryMetrics()
    {
        WarlineCaptureOperationRuntime.SelectDistrict("port_breach");
        GameObject root = InstantiatePrefab(DistrictPrefabPath);
        try
        {
            DistrictDetailScreenSystem controller = root.GetComponent<DistrictDetailScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();
            Assert.AreEqual("REPAIR", Text(root, "StatusCard_4/TitleText"));
            Assert.AreEqual("EVACUATE", Text(root, "StatusCard_5/TitleText"));
            Assert.AreEqual("BUILD OUTPOST", Text(root, "StatusCard_6/TitleText"));

            root.transform.Find("StatusCard_4").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(58, WarlineCaptureOperationRuntime.SelectedDistrict.infrastructure);
            Assert.AreEqual(58, WarlineCaptureOperationRuntime.SelectedDistrict.heat);
            StringAssert.Contains("Supplies 3", Text(root, "HeroPanel/BodyText"));
            StringAssert.Contains("Infra 58", Text(root, "HeroPanel/BodyText"));
            StringAssert.Contains("Influence 82", Text(root, "FeedRow_2/BodyText"));

            root.transform.Find("StatusCard_5").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(45, WarlineCaptureOperationRuntime.SelectedDistrict.civilianRisk);
            Assert.AreEqual(61, WarlineCaptureOperationRuntime.SelectedDistrict.heat);
            StringAssert.Contains("Civilian Risk 45", Text(root, "HeroPanel/BodyText"));

            root.transform.Find("StatusCard_6").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(41, WarlineCaptureOperationRuntime.SelectedDistrict.security);
            Assert.AreEqual(74, WarlineCaptureOperationRuntime.SelectedDistrict.enemyInfluence);
            Assert.AreEqual(0, WarlineCaptureOperationRuntime.State.operationSupplies);
            StringAssert.Contains("Security 41", Text(root, "HeroPanel/BodyText"));
            StringAssert.Contains("Influence 74", Text(root, "FeedRow_2/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OperationShell_ScanAndRaidUseModalPopupFlow()
    {
        GameObject root = InstantiatePrefab(ShellPrefabPath);
        try
        {
            WarlineCaptureRouter router = root.GetComponent<WarlineCaptureRouter>();
            Assert.NotNull(router);

            router.GoTo(WarlineCaptureRoute.OperationDashboard, false);
            Transform contentRoot = router.ContentRoot;
            Transform dashboard = contentRoot.Find("Screen_OperationDashboard");
            dashboard.GetComponent<OperationDashboardScreenSystem>().RefreshForTests();
            dashboard.Find("StatusCard_1").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(WarlineCaptureRoute.DistrictDetail, router.ActiveRoute);
            Transform district = contentRoot.Find("Screen_DistrictDetail");
            district.GetComponent<DistrictDetailScreenSystem>().RefreshForTests();
            district.Find("StatusCard_2").GetComponent<Button>().onClick.Invoke();

            Transform modalOverlay = root.transform.Find("SafeAreaRoot/ModalOverlay");
            Assert.IsTrue(modalOverlay.gameObject.activeSelf);
            Transform intelPopup = modalOverlay.Find("IntelRevealPopup(Clone)");
            Assert.NotNull(intelPopup);
            StringAssert.Contains("Confidence raised to 44", intelPopup.Find("Frame/BodyRoot/SubheadingText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("NORTH BRIDGE INTEL SWEEP", intelPopup.Find("Frame/BodyRoot/CargoManifestCard/TitleText").GetComponent<TMP_Text>().text);
            Assert.AreEqual(1, WarlineCaptureOperationRuntime.UnreadEvidenceCount("north_bridge"));

            modalOverlay.Find("IntelRevealPopup(Clone)/Frame/ButtonRow/ViewIntelButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(modalOverlay.gameObject.activeSelf);
            Assert.AreEqual(0, WarlineCaptureOperationRuntime.UnreadEvidenceCount("north_bridge"));

            district.Find("StatusCard_3").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(modalOverlay.gameObject.activeSelf);
            Transform confirmPopup = modalOverlay.Find("ConfirmRaidPopup(Clone)");
            Assert.NotNull(confirmPopup);
            Assert.AreEqual("Threat 68 / Heat 43", confirmPopup.Find("Frame/BodyRoot/TargetPanel/TargetInfoCard/ThreatText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("LOW", confirmPopup.Find("Frame/BodyRoot/RiskPanel/CollateralRiskRow/ValueText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("ELEVATED", confirmPopup.Find("Frame/BodyRoot/RiskPanel/CivilianDensityRow/ValueText").GetComponent<TMP_Text>().text);
            StringAssert.Contains("Security 32", confirmPopup.Find("Frame/BodyRoot/RiskPanel/WarningTextPanel/WarningText").GetComponent<TMP_Text>().text);

            confirmPopup.Find("Frame/ButtonRow/ConfirmButton").GetComponent<Button>().onClick.Invoke();

            Assert.IsTrue(new ActiveMissionSession().HasActiveMission);
            Assert.AreEqual("saga.ch01.m05.breach_assault", new ActiveMissionSession().ActiveMission.MissionId);
            Assert.AreEqual(WarlineCaptureRoute.MissionBriefing, router.ActiveRoute);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OperationShell_EndDayReportBindsSecondaryMetrics()
    {
        GameObject root = InstantiatePrefab(ShellPrefabPath);
        try
        {
            WarlineCaptureRouter router = root.GetComponent<WarlineCaptureRouter>();
            Assert.NotNull(router);

            router.GoTo(WarlineCaptureRoute.OperationDashboard, false);
            Transform dashboard = router.ContentRoot.Find("Screen_OperationDashboard");
            dashboard.GetComponent<OperationDashboardScreenSystem>().RefreshForTests();

            dashboard.Find("HeroPanel/UnavailableButton").GetComponent<Button>().onClick.Invoke();

            Transform modalOverlay = root.transform.Find("SafeAreaRoot/ModalOverlay");
            Assert.IsTrue(modalOverlay.gameObject.activeSelf);
            Transform endDayPopup = modalOverlay.Find("EndOfDayReportPopup(Clone)");
            Assert.NotNull(endDayPopup);
            StringAssert.Contains("Trust 51", endDayPopup.Find("Frame/BodyRoot/DeltaSummary/DeltaText").GetComponent<TMP_Text>().text);
            StringAssert.Contains("Heat 48", endDayPopup.Find("Frame/BodyRoot/EnemyActivityPanel/TrendLabelText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("51", endDayPopup.Find("Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow/ValueText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("35", endDayPopup.Find("Frame/BodyRoot/TrustStabilityPanel/RegionStabilityRow/ValueText").GetComponent<TMP_Text>().text);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OperationInbox_BindsPendingOperationReports()
    {
        WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Scan);
        WarlineCaptureOperationRuntime.SelectDistrict("old_market");
        WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Aid);

        GameObject root = InstantiatePrefab(InboxPrefabPath);
        try
        {
            OperationInboxScreenSystem controller = root.GetComponent<OperationInboxScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();

            Assert.AreEqual("2 FIELD REPORTS", Text(root, "HeroPanel/HeroTitleText"));
            Assert.AreEqual("OLD MARKET AID DISTRIBUTION", Text(root, "StatusCard_1/TitleText"));
            Assert.AreEqual("INFO / UNREAD", Text(root, "StatusCard_1/StatusText"));
            StringAssert.Contains("Medical and water distribution", Text(root, "StatusCard_1/BodyText"));
            Assert.AreEqual("NORTH BRIDGE INTEL SWEEP", Text(root, "StatusCard_3/TitleText"));
            Assert.AreEqual("44% / UNREAD", Text(root, "StatusCard_3/StatusText"));
            StringAssert.Contains("Drone Scan", Text(root, "FeedRow_2/BodyText"));
            Assert.AreEqual("INTEL", Text(root, "FeedRow_3/TagText"));
            StringAssert.Contains("saved Operation state", Text(root, "ImplementationNotePanel/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OperationEvents_BindsLatestOperationEventAndHotZone()
    {
        WarlineCaptureOperationRuntime.SelectDistrict("port_breach");
        WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Scan);

        GameObject root = InstantiatePrefab(EventsPrefabPath);
        try
        {
            OperationEventsScreenSystem controller = root.GetComponent<OperationEventsScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();

            Assert.AreEqual("ACTIVE CITY EVENT", Text(root, "HeroPanel/HeroTitleText"));
            StringAssert.Contains("Port Sensor Sweep", Text(root, "HeroPanel/BodyText"));
            Assert.AreEqual("INFO / UNREAD", Text(root, "StatusCard_1/StatusText"));
            Assert.AreEqual("HOT ZONE", Text(root, "StatusCard_2/TitleText"));
            StringAssert.Contains("PORT BREACH", Text(root, "StatusCard_2/BodyText"));
            StringAssert.Contains("PORT BREACH Intel Sweep", Text(root, "StatusCard_3/BodyText"));
            Assert.AreEqual("INTEL", Text(root, "FeedRow_3/TagText"));
            StringAssert.Contains("Operation event ledger", Text(root, "ImplementationNotePanel/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OperationCommandFeed_BindsLocalOperationUpdates()
    {
        WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Scan);
        WarlineCaptureOperationRuntime.EndDay();

        GameObject root = InstantiatePrefab(CommandFeedPrefabPath);
        try
        {
            OperationCommandFeedScreenSystem controller = root.GetComponent<OperationCommandFeedScreenSystem>();
            Assert.NotNull(controller);

            controller.RefreshForTests();

            Assert.AreEqual("4 LOCAL UPDATES", Text(root, "HeroPanel/HeroTitleText"));
            StringAssert.Contains("Enemy Influence Entrenched", Text(root, "HeroPanel/BodyText"));
            Assert.AreEqual("OPERATION FEED", Text(root, "StatusCard_2/TitleText"));
            Assert.AreEqual("WARNING / ENEMY INFLUENCE 84 / UNREAD", Text(root, "StatusCard_2/StatusText"));
            Assert.AreEqual("INTEL ARCHIVE", Text(root, "StatusCard_3/TitleText"));
            Assert.AreEqual("44% / UNREAD", Text(root, "StatusCard_3/StatusText"));
            StringAssert.Contains("Enemy Influence Entrenched", Text(root, "FeedRow_1/BodyText"));
            StringAssert.Contains("Civilian Risk Elevated", Text(root, "FeedRow_2/BodyText"));
            Assert.AreEqual("INTEL", Text(root, "FeedRow_3/TagText"));
            StringAssert.Contains("recent Operation reports", Text(root, "ImplementationNotePanel/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject InstantiatePrefab(string path)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, path);
        return Object.Instantiate(prefab);
    }

    private static string Text(GameObject root, string path)
    {
        Transform target = root.transform.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        return text.text;
    }
}
