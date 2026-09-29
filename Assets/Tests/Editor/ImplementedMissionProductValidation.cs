using System;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public static class ImplementedMissionProductValidation
{
    public static void Run()
    {
        ImplementedContentAccessChecks.RunAll();
        Debug.Log(ImplementedContentAccessChecks.PassMarker);
        var profile = new PlayerProfileSaveData
        {
            credits = 4000, commanderXp = 750, materials = 73, fuel = 29, intel = 11,
            rushTickets = 5, commandAuthority = 3,
            blueprintParts = new[] { new BlueprintPartSaveData { targetItemId = "Unit_Veh_APC_Heavy", amount = 35 } },
            ownedUnitUnlocks = new[] { "Unit_Chr_Ghillie_Male_01" },
            ownedSupportAbilityUnlocks = new[] { "ability.smoke_screen", "legacy.support" },
            campaignMissionProgress = new[] { new CampaignMissionProgressSaveData
            { missionId = "saga.ch01.m05.breach_assault", firstClearCompleted = true,
              firstClearRewardSettled = true, settledTokens = new[] { "earned-first-clear" } } }
        };
        string before = JsonUtility.ToJson(profile);
        MissionProductProfileMigration.Normalize(profile);
        MissionProductProfileMigration.Normalize(profile);
        Assert.AreEqual(MissionProductProfileMigration.Version, profile.missionProductMigrationVersion);
        var expected = JsonUtility.FromJson<PlayerProfileSaveData>(before);
        expected.missionProductMigrationVersion = MissionProductProfileMigration.Version;
        Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(profile), "Migration must preserve earned account data.");
        profile.missionProductMigrationVersion = 99;
        string future = JsonUtility.ToJson(profile);
        MissionProductProfileMigration.Normalize(profile);
        Assert.AreEqual(future, JsonUtility.ToJson(profile), "Future profile must be preserved.");
        var m05 = new M05BreachAssaultIntegrationTests();
        m05.FirstClearAndReplayPersistWithoutDuplicateUnlocks();
        var smoke = new Game.Tests.Editor.SupportCampaignValidation();
        smoke.FirstClearUnlockIsIdempotent();
        smoke.MigrationOnlyFromCompletedMilestone();
        smoke.AuthoredSteelPushRewardContract();
        smoke.EarlyReplayStillHidden();
        Debug.Log("[ImplementedMissionProducts] result=Passed access=205 migration=preserve,idempotent,future M05=first-clear,replay,duplicate Smoke=canonical,migration,early-replay");
    }
}
