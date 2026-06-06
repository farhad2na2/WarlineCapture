using System;
using UnityEngine;

public sealed class SagaProgressStore
{
    private SagaProgressStore()
    {
    }

    private const string Prefix = "WarlineCapture.Saga.";

    public static void ApplyMissionResult(MissionResultData result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (string.IsNullOrWhiteSpace(result.MissionId))
            throw new ArgumentException("Mission result must include a mission id.", nameof(result));

        if (result.Victory)
            PlayerPrefs.SetInt(CompletedKey(result.MissionId), 1);

        int previousStars = GetStars(result.MissionId);
        PlayerPrefs.SetInt(StarsKey(result.MissionId), Mathf.Max(previousStars, result.StarsEarned));
        PlayerPrefs.Save();
    }

    public static bool IsCompleted(string missionId)
    {
        return PlayerPrefs.GetInt(CompletedKey(missionId), 0) == 1;
    }

    public static int GetStars(string missionId)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(StarsKey(missionId), 0), 0, 3);
    }

    public static void ClearMission(string missionId)
    {
        PlayerPrefs.DeleteKey(CompletedKey(missionId));
        PlayerPrefs.DeleteKey(StarsKey(missionId));
    }

    private static string CompletedKey(string missionId)
    {
        return $"{Prefix}{missionId}.completed";
    }

    private static string StarsKey(string missionId)
    {
        return $"{Prefix}{missionId}.stars";
    }
}
