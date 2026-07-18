using Game.Editor;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Game.Composition;
using Game.Authoring;

public sealed class OperationMapCurrentCompatibilitySceneStagerTests
{
    [Test]
    public void StagedSceneUsesCanonicalPathAndDistinctGuid()
    {
        Assert.That(
            OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
            Is.EqualTo(
                "Assets/Game/Scenes/OperationMaps/Skirmish/" +
                "opmap_skirmish_desert_base_01.unity"));
        Assert.That(
            AssetDatabase.AssetPathToGUID(OperationMapCurrentCompatibilitySceneStager.SourceScenePath),
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapCurrentCompatibilitySceneStager.DestinationScenePath)));
    }

    [Test]
    public void StagedSceneContainsOnlyAcceptedMapAndCompatibilityRoots()
    {
        Assert.That(
            OperationMapCurrentCompatibilityRootExtractor.TryValidate(out string error),
            Is.True,
            error);
    }

    [Test]
    public void StagedSubSceneRetainsMapRootsAndUsesDistinctGuid()
    {
        Assert.That(
            AssetDatabase.AssetPathToGUID(OperationMapCurrentCompatibilitySubSceneStager.SourceSubScenePath),
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath)));
        Assert.That(
            OperationMapCurrentCompatibilitySubSceneStager.TryValidate(out string error),
            Is.True,
            error);
    }

    [Test]
    public void StagedPlacementConfigsMatchTheExtractedMapHierarchy()
    {
        Assert.That(
            OperationMapCurrentCompatibilityPlacementStager.TryValidate(out string error),
            Is.True,
            error);
        Assert.That(
            AssetDatabase.AssetPathToGUID(
                OperationMapCurrentCompatibilityPlacementStager.SourceBuildingConfigPath),
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapCurrentCompatibilityPlacementStager.DestinationBuildingConfigPath)));
        Assert.That(
            AssetDatabase.AssetPathToGUID(
                OperationMapCurrentCompatibilityPlacementStager.SourceVehicleConfigPath),
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapCurrentCompatibilityPlacementStager.DestinationVehicleConfigPath)));
    }

    [Test]
    public void StagedSceneViewBindsOnlyAcceptedMapReferences()
    {
        Assert.That(
            OperationMapCurrentCompatibilitySceneViewStager.TryValidate(out string error),
            Is.True,
            error);
    }

    [Test]
    public void StagedSceneViewBindsTheMapOwnedGridConfig()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
            OpenSceneMode.Additive);
        try
        {
            OperationMapSceneView[] views = scene.GetRootGameObjects()[^1]
                .GetComponentsInChildren<OperationMapSceneView>(true);
            Assert.That(views, Has.Length.EqualTo(1));
            Assert.That(
                AssetDatabase.GetAssetPath(views[0].GridAuthoringConfig),
                Is.EqualTo("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }

    [TestCase("MatchSubScene")]
    [TestCase("Map")]
    [TestCase("Faction1")]
    public void MatchShellCutoverClassifiesMapRoots(string rootName)
    {
        Assert.That(OperationMapCurrentMatchShellCutover.IsMapRootName(rootName), Is.True);
    }

    [TestCase("Bootstrap")]
    [TestCase("Main Camera")]
    [TestCase("Global Volume")]
    [TestCase("MatchRuntimeSubScene")]
    public void MatchShellCutoverRejectsShellRootsAsMapOwnership(string rootName)
    {
        Assert.That(OperationMapCurrentMatchShellCutover.IsMapRootName(rootName), Is.False);
    }

    [Test]
    public void MatchRuntimeSubSceneContainsOnlySharedUnitRegistryAuthoring()
    {
        Assert.That(
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                OperationMapCurrentMatchShellCutover.MatchRuntimeSubScenePath),
            Is.Not.Null);
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapCurrentMatchShellCutover.MatchRuntimeSubScenePath,
            OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            Assert.That(roots, Has.Length.EqualTo(1));
            Assert.That(roots[0].name, Is.EqualTo("UnitPrefabRegistryAuthoring"));
            Assert.That(roots[0].GetComponent<UnitPrefabRegistryAuthoring>(), Is.Not.Null);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }

    [Test]
    public void MatchSceneContainsOnlyValidatedShellOwnershipAfterCutover()
    {
        Assert.That(
            OperationMapCurrentMatchShellCutover.TryValidateThinShell(out string error),
            Is.True,
            error);
    }

    [Test]
    public void StagedDefinitionPreservesLogicalIdentityAndBindsMapSubScene()
    {
        Assert.That(
            OperationMapCurrentStagedDefinitionBuilder.TryValidate(out string error),
            Is.True,
            error);
        Assert.That(
            AssetDatabase.AssetPathToGUID(OperationMapCurrentStagedDefinitionBuilder.DefinitionPath),
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath)));
    }

    [Test]
    public void StagedSceneBindsCurrentSpatialAndLightingMetadata()
    {
        Assert.That(
            OperationMapCurrentStagedSpatialBindingValidator.TryValidate(out string error),
            Is.True,
            error);
    }

    [TestCase("Update")]
    [TestCase("LateUpdate")]
    [TestCase("FixedUpdate")]
    public void OperationMapSceneViewHasNoUpdateLoop(string methodName)
    {
        Assert.That(
            typeof(OperationMapSceneView).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            Is.Null);
    }
}
