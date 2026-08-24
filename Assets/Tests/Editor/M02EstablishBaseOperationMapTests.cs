using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseOperationMapTests
{
    private const string Marker =
        "[M02EstablishBaseOperationMapValidation] result=Passed tests=10";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseForwardPostWindowValidation.RunFocusedValidation();
            M02EstablishBaseOperationMapTests tests = new();
            tests.RegenerationIsByteStable();
            tests.MetadataAndLocalReferencesValidate();
            tests.ExactAcceptedPhysicalSourceIsReused();
            tests.PhysicalSourceFilesRemainFrozen();
            tests.WorldGridSurfaceAndNavigationRemainCanonical();
            tests.PlayableAndCameraBoundsAreCroppedToForwardPost();
            tests.CameraAndMinimapMetadataIsMissionScoped();
            tests.RequiredScenarioAnchorsResolveWithExactKinds();
            tests.AdditionalResourceAndCommsAnchorsResolve();
            tests.BuildLotRouteAndSightlinesPassFocusedValidation();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseOperationMapValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RegenerationIsByteStable()
    {
        string before = HashFile(M02EstablishBaseForwardPostWindowValidation.DefinitionPath);
        M02EstablishBaseForwardPostWindowValidation.RunFocusedValidation();
        Assert.AreEqual(before, HashFile(M02EstablishBaseForwardPostWindowValidation.DefinitionPath));
    }

    [Test]
    public void MetadataAndLocalReferencesValidate()
    {
        OperationMapDefinition map = Map();
        Assert.IsTrue(map.TryValidateMetadata(out string error), error);
        Assert.IsTrue(map.TryValidateLocalContentReferences(out error), error);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.MapId, map.OperationMapId);
    }

    [Test]
    public void ExactAcceptedPhysicalSourceIsReused()
    {
        OperationMapDefinition map = Map();
        OperationMapDefinition source = Source();
        Assert.AreEqual(source.OperationMapId, map.SourceBinding.SourceOperationMapId);
        Assert.AreEqual(source.SourceIdentityHash, map.SourceBinding.SourceIdentityHash);
        Assert.AreEqual(source.ContentHash, map.SourceBinding.SourceContentHash);
        Assert.AreEqual(source.SourceSceneReference.AssetGUID, map.SourceSceneReference.AssetGUID);
        Assert.AreEqual(source.MapSurfaceDataReference.AssetGUID, map.MapSurfaceDataReference.AssetGUID);
        Assert.AreEqual(source.MinimapRasterReference.AssetGUID, map.MinimapRasterReference.AssetGUID);
    }

    [Test]
    public void PhysicalSourceFilesRemainFrozen()
    {
        Assert.AreEqual(
            M02EstablishBaseForwardPostWindowValidation.SourceDefinitionSha256,
            HashFile(M02EstablishBaseForwardPostWindowValidation.SourceDefinitionPath));
        Assert.AreEqual(
            M02EstablishBaseForwardPostWindowValidation.BuildingPlacementsSha256,
            HashFile(M02EstablishBaseForwardPostWindowValidation.BuildingPlacementsPath));
    }

    [Test]
    public void WorldGridSurfaceAndNavigationRemainCanonical()
    {
        OperationMapDefinition map = Map();
        OperationMapDefinition source = Source();
        Assert.AreEqual(source.Bounds.WorldMin, map.Bounds.WorldMin);
        Assert.AreEqual(source.Bounds.WorldMax, map.Bounds.WorldMax);
        Assert.AreEqual(source.GridMetadata.AssetGuid, map.GridMetadata.AssetGuid);
        Assert.AreEqual(source.GridMetadata.ContentHash, map.GridMetadata.ContentHash);
        Assert.AreEqual(source.SurfaceMetadata.ContentHash, map.SurfaceMetadata.ContentHash);
        Assert.AreEqual(source.NavigationMetadata.AuthoredSubSceneGuid,
            map.NavigationMetadata.AuthoredSubSceneGuid);
    }

    [Test]
    public void PlayableAndCameraBoundsAreCroppedToForwardPost()
    {
        OperationMapDefinition map = Map();
        RectInt window = M02EstablishBaseForwardPostWindowValidation.PlayableWindow;
        Assert.AreEqual(new Vector2(window.xMin, window.yMin),
            new Vector2(map.Bounds.PlayableMin.x, map.Bounds.PlayableMin.z));
        Assert.AreEqual(new Vector2(window.xMax, window.yMax),
            new Vector2(map.Bounds.PlayableMax.x, map.Bounds.PlayableMax.z));
        Assert.AreEqual(new Vector2(window.xMin, window.yMin),
            new Vector2(map.Bounds.CameraMin.x, map.Bounds.CameraMin.z));
        Assert.AreEqual(new Vector2(window.xMax, window.yMax),
            new Vector2(map.Bounds.CameraMax.x, map.Bounds.CameraMax.z));
    }

    [Test]
    public void CameraAndMinimapMetadataIsMissionScoped()
    {
        OperationMapDefinition map = Map();
        Assert.AreEqual(2, map.Cameras.Length);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.PlanningCameraId,
            map.PlanningCameraId);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.BattleCameraId,
            map.BattleCameraId);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.MinimapId,
            map.Minimap.MinimapId);
        Assert.AreEqual(new Vector2(
                M02EstablishBaseForwardPostWindowValidation.PlayableWindow.width,
                M02EstablishBaseForwardPostWindowValidation.PlayableWindow.height),
            map.Minimap.ProjectionSize);
    }

    [Test]
    public void RequiredScenarioAnchorsResolveWithExactKinds()
    {
        OperationMapDefinition map = Map();
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        Assert.IsNotNull(scenario);
        Dictionary<string, OperationMapAnchorKind> anchors = AnchorKinds(map);
        foreach (ScenarioAnchorRequirementConfig required in scenario.RequiredAnchors)
        {
            Assert.IsTrue(anchors.TryGetValue(required.AnchorId, out OperationMapAnchorKind kind),
                required.AnchorId);
            Assert.AreEqual(required.Kind, kind, required.AnchorId);
        }
    }

    [Test]
    public void AdditionalResourceAndCommsAnchorsResolve()
    {
        Dictionary<string, OperationMapAnchorKind> anchors = AnchorKinds(Map());
        Assert.AreEqual(OperationMapAnchorKind.Resource,
            anchors["anchor.ch01.m02.resource_focus"]);
        Assert.AreEqual(OperationMapAnchorKind.Objective,
            anchors["anchor.ch01.m02.comms_focus"]);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.ExpectedAnchorCount,
            anchors.Count);
    }

    [Test]
    public void BuildLotRouteAndSightlinesPassFocusedValidation()
    {
        RectInt lot = M02EstablishBaseForwardPostWindowValidation.ValidateCurrentDefinition();
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.BuildLotSize, lot.size);
        Assert.IsTrue(M02EstablishBaseForwardPostWindowValidation.PlayableWindow.Contains(lot.min));
        Assert.IsTrue(M02EstablishBaseForwardPostWindowValidation.PlayableWindow.Contains(
            new Vector2Int(lot.xMax - 1, lot.yMax - 1)));
    }

    private static Dictionary<string, OperationMapAnchorKind> AnchorKinds(OperationMapDefinition map)
    {
        Dictionary<string, OperationMapAnchorKind> anchors = new(StringComparer.Ordinal);
        foreach (OperationMapAnchorConfig anchor in map.Anchors)
            Assert.IsTrue(anchors.TryAdd(anchor.AnchorId, anchor.Kind), anchor.AnchorId);
        return anchors;
    }

    private static OperationMapDefinition Map() => Load(
        M02EstablishBaseForwardPostWindowValidation.DefinitionPath);

    private static OperationMapDefinition Source() => Load(
        M02EstablishBaseForwardPostWindowValidation.SourceDefinitionPath);

    private static OperationMapDefinition Load(string path)
    {
        OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
        Assert.IsNotNull(map, path);
        return map;
    }

    private static string HashFile(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty).ToLowerInvariant();
    }
}
