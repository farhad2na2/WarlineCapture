using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapEntityPresentationReadinessValidatorTests
{
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new OperationMapEntityPresentationReadinessValidatorTests();
        Action[] tests =
        {
            suite.Readiness_AcceptsCompleteRoleAndIdentityOwnership,
            suite.Readiness_RejectsDuplicateSourceIdentity,
            suite.Readiness_RejectsIdentityUnderWrongRole,
            suite.Readiness_RejectsInactivePhysicsDescendant,
            suite.LegacyPlacementParity_AcceptsVehicleIdentityOwnedByUnitBaker,
            suite.LegacyPlacementParity_RejectsVehicleIdentityWithoutUnitBaker
        };
        for (int index = 0; index < tests.Length; index++)
        {
            suite.SetUp();
            tests[index]();
        }
        Debug.Log($"[OperationMapEntityPresentationReadinessValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void Readiness_AcceptsCompleteRoleAndIdentityOwnership()
    {
        Scene scene = SceneManager.GetActiveScene();
        CreateValidHierarchy(scene);

        Assert.That(
            OperationMapEntityPresentationReadinessValidator.TryValidateScene(
                scene, OperationMapId, 1, 1, 1, out string error),
            Is.True,
            error);
    }

    [Test]
    public void Readiness_RejectsDuplicateSourceIdentity()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform[] roots = CreateValidHierarchy(scene);
        CreateIdentity(
            roots[2],
            "DuplicateRenderOnly",
            SourceId(1),
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);

        Assert.That(
            OperationMapEntityPresentationReadinessValidator.TryValidateScene(
                scene, OperationMapId, 1, 1, 2, out string error),
            Is.False);
        StringAssert.Contains("Duplicate entity-presentation source identity", error);
    }

    [Test]
    public void Readiness_RejectsIdentityUnderWrongRole()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform[] roots = CreateValidHierarchy(scene);
        OperationMapEntityPresentationIdentityAuthoring identity =
            roots[0].GetComponentInChildren<OperationMapEntityPresentationIdentityAuthoring>(true);
        identity.transform.SetParent(roots[2], false);

        Assert.That(
            OperationMapEntityPresentationReadinessValidator.TryValidateScene(
                scene, OperationMapId, 1, 1, 1, out string error),
            Is.False);
        StringAssert.Contains("does not match its nearest role owner", error);
    }

    [Test]
    public void Readiness_RejectsInactivePhysicsDescendant()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform[] roots = CreateValidHierarchy(scene);
        var invalid = new GameObject("InactiveCollider");
        invalid.transform.SetParent(roots[2], false);
        invalid.AddComponent<BoxCollider>();
        invalid.SetActive(false);

        Assert.That(
            OperationMapEntityPresentationReadinessValidator.TryValidateScene(
                scene, OperationMapId, 1, 1, 1, out string error),
            Is.False);
        StringAssert.Contains("InactiveCollider", error);
        StringAssert.Contains("BoxCollider", error);
    }

    [Test]
    public void LegacyPlacementParity_AcceptsVehicleIdentityOwnedByUnitBaker()
    {
        AssertLegacyVehicleParity(unitBakerOwnsIdentity: true, expected: true);
    }

    [Test]
    public void LegacyPlacementParity_RejectsVehicleIdentityWithoutUnitBaker()
    {
        AssertLegacyVehicleParity(unitBakerOwnsIdentity: false, expected: false);
    }

    private static void AssertLegacyVehicleParity(bool unitBakerOwnsIdentity, bool expected)
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform vehicles = CreateRoleRoot(
            scene,
            "GameplayVehicles",
            OperationMapEntityPresentationRole.GameplayVehicles);
        var prefab = new GameObject("VehiclePrefab");
        prefab.AddComponent<UnitGridAuthoring>();
        var owner = new GameObject("VehicleOwner");
        owner.transform.SetParent(vehicles, false);
        if (unitBakerOwnsIdentity)
            owner.AddComponent<UnitGridAuthoring>();
        Transform identityParent = unitBakerOwnsIdentity ? owner.transform : vehicles;
        CreateIdentity(
            identityParent,
            "Model",
            SourceId(2),
            OperationMapEntityPresentationRole.GameplayVehicles,
            0);

        var buildingConfig = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        buildingConfig.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>());
        var vehicleConfig = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        vehicleConfig.EditorSetPlacements(new List<MapVehiclePlacementConfigEntry>
        {
            new(
                "Map/Vehicles/Vehicle",
                "Vehicle",
                prefab,
                1,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                Vector3.one)
        });
        var vehicleRows = new[]
        {
            new OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport
            {
                placementIndex = 0,
                sourcePath = "Map/Vehicles/Vehicle",
                vehiclePrefabPath = string.Empty,
                authoredJoinResolveState = "Exact",
                authoredSourceGlobalObjectId = SourceId(2),
                conversionDisposition = "AlreadyProducesEcsGameplayAndRender"
            }
        };

        bool result = OperationMapEntityPresentationReadinessValidator.TryValidateLegacyPlacementParity(
            scene,
            buildingConfig,
            Array.Empty<OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildingPlacementReport>(),
            vehicleConfig,
            vehicleRows,
            out string error);

        Assert.That(result, Is.EqualTo(expected), error);
        if (!expected)
            StringAssert.Contains("ECS unit Baker owner", error);
        UnityEngine.Object.DestroyImmediate(buildingConfig);
        UnityEngine.Object.DestroyImmediate(vehicleConfig);
        UnityEngine.Object.DestroyImmediate(prefab);
    }

    private static Transform[] CreateValidHierarchy(Scene scene)
    {
        Transform buildings = CreateRoleRoot(scene, "GameplayBuildings", OperationMapEntityPresentationRole.GameplayBuildings);
        Transform vehicles = CreateRoleRoot(scene, "GameplayVehicles", OperationMapEntityPresentationRole.GameplayVehicles);
        Transform renderOnly = CreateRoleRoot(scene, "RenderOnly", OperationMapEntityPresentationRole.RenderOnly);
        CreateIdentity(buildings, "Building", SourceId(1), OperationMapEntityPresentationRole.GameplayBuildings, 0);
        CreateIdentity(vehicles, "Vehicle", SourceId(2), OperationMapEntityPresentationRole.GameplayVehicles, 0);
        CreateIdentity(
            renderOnly,
            "RenderOnlyOwner",
            SourceId(3),
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
        return new[] { buildings, vehicles, renderOnly };
    }

    private static Transform CreateRoleRoot(
        Scene scene,
        string name,
        OperationMapEntityPresentationRole role)
    {
        var owner = new GameObject(name);
        SceneManager.MoveGameObjectToScene(owner, scene);
        OperationMapEntityPresentationRootAuthoring root =
            owner.AddComponent<OperationMapEntityPresentationRootAuthoring>();
        var serialized = new SerializedObject(root);
        serialized.FindProperty("operationMapId").stringValue = OperationMapId;
        serialized.FindProperty("role").enumValueIndex = (int)role;
        serialized.FindProperty("schemaVersion").intValue =
            OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion;
        serialized.FindProperty("migrationRecordSetHash").stringValue = Hash;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return owner.transform;
    }

    private static void CreateIdentity(
        Transform parent,
        string name,
        string sourceId,
        OperationMapEntityPresentationRole role,
        int placementIndex)
    {
        var owner = new GameObject(name);
        owner.transform.SetParent(parent, false);
        owner.AddComponent<OperationMapEntityPresentationIdentityAuthoring>()
            .ConfigureForEditor(OperationMapId, sourceId, role, placementIndex);
    }

    private static string SourceId(int localId) =>
        $"GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-{localId}-0";
}
