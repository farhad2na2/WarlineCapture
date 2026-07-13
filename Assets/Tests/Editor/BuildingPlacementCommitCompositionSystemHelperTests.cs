using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BuildingPlacementCommitCompositionSystemHelperTests
{
    private readonly List<GameObject> _objects = new();
    private GameObject _buildingRoot;

    public static void RunFocusedValidation()
    {
        RunCase(test => test.SinglePlacementWithoutAutoSelectReportsCommittedOutcome());
        RunCase(test => test.SingleRegistrationFailureReportsNoCommit());
        RunCase(test => test.WallPlacementCompleteSuccessReportsAllCommittedInstances());
        RunCase(test => test.WallPlacementRegistrationFailureRollsBackRegisteredSegments());
        RunCase(test => test.InvalidRequestReportsNoCommit());
        Debug.Log("[BuildingPlacementCommitFocusedValidation] result=Passed tests=5");
    }

    private static void RunCase(Action<BuildingPlacementCommitCompositionSystemHelperTests> testCase)
    {
        var test = new BuildingPlacementCommitCompositionSystemHelperTests();
        test.SetUp();
        try
        {
            testCase(test);
        }
        finally
        {
            test.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _buildingRoot = Track(new GameObject("BuildingPlacementCommitTestsRoot"));
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
                Object.DestroyImmediate(_objects[i]);
        }

        _objects.Clear();
        _buildingRoot = null;
    }

    [Test]
    public void SinglePlacementWithoutAutoSelectReportsCommittedOutcome()
    {
        BuildingDefinition definition = CreateDefinition(autoSelect: false);
        GameObject preview = Track(new GameObject("SinglePreview"));
        RuntimeBuildingEntity registeredBuilding = null;
        var context = CreateContext(
            hasGrid: false,
            createVisual: null,
            registerRuntimeBuilding: (committedDefinition, instance, origin, _) =>
            {
                registeredBuilding = CreateRuntimeBuilding(committedDefinition, instance, origin);
                return registeredBuilding;
            });

        BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome = new BuildingPlacementCommitCompositionSystemHelper()
            .CommitPlacement(CreateSingleRequest(definition, preview), context);

        Assert.IsTrue(outcome.PlacementCommitted);
        Assert.AreEqual(1, outcome.CommittedInstanceCount);
        Assert.AreEqual(1, outcome.ExpectedInstanceCount);
        Assert.IsTrue(outcome.FullyCommitted);
        Assert.IsNull(outcome.AutoSelectBuilding);
        Assert.AreSame(preview, registeredBuilding.Instance);
    }

    [Test]
    public void SingleRegistrationFailureReportsNoCommit()
    {
        BuildingDefinition definition = CreateDefinition(autoSelect: true);
        GameObject preview = Track(new GameObject("RejectedSinglePreview"));
        var context = CreateContext(
            hasGrid: false,
            createVisual: null,
            registerRuntimeBuilding: (_, _, _, _) => null);

        BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome = new BuildingPlacementCommitCompositionSystemHelper()
            .CommitPlacement(CreateSingleRequest(definition, preview), context);

        Assert.IsFalse(outcome.PlacementCommitted);
        Assert.AreEqual(0, outcome.CommittedInstanceCount);
        Assert.AreEqual(1, outcome.ExpectedInstanceCount);
        Assert.IsFalse(outcome.FullyCommitted);
        Assert.IsNull(outcome.AutoSelectBuilding);
    }

    [Test]
    public void WallPlacementCompleteSuccessReportsAllCommittedInstances()
    {
        BuildingDefinition definition = CreateDefinition(autoSelect: true);
        GameObject preview = Track(new GameObject("WallPreview"));
        var origins = new[] { new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(6, 3) };
        int positionedCount = 0;
        RuntimeBuildingEntity lastRegisteredBuilding = null;
        var context = CreateContext(
            hasGrid: true,
            createVisual: (_, parent) => CreateWallVisual(parent),
            registerRuntimeBuilding: (committedDefinition, instance, origin, _) =>
            {
                lastRegisteredBuilding = CreateRuntimeBuilding(committedDefinition, instance, origin);
                return lastRegisteredBuilding;
            },
            positionVisual: (_, _, _, _, _) => positionedCount++);

        BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome = new BuildingPlacementCommitCompositionSystemHelper()
            .CommitPlacement(CreateWallRequest(definition, preview, origins), context);

        Assert.IsTrue(outcome.PlacementCommitted);
        Assert.AreEqual(origins.Length, outcome.CommittedInstanceCount);
        Assert.AreEqual(origins.Length, outcome.ExpectedInstanceCount);
        Assert.IsTrue(outcome.FullyCommitted);
        Assert.AreEqual(origins.Length, positionedCount);
        Assert.AreSame(lastRegisteredBuilding, outcome.AutoSelectBuilding);
        Assert.IsTrue(preview == null);
    }

    [Test]
    public void WallPlacementRegistrationFailureRollsBackRegisteredSegments()
    {
        BuildingDefinition definition = CreateDefinition(autoSelect: false);
        GameObject preview = Track(new GameObject("PartialWallPreview"));
        var origins = new[]
        {
            new Vector2Int(1, 5),
            new Vector2Int(3, 5),
            new Vector2Int(5, 5),
            new Vector2Int(7, 5)
        };
        int registrationAttempt = 0;
        int rollbackCount = 0;
        GameObject rejectedVisual = null;
        var context = CreateContext(
            hasGrid: true,
            createVisual: (_, parent) => CreateWallVisual(parent),
            registerRuntimeBuilding: (committedDefinition, instance, origin, _) =>
            {
                registrationAttempt++;
                if (registrationAttempt == 3)
                {
                    rejectedVisual = instance;
                    return null;
                }

                return CreateRuntimeBuilding(committedDefinition, instance, origin);
            },
            rollbackRuntimeBuilding: building =>
            {
                rollbackCount++;
                return building != null;
            });

        BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome = new BuildingPlacementCommitCompositionSystemHelper()
            .CommitPlacement(CreateWallRequest(definition, preview, origins), context);

        Assert.IsFalse(outcome.PlacementCommitted);
        Assert.AreEqual(0, outcome.CommittedInstanceCount);
        Assert.AreEqual(origins.Length, outcome.ExpectedInstanceCount);
        Assert.IsFalse(outcome.FullyCommitted);
        Assert.AreEqual(3, registrationAttempt);
        Assert.AreEqual(2, rollbackCount);
        Assert.IsTrue(rejectedVisual == null);
        Assert.IsNull(outcome.AutoSelectBuilding);
        Assert.IsNotNull(preview);
    }

    [Test]
    public void InvalidRequestReportsNoCommit()
    {
        var request = new BuildingPlacementCommitCompositionSystemHelper.CommitRequest(
            definition: null,
            previewInstance: null,
            originCell: default,
            autoRotateVertical: false,
            isWall: false,
            hideCurrentWallPreview: false,
            committedWallRuns: null,
            currentWallOrigins: null,
            currentWallVertical: false);

        BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome = new BuildingPlacementCommitCompositionSystemHelper()
            .CommitPlacement(request, default);

        Assert.IsFalse(outcome.PlacementCommitted);
        Assert.AreEqual(0, outcome.CommittedInstanceCount);
        Assert.AreEqual(0, outcome.ExpectedInstanceCount);
        Assert.IsFalse(outcome.FullyCommitted);
        Assert.IsNull(outcome.AutoSelectBuilding);
    }

    private BuildingPlacementCommitCompositionSystemHelper.CommitContext CreateContext(
        bool hasGrid,
        BuildingPlacementCommitCompositionSystemHelper.CreateVisualDelegate createVisual,
        BuildingPlacementCommitCompositionSystemHelper.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
        BuildingPlacementCommitCompositionSystemHelper.RollbackRuntimeBuildingDelegate rollbackRuntimeBuilding = null,
        BuildingPlacementCommitCompositionSystemHelper.PositionVisualDelegate positionVisual = null)
    {
        return new BuildingPlacementCommitCompositionSystemHelper.CommitContext(
            _buildingRoot.transform,
            hasGrid,
            default,
            createVisual,
            positionVisual ?? ((_, _, _, _, _) => { }),
            registerRuntimeBuilding,
            rollbackRuntimeBuilding ?? (_ => true),
            CloneDefinitionWithFootprint,
            (_, _) => new Vector2Int(2, 2),
            (_, vertical) => vertical ? new Vector2Int(1, 2) : new Vector2Int(2, 1),
            Object.DestroyImmediate);
    }

    private static BuildingPlacementCommitCompositionSystemHelper.CommitRequest CreateSingleRequest(
        BuildingDefinition definition,
        GameObject preview)
    {
        return new BuildingPlacementCommitCompositionSystemHelper.CommitRequest(
            definition,
            preview,
            new Vector2Int(7, 9),
            autoRotateVertical: false,
            isWall: false,
            hideCurrentWallPreview: false,
            committedWallRuns: null,
            currentWallOrigins: null,
            currentWallVertical: false);
    }

    private static BuildingPlacementCommitCompositionSystemHelper.CommitRequest CreateWallRequest(
        BuildingDefinition definition,
        GameObject preview,
        IReadOnlyList<Vector2Int> origins)
    {
        return new BuildingPlacementCommitCompositionSystemHelper.CommitRequest(
            definition,
            preview,
            originCell: origins[0],
            autoRotateVertical: false,
            isWall: true,
            hideCurrentWallPreview: false,
            committedWallRuns: null,
            currentWallOrigins: origins,
            currentWallVertical: false);
    }

    private BuildingDefinition CreateDefinition(bool autoSelect)
    {
        return new BuildingDefinition
        {
            FootprintCells = new Vector2Int(2, 1),
            SpawnUnitPrefab = autoSelect ? Track(new GameObject("SpawnUnitPrefab")) : null
        };
    }

    private static BuildingDefinition CloneDefinitionWithFootprint(BuildingDefinition definition, Vector2Int footprint)
    {
        return new BuildingDefinition
        {
            FootprintCells = footprint,
            SpawnUnitPrefab = definition.SpawnUnitPrefab
        };
    }

    private static RuntimeBuildingEntity CreateRuntimeBuilding(
        BuildingDefinition definition,
        GameObject instance,
        Vector2Int origin)
    {
        return new RuntimeBuildingEntity
        {
            Definition = definition,
            Instance = instance,
            OriginCell = origin
        };
    }

    private GameObject CreateWallVisual(Transform parent)
    {
        GameObject instance = Track(new GameObject("WallSegment"));
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private GameObject Track(GameObject instance)
    {
        _objects.Add(instance);
        return instance;
    }
}
