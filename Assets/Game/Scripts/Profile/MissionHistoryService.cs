using System;
using UnityEngine;

public sealed class MissionHistoryService
{
    private MissionHistoryService()
    {
    }

    public const int MaxEntries = 20;

    public static void RecordResult(PlayerProfileSaveData profile, MissionResultData result)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (result == null)
            return;

        profile.missionHistory ??= Array.Empty<MissionHistoryEntrySaveData>();
        MissionHistoryEntrySaveData entry = CreateEntry(result);
        var entries = new MissionHistoryEntrySaveData[Mathf.Min(MaxEntries, profile.missionHistory.Length + 1)];
        entries[0] = entry;

        int copyCount = Mathf.Min(profile.missionHistory.Length, entries.Length - 1);
        for (int i = 0; i < copyCount; i++)
            entries[i + 1] = profile.missionHistory[i];

        profile.missionHistory = entries;
    }

    public static MissionHistoryEntrySaveData GetLatest(PlayerProfileSaveData profile)
    {
        if (profile?.missionHistory == null || profile.missionHistory.Length == 0)
            return null;

        return profile.missionHistory[0];
    }

    private static MissionHistoryEntrySaveData CreateEntry(MissionResultData result)
    {
        return new MissionHistoryEntrySaveData
        {
            missionId = result.MissionId ?? string.Empty,
            missionName = string.IsNullOrWhiteSpace(result.MissionName) ? "Mission" : result.MissionName,
            victory = result.Victory,
            starsEarned = Mathf.Clamp(result.StarsEarned, 0, 3),
            enemiesDefeated = Mathf.Max(0, result.EnemiesDefeated),
            unitsLost = Mathf.Max(0, result.UnitsLost),
            buildingsBuilt = Mathf.Max(0, result.BuildingsBuilt),
            resourcesEarned = Mathf.Max(0, result.ResourcesEarned),
            summary = FormatSummary(result)
        };
    }

    private static string FormatSummary(MissionResultData result)
    {
        string outcome = result.Victory ? "Victory" : "Defeat";
        return $"{outcome} | Stars {Mathf.Clamp(result.StarsEarned, 0, 3)}/3 | Kills {Mathf.Max(0, result.EnemiesDefeated)} | Losses {Mathf.Max(0, result.UnitsLost)}";
    }
}
