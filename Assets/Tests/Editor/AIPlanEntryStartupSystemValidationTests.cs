#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;

public sealed class AIPlanEntryStartupSystemValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new AIPlanEntryStartupSystemValidationTests();
            tests.WriteBuildPlanEntries_UsesPreferredIdsBeforeFallbackDefaults();
            tests.WriteBuildPlanEntries_UsesFallbackDefaultsWhenPreferencesAreEmpty();
            tests.WriteProductionPlanEntries_CombinesPreferredUnitsAndVehiclesBeforeFallbackDefault();
            tests.WriteProductionPlanEntries_UsesFallbackDefaultWhenPreferencesAreEmpty();
            UnityEngine.Debug.Log("[AIPlanEntryStartupValidation] result=Passed tests=4");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("[AIPlanEntryStartupValidation] result=Failed");
            UnityEngine.Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void WriteBuildPlanEntries_UsesPreferredIdsBeforeFallbackDefaults()
    {
        using var world = new World("AIPlanEntryStartupSystemBuildPreferredTests");
        EntityManager em = world.EntityManager;
        Entity entity = em.CreateEntity();
        DynamicBuffer<AIBuildPlanEntry> entries = em.AddBuffer<AIBuildPlanEntry>(entity);

        AIPlanEntryStartupSystem system = world.GetOrCreateSystemManaged<AIPlanEntryStartupSystem>();
        system.WriteBuildPlanEntries(
            entries,
            new[] { "Custom_Barracks", "", "  ", "Custom_Refinery" },
            LoadPlanEntryConfig());

        Assert.AreEqual(2, entries.Length);
        Assert.AreEqual("Custom_Barracks", entries[0].BuildingId.ToString());
        Assert.AreEqual("Custom_Refinery", entries[1].BuildingId.ToString());
    }

    [Test]
    public void WriteBuildPlanEntries_UsesFallbackDefaultsWhenPreferencesAreEmpty()
    {
        using var world = new World("AIPlanEntryStartupSystemBuildFallbackTests");
        EntityManager em = world.EntityManager;
        Entity entity = em.CreateEntity();
        DynamicBuffer<AIBuildPlanEntry> entries = em.AddBuffer<AIBuildPlanEntry>(entity);

        AIPlanEntryStartupSystem system = world.GetOrCreateSystemManaged<AIPlanEntryStartupSystem>();
        system.WriteBuildPlanEntries(entries, null, LoadPlanEntryConfig());

        Assert.AreEqual(5, entries.Length);
        Assert.AreEqual("Tent_Regular", entries[0].BuildingId.ToString());
        Assert.AreEqual("Building_Barrack", entries[1].BuildingId.ToString());
        Assert.AreEqual("Building_OilPump", entries[2].BuildingId.ToString());
        Assert.AreEqual("Building_Fuel_Bladder", entries[3].BuildingId.ToString());
        Assert.AreEqual("Building_Ammunition_Depot", entries[4].BuildingId.ToString());
    }

    [Test]
    public void WriteProductionPlanEntries_CombinesPreferredUnitsAndVehiclesBeforeFallbackDefault()
    {
        using var world = new World("AIPlanEntryStartupSystemProductionPreferredTests");
        EntityManager em = world.EntityManager;
        Entity entity = em.CreateEntity();
        DynamicBuffer<AIProductionPlanEntry> entries = em.AddBuffer<AIProductionPlanEntry>(entity);

        AIPlanEntryStartupSystem system = world.GetOrCreateSystemManaged<AIPlanEntryStartupSystem>();
        system.WriteProductionPlanEntries(
            entries,
            new[] { "Custom_Rifleman", "" },
            new[] { "Custom_APC", " " },
            LoadPlanEntryConfig());

        Assert.AreEqual(2, entries.Length);
        Assert.AreEqual("custom_rifleman", entries[0].UnitId.ToString());
        Assert.AreEqual("custom_apc", entries[1].UnitId.ToString());
    }

    [Test]
    public void WriteProductionPlanEntries_UsesFallbackDefaultWhenPreferencesAreEmpty()
    {
        using var world = new World("AIPlanEntryStartupSystemProductionFallbackTests");
        EntityManager em = world.EntityManager;
        Entity entity = em.CreateEntity();
        DynamicBuffer<AIProductionPlanEntry> entries = em.AddBuffer<AIProductionPlanEntry>(entity);

        AIPlanEntryStartupSystem system = world.GetOrCreateSystemManaged<AIPlanEntryStartupSystem>();
        system.WriteProductionPlanEntries(entries, null, null, LoadPlanEntryConfig());

        Assert.AreEqual(1, entries.Length);
        Assert.AreEqual("unit_chr_soldier_male_02_alt_04", entries[0].UnitId.ToString());
    }

    private static AIPlanEntryStartupConfig LoadPlanEntryConfig()
    {
        const string path = "Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset";
        AIPlanEntryStartupConfig config = AssetDatabase.LoadAssetAtPath<AIPlanEntryStartupConfig>(path);
        Assert.NotNull(config, $"Missing AI plan entry startup config asset at {path}");
        return config;
    }
}
#endif
