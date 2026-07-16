using Game.Editor;
using NUnit.Framework;
using UnityEditor;

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
}
