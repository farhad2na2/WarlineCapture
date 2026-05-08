using System;
using NUnit.Framework;

public sealed class MissionHistoryServiceTests
{
    [Test]
    public void MissionHistoryService_RecordsLatestResultFirst()
    {
        var profile = new PlayerProfileSaveData();

        MissionHistoryService.RecordResult(profile, Result("saga.ch01.m01", "First Contact", true, 2));
        MissionHistoryService.RecordResult(profile, Result("saga.ch01.m02", "Broken Bridge", false, 1));

        Assert.AreEqual(2, profile.missionHistory.Length);
        Assert.AreEqual("saga.ch01.m02", profile.missionHistory[0].missionId);
        Assert.AreEqual("Broken Bridge", MissionHistoryService.GetLatest(profile).missionName);
        Assert.AreEqual("Defeat | Stars 1/3 | Kills 12 | Losses 2", profile.missionHistory[0].summary);
    }

    [Test]
    public void MissionHistoryService_CapsArchive()
    {
        var profile = new PlayerProfileSaveData();

        for (int i = 0; i < MissionHistoryService.MaxEntries + 3; i++)
            MissionHistoryService.RecordResult(profile, Result($"mission.{i:00}", $"Mission {i:00}", true, 3));

        Assert.AreEqual(MissionHistoryService.MaxEntries, profile.missionHistory.Length);
        Assert.AreEqual("mission.22", profile.missionHistory[0].missionId);
        Assert.AreEqual("mission.03", profile.missionHistory[^1].missionId);
    }

    private static MissionResultData Result(string missionId, string missionName, bool victory, int stars)
    {
        return new MissionResultData(
            missionId,
            missionName,
            victory,
            stars,
            12,
            2,
            4,
            900,
            Array.Empty<ObjectiveRuntimeState>());
    }
}
