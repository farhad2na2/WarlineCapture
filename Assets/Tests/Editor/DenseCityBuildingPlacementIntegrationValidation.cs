using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DenseCityBuildingPlacementIntegrationValidation
{
    private const string ScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";

    public static void RunFocusedValidation()
    {
        byte[] sourceHash = ComputeHash(ScenePath);
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RuntimeCityRAndDMapView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RuntimeCityRAndDMapView>(true))
                .ToArray();
            Assert.That(views, Has.Length.EqualTo(1));

            RuntimeCityRAndDMapView view = views[0];
            DenseMiddleEasternCityEditModeBuilder.Result result =
                RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(view);
            Assert.That(view.GeneratedRoot.position, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(view.GeneratedRoot.rotation, Quaternion.identity), Is.LessThan(0.0001f));
            Assert.That(view.GeneratedRoot.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(result.Buildings, Is.GreaterThan(0));
            Assert.That(result.SemanticBuildings, Is.GreaterThan(result.Buildings));
            Assert.That(result.SemanticBuildingAttachments, Is.GreaterThan(0));
            Assert.That(result.SemanticRoadShoulders, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalWaterExclusions, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalBankTerrains, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalParkTerrains, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalTrees, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalBushes, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalLights, Is.GreaterThan(0));
            Assert.That(result.SemanticCivicBuildings, Is.GreaterThan(0));
            Assert.That(result.SemanticCivicRoads, Is.GreaterThan(0));
            Assert.That(result.SemanticSurfaces, Is.GreaterThan(result.SemanticBuildings * 2));
            Debug.Log(
                $"[DenseCityBuildingPlacementIntegrationValidation] result=Passed " +
                $"districtBuildings={result.Buildings} semanticBuildings={result.SemanticBuildings} " +
                $"semanticAttachments={result.SemanticBuildingAttachments} " +
                $"surfaces={result.SemanticSurfaces} presentations={result.SemanticPresentations} " +
                $"roadShoulders={result.SemanticRoadShoulders} " +
                $"canalWaterExclusions={result.SemanticCanalWaterExclusions} " +
                $"canalBankTerrains={result.SemanticCanalBankTerrains} " +
                $"canalParkTerrains={result.SemanticCanalParkTerrains} " +
                $"canalTrees={result.SemanticCanalTrees} canalBushes={result.SemanticCanalBushes} " +
                $"canalLights={result.SemanticCanalLights} " +
                $"civicBuildings={result.SemanticCivicBuildings} " +
                $"civicRoads={result.SemanticCivicRoads}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DenseCityBuildingPlacementIntegrationValidation] result=Failed");
            ValidationExit.Exit(1);
            return;
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        byte[] finalHash = ComputeHash(ScenePath);
        if (!sourceHash.SequenceEqual(finalHash))
        {
            Debug.LogError("[DenseCityBuildingPlacementIntegrationValidation] accepted scene bytes changed.");
            ValidationExit.Exit(1);
            return;
        }

        ValidationExit.Exit(0);
    }

    private static byte[] ComputeHash(string path)
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(File.ReadAllBytes(path));
    }
}
