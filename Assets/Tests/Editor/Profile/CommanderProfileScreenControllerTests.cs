using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class CommanderProfileScreenControllerTests
{
    [Test]
    public void CommanderProfileScreenController_BindsSavedProfileSummary()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab");
        Assert.NotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            CommanderProfileScreenController controller = instance.GetComponent<CommanderProfileScreenController>();
            Assert.NotNull(controller);

            controller.SetProfileForTests(new PlayerProfileSaveData
            {
                commanderName = "Mandel",
                commanderLevel = 2,
                commanderXp = 460,
                credits = 24800,
                materials = 12600,
                fuel = 430,
                intel = 12,
                commandAuthority = 1250,
                victories = 7,
                defeats = 1,
                missionsCompleted = 8,
                starsEarned = 19,
                enemiesDefeated = 142,
                unitsLost = 11,
                buildingsBuilt = 23,
                resourcesEarned = 3700,
                ownedUnitUnlocks = new[] { "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Heavy" },
                ownedBuildingUnlocks = new[] { "Building_Barrack" },
                ownedSupportAbilityUnlocks = new[] { "ability.radar_ping" }
            });

            controller.RefreshForTests();

            AssertText(instance.transform, "HeaderBar/CreditsCounter/ValueText", "24.8K");
            AssertText(instance.transform, "HeaderBar/MaterialsCounter/ValueText", "12.6K");
            AssertText(instance.transform, "HeaderBar/AuthorityCounter/ValueText", "1.3K");
            AssertText(instance.transform, "HeroPanel/HeroTitleText", "MANDEL");
            AssertText(instance.transform, "StatusCard_1/StatusText", "LV. 3");
            AssertText(instance.transform, "StatusCard_2/StatusText", "4 owned");
            AssertText(instance.transform, "StatusCard_3/StatusText", "7 W / 1 L");
            StringAssert.Contains("Reward nodes ready: 2", TextAt(instance.transform, "StatusCard_1/BodyText"));
            AssertText(instance.transform, "FeedRow_1/TagText", "TRACK");
            StringAssert.Contains("Field Budget ready to claim", TextAt(instance.transform, "FeedRow_1/BodyText"));
            StringAssert.Contains("Resources 3,700", TextAt(instance.transform, "StatusCard_3/BodyText"));
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_1/StatusText", "CLAIM");
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_2/StatusText", "CLAIM");
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_3/StatusText", "LOCKED");
            StringAssert.Contains("2 milestone", TextAt(instance.transform, "RewardTrackPanel/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void CommanderProfileScreenController_ClaimsFirstAvailableRewardTrackNode()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab");
        Assert.NotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            CommanderProfileScreenController controller = instance.GetComponent<CommanderProfileScreenController>();
            Assert.NotNull(controller);

            PlayerProfileSaveData profile = new PlayerProfileSaveData
            {
                commanderXp = 180
            };
            ProgressionService.GrantCommanderXp(profile, 0);

            controller.SetProfileForTests(profile);
            controller.RefreshForTests();

            AssertText(instance.transform, "HeroPanel/UnavailableButton/LabelText", "CLAIM REWARD");
            Assert.IsTrue(controller.TryClaimFirstRewardTrackNode());

            Assert.AreEqual(500, profile.credits);
            Assert.AreEqual(1, profile.claimedRewardTrackNodes.Length);
            AssertText(instance.transform, "HeroPanel/UnavailableButton/LabelText", "TRACK LIVE");
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_1/StatusText", "CLAIMED");
            StringAssert.Contains("Next reward: Material Reserve", TextAt(instance.transform, "FeedRow_1/BodyText"));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void CommanderProfileScreenController_ClaimsSelectedRewardTrackNode()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab");
        Assert.NotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            CommanderProfileScreenController controller = instance.GetComponent<CommanderProfileScreenController>();
            Assert.NotNull(controller);

            PlayerProfileSaveData profile = new PlayerProfileSaveData
            {
                commanderXp = 450
            };
            ProgressionService.GrantCommanderXp(profile, 0);

            controller.SetProfileForTests(profile);
            controller.RefreshForTests();

            Assert.IsTrue(controller.TryClaimRewardTrackNodeAt(1));

            Assert.AreEqual(350, profile.materials);
            Assert.AreEqual("commander.level.03", profile.claimedRewardTrackNodes[0]);
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_1/StatusText", "CLAIM");
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_2/StatusText", "CLAIMED");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void CommanderProfileScreenController_RewardTrackClickShowsDetailModalAndClaims()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab");
        Assert.NotNull(prefab);

        GameObject shellRoot = new GameObject("ShellRoot");
        GameObject overlay = new GameObject("ModalOverlay");
        GameObject placeholder = new GameObject("PlaceholderPopup");
        GameObject titleObject = new GameObject("TitleText");
        GameObject bodyObject = new GameObject("BodyText");
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            overlay.transform.SetParent(shellRoot.transform);
            placeholder.transform.SetParent(overlay.transform);
            titleObject.transform.SetParent(placeholder.transform);
            bodyObject.transform.SetParent(placeholder.transform);
            instance.transform.SetParent(shellRoot.transform);
            overlay.SetActive(false);

            TMP_Text titleText = titleObject.AddComponent<TextMeshProUGUI>();
            TMP_Text bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            WarlineCaptureModalController modal = shellRoot.AddComponent<WarlineCaptureModalController>();
            SetPrivateField(modal, "modalOverlay", overlay);
            SetPrivateField(modal, "placeholderContent", placeholder);
            SetPrivateField(modal, "placeholderTitleText", titleText);
            SetPrivateField(modal, "placeholderBodyText", bodyText);

            CommanderProfileScreenController controller = instance.GetComponent<CommanderProfileScreenController>();
            Assert.NotNull(controller);

            PlayerProfileSaveData profile = new PlayerProfileSaveData
            {
                commanderXp = 180
            };
            ProgressionService.GrantCommanderXp(profile, 0);

            controller.SetProfileForTests(profile);
            controller.RefreshForTests();

            Assert.IsTrue(controller.ShowRewardTrackNodeDetailForTests(0));

            Assert.IsTrue(overlay.activeSelf);
            Assert.AreEqual("Reward Claimed", titleText.text);
            StringAssert.Contains("Field Budget", bodyText.text);
            StringAssert.Contains("Granted: +500 Credits", bodyText.text);
            Assert.AreEqual(500, profile.credits);
            AssertText(instance.transform, "RewardTrackPanel/TrackNode_1/StatusText", "CLAIMED");
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(shellRoot);
        }
    }

    [Test]
    public void CommanderProfileScreenController_LocalTabsSwitchProfileContent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab");
        Assert.NotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            CommanderProfileScreenController controller = instance.GetComponent<CommanderProfileScreenController>();
            Assert.NotNull(controller);

            controller.SetProfileForTests(new PlayerProfileSaveData
            {
                commanderName = "Mandel",
                commanderXp = 450,
                victories = 4,
                defeats = 1,
                missionsCompleted = 5,
                enemiesDefeated = 42,
                unitsLost = 6,
                buildingsBuilt = 9,
                resourcesEarned = 1400,
                ownedUnitUnlocks = new[] { "Unit_Chr_Soldier_Male_02_Alt_04" },
                ownedBuildingUnlocks = new[] { "Building_Barrack" },
                ownedSupportAbilityUnlocks = new[] { "ability.radar_ping" },
                ownedCosmetics = new[] { "cosmetic.commander_frame.iron_guard" },
                blueprintParts = new[] { new BlueprintPartSaveData { targetItemId = "Unit_Veh_APC_Heavy", amount = 20 } },
                missionHistory = new[]
                {
                    new MissionHistoryEntrySaveData
                    {
                        missionId = "saga.ch01.m02",
                        missionName = "Broken Bridge",
                        victory = true,
                        starsEarned = 3,
                        enemiesDefeated = 42,
                        unitsLost = 6,
                        buildingsBuilt = 9,
                        resourcesEarned = 1400,
                        summary = "Victory | Stars 3/3 | Kills 42 | Losses 6"
                    }
                }
            });

            controller.SelectTabForTests(1);
            AssertText(instance.transform, "FeedRow_1/TagText", "UPGRADES");
            AssertText(instance.transform, "StatusCard_1/TitleText", "UPGRADE LINKS");
            AssertText(instance.transform, "StatusCard_3/StatusText", "1 stacks");

            controller.SelectTabForTests(2);
            AssertText(instance.transform, "FeedRow_1/TagText", "HISTORY");
            AssertText(instance.transform, "StatusCard_1/TitleText", "MISSION HISTORY");
            AssertText(instance.transform, "StatusCard_3/StatusText", "VICTORY");
            StringAssert.Contains("Broken Bridge", TextAt(instance.transform, "StatusCard_3/BodyText"));
            StringAssert.Contains("Latest: Broken Bridge", TextAt(instance.transform, "FeedRow_2/BodyText"));

            controller.SelectTabForTests(3);
            AssertText(instance.transform, "FeedRow_1/TagText", "COSMETICS");
            AssertText(instance.transform, "StatusCard_2/StatusText", "OWNED");

            controller.SelectTabForTests(4);
            AssertText(instance.transform, "FeedRow_1/TagText", "STATS");
            AssertText(instance.transform, "StatusCard_3/StatusText", "7.0");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void AssertText(Transform root, string path, string expected)
    {
        Assert.AreEqual(expected, TextAt(root, path), path);
    }

    private static string TextAt(Transform root, string path)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        return text.text;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, fieldName);
        field.SetValue(target, value);
    }
}
