using System;
using UnityEngine;

public static class ProgressionService
{
    private static readonly int[] LevelStartXp =
    {
        0,
        0,
        180,
        450,
        820,
        1300,
        1900,
        2650,
        3550,
        4600,
        5800
    };

    public static CommanderProgression GetCommanderProgression(PlayerProfileSaveData profile)
    {
        profile ??= new PlayerProfileSaveData();

        int levelFromXp = GetLevelForXp(profile.commanderXp);
        int level = Mathf.Max(1, profile.commanderLevel, levelFromXp);
        int clampedLevel = Mathf.Min(level, MaxKnownLevel);
        bool isMaxKnownLevel = clampedLevel >= MaxKnownLevel && profile.commanderXp >= LevelStartXp[MaxKnownLevel];
        int currentLevelXp = LevelStartXp[clampedLevel];
        int nextLevelXp = isMaxKnownLevel ? currentLevelXp : LevelStartXp[Mathf.Min(MaxKnownLevel, clampedLevel + 1)];

        return new CommanderProgression(clampedLevel, profile.commanderXp, currentLevelXp, nextLevelXp, isMaxKnownLevel);
    }

    public static int GrantCommanderXp(PlayerProfileSaveData profile, int amount)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        int before = Mathf.Max(1, profile.commanderLevel);
        profile.commanderXp = Mathf.Max(0, profile.commanderXp + Mathf.Max(0, amount));
        profile.commanderLevel = Mathf.Max(before, GetLevelForXp(profile.commanderXp));
        return profile.commanderLevel - before;
    }

    public static void AccumulateAccountStats(PlayerProfileSaveData profile, MissionResultData result, int starDelta)
    {
        if (profile == null || result == null)
            return;

        if (result.Victory)
        {
            profile.victories++;
            profile.missionsCompleted++;
        }
        else
        {
            profile.defeats++;
        }

        profile.starsEarned += Mathf.Max(0, starDelta);
        profile.enemiesDefeated += Mathf.Max(0, result.EnemiesDefeated);
        profile.unitsLost += Mathf.Max(0, result.UnitsLost);
        profile.buildingsBuilt += Mathf.Max(0, result.BuildingsBuilt);
        profile.resourcesEarned += Mathf.Max(0, result.ResourcesEarned);
    }

    public static int GetLevelForXp(int commanderXp)
    {
        int xp = Mathf.Max(0, commanderXp);
        int level = 1;

        for (int i = 2; i <= MaxKnownLevel; i++)
        {
            if (xp < LevelStartXp[i])
                break;

            level = i;
        }

        return level;
    }

    public static int GetRequiredTotalXpForLevel(int level)
    {
        return LevelStartXp[Mathf.Clamp(level, 1, MaxKnownLevel)];
    }

    public static int MaxKnownLevel => LevelStartXp.Length - 1;
}
