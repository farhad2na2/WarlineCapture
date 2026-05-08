using System;
using System.Globalization;
using UnityEngine;

public readonly struct CommanderProgression
{
    public readonly int Level;
    public readonly int TotalXp;
    public readonly int CurrentLevelXp;
    public readonly int NextLevelXp;
    public readonly int XpIntoLevel;
    public readonly int XpToNextLevel;
    public readonly float Progress01;
    public readonly bool IsMaxKnownLevel;

    public CommanderProgression(int level, int totalXp, int currentLevelXp, int nextLevelXp, bool isMaxKnownLevel)
    {
        Level = Mathf.Max(1, level);
        TotalXp = Mathf.Max(0, totalXp);
        CurrentLevelXp = Mathf.Max(0, currentLevelXp);
        NextLevelXp = Mathf.Max(CurrentLevelXp, nextLevelXp);
        IsMaxKnownLevel = isMaxKnownLevel;

        int span = Mathf.Max(1, NextLevelXp - CurrentLevelXp);
        XpIntoLevel = Mathf.Max(0, TotalXp - CurrentLevelXp);
        XpToNextLevel = isMaxKnownLevel ? 0 : Mathf.Max(0, NextLevelXp - TotalXp);
        Progress01 = isMaxKnownLevel ? 1f : Mathf.Clamp01((float)XpIntoLevel / span);
    }

    public string FormatLevel()
    {
        return $"LV. {Level}";
    }

    public string FormatXpProgress()
    {
        return IsMaxKnownLevel
            ? $"{FormatNumber(TotalXp)} / MAX"
            : $"{FormatNumber(TotalXp)} / {FormatNumber(NextLevelXp)}";
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
