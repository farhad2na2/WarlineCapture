using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Chapter01LegacyRuntimeGuardrailTests
{
    private const string MissionId = "saga.ch01.m01.first_contact";
    private const string ManifestPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset";
    private const string GuardrailDocPath = "Assets/Game/Data/TacticalMaps/Chapter01/WarlineCapture_M01_Legacy_Runtime_Guardrails.md";
    private const string M01SoldierPrefabPath = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";

    [Test]
    public void M01AtlasManifest_RequiresFixedDirectionBakedContactShadows()
    {
        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(ManifestPath);
        Assert.NotNull(manifest);

        AssertM01ShadowPlan(manifest, "unit.player.rifle_squad_01");
        AssertM01ShadowPlan(manifest, "unit.enemy.patrol_01");
        AssertM01ShadowPlan(manifest, "decor.command_point");
    }

    [Test]
    public void M01LegacyPrefabAudit_DocumentsCurrentModelAndDestroyedBlockers()
    {
        Assert.IsTrue(File.Exists(GuardrailDocPath), "M01 legacy runtime guardrail audit must be checked in.");
        string doc = File.ReadAllText(GuardrailDocPath);

        StringAssert.Contains("Legacy Render Blockers", doc);
        StringAssert.Contains("Unit_Chr_Soldier_Male_02_Alt_04.prefab", doc);
        StringAssert.Contains("Model", doc);
        StringAssert.Contains("Destroyed", doc);
        StringAssert.Contains("vfx.unit.destroyed.small", doc);
        StringAssert.Contains("DayNightSystem", doc);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(M01SoldierPrefabPath);
        try
        {
            Assert.NotNull(prefabRoot);
            Assert.IsTrue(HasDirectOrNestedChild(prefabRoot.transform, "Model"), "The audit should stay accurate while M01 still references a legacy prefab with a Model child.");
            Assert.IsFalse(HasDirectOrNestedChild(prefabRoot.transform, "Destroyed"), "The M01 soldier source prefab should not rely on a separate Destroyed child.");
        }
        finally
        {
            if (prefabRoot != null)
                PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [Test]
    public void M01GuardrailAudit_DefinesAtlasMigrationAndDayNightIsolation()
    {
        Assert.IsTrue(File.Exists(GuardrailDocPath), "M01 legacy runtime guardrail audit must be checked in.");
        string doc = File.ReadAllText(GuardrailDocPath);

        StringAssert.Contains("Migration Plan", doc);
        StringAssert.Contains("sprite presenter", doc);
        StringAssert.Contains("fixed-direction baked/contact shadows", doc);
        StringAssert.Contains("idle, move, attack, damaged, and destroyed", doc);
        StringAssert.Contains("DayNightSystem is disabled for M01 fixed tactical gameplay", doc);
    }

    private static void AssertM01ShadowPlan(Chapter01TacticalAssetManifest manifest, string assetId)
    {
        Assert.IsTrue(manifest.TryGetEntry(assetId, out TacticalAssetManifestEntry entry), $"{assetId} must exist.");
        CollectionAssert.Contains(entry.UsedByMissionIds, MissionId, $"{assetId} must be used by M01.");
        StringAssert.Contains("baked/contact shadow", entry.Notes, $"{assetId} must document baked/contact shadow requirement.");
        StringAssert.Contains("fixed-direction", entry.Notes, $"{assetId} must document fixed-direction lighting requirement.");
    }

    private static bool HasDirectOrNestedChild(Transform root, string childName)
    {
        if (root == null)
            return false;

        foreach (Transform child in root)
        {
            if (child.name == childName || HasDirectOrNestedChild(child, childName))
                return true;
        }

        return false;
    }
}
