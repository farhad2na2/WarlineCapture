using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCitySurfaceProxyBuilderTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCitySurfaceProxyBuilderTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private const string OutputRoot = TempRoot + "/Primary/" + OperationMapId + "/Candidate/" + Hash;
    private const string OutputFolder = OutputRoot + "/SurfaceProxies";
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private static readonly Rect MapSurfaceBounds = new(-10f, -10f, 100f, 100f);
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCitySurfaceProxyBuilderTests();
        Action[] tests =
        {
            suite.Build_PartitionsRecordsAndCreatesBakeOnlyMeshes,
            suite.Build_MergesOnlyAdjacentCoplanarRectangles,
            suite.Build_IsDeterministicAcrossEquivalentRecordSets,
            suite.Build_OutOfBoundsPolygonLeavesNoCandidateOutput,
            suite.Build_InvalidConcavePolygonLeavesNoCandidateOutput
        };

        for (int index = 0; index < tests.Length; index++)
        {
            suite.SetUp();
            try
            {
                tests[index]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[DenseCitySurfaceProxyValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        CloseSceneIfLoaded(EntityScenePath);
        if (SceneManager.GetSceneByPath(MapScenePath).isLoaded)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void Build_PartitionsRecordsAndCreatesBakeOnlyMeshes()
    {
        (Scene mapScene, Scene entityScene, DenseCityGeneratedRootAuthoring mapRoot) = CreateScenePair("primary");
        using DenseCityGenerationRecordSet records = CreateRepresentativeRecords();

        DenseCitySurfaceProxyBuildResult result =
            DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder);

        Assert.That(result.Records, Is.EqualTo(7));
        Assert.That(result.Partitions, Is.EqualTo(6));
        Assert.That(result.Vertices, Is.EqualTo(24));
        Assert.That(result.Triangles, Is.EqualTo(12));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.True);
        Assert.That(AssetDatabase.FindAssets("t:Mesh", new[] { OutputFolder }), Has.Length.EqualTo(6));
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Has.Length.EqualTo(6));
        Assert.That(mapRoot.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Collider2D>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);
        Assert.That(CountPartitions(mapRoot, "Terrain"), Is.EqualTo(2));
        Assert.That(CountPartitions(mapRoot, "Roads"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Bridges"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Ramps"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Blockers"), Is.EqualTo(1));

        var metadata = new HashSet<string>(StringComparer.Ordinal);
        foreach (MeshFilter filter in mapRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            MapBakeGroupAuthoring owner = filter.GetComponent<MapBakeGroupAuthoring>();
            Assert.That(owner, Is.Not.Null);
            Assert.That(filter.transform.parent.GetComponent<MapBakeGroupAuthoring>().Role, Is.EqualTo(owner.Role));
            metadata.Add($"{owner.Role}:{owner.LayerId}:{(uint)owner.MovementMask:x8}");
            AssertMeshFacesUp(filter.sharedMesh);
        }
        Assert.That(metadata, Does.Contain("Terrain:0:00000001"));
        Assert.That(metadata, Does.Contain("Terrain:0:00000003"));
        Assert.That(metadata, Does.Contain("Road:0:00000003"));
        Assert.That(metadata, Does.Contain("Bridge:1:00000001"));
        Assert.That(metadata, Does.Contain("Ramp:1:00000001"));
        Assert.That(metadata, Does.Contain("Blocker:0:00000000"));
        MeshFilter mergedTerrain = Array.Find(
            mapRoot.GetComponentsInChildren<MeshFilter>(true),
            filter =>
            {
                MapBakeGroupAuthoring owner = filter.GetComponent<MapBakeGroupAuthoring>();
                return owner.Role == MapBakeGroupRole.Terrain && (uint)owner.MovementMask == 1;
            });
        Assert.That(mergedTerrain.sharedMesh.vertexCount, Is.EqualTo(4));
        Assert.That(
            DenseCitySemanticHierarchyBuilder.TryValidate(
                mapScene,
                entityScene,
                "dense-city:surface-proxy:primary",
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void Build_MergesOnlyAdjacentCoplanarRectangles()
    {
        var (_, _, mapRoot) = CreateScenePair("merge-boundary");
        using var records = new DenseCityGenerationRecordSet(1, 5, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(0f),
            1,
            0,
            Vector2Int.zero,
            2f));
        records.Add(CreateSurface(
            2,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(4f),
            1,
            0,
            Vector2Int.zero,
            2f));
        records.Add(CreateSurface(
            3,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(8f),
            1,
            0,
            Vector2Int.zero,
            3f));
        records.Add(CreateSurface(
            4,
            DenseCitySurfaceRecordKind.Road,
            Rectangle(20f),
            3,
            0,
            Vector2Int.one,
            1f));
        records.Add(CreateSurface(
            5,
            DenseCitySurfaceRecordKind.Road,
            Rectangle(24f),
            3,
            0,
            Vector2Int.one,
            1f));
        records.Seal();

        DenseCitySurfaceProxyBuildResult result =
            DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder);

        Assert.That(result.Partitions, Is.EqualTo(2));
        Assert.That(result.Records, Is.EqualTo(5));
        Assert.That(result.Vertices, Is.EqualTo(12));
        Assert.That(result.Triangles, Is.EqualTo(6));
        MeshFilter[] filters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
        Assert.That(filters, Has.Length.EqualTo(2));
        MeshFilter road = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Road);
        Assert.That(road.sharedMesh.vertexCount, Is.EqualTo(4));
    }

    [Test]
    public void Build_IsDeterministicAcrossEquivalentRecordSets()
    {
        var (_, firstEntity, firstRoot) = CreateScenePair("first");
        using DenseCityGenerationRecordSet firstRecords = CreateRepresentativeRecords();
        DenseCitySurfaceProxyBuilder.Build(
            firstRecords,
            firstRoot,
            OperationMapId,
            MapSurfaceBounds,
            TempRoot + "/First/" + OperationMapId + "/Candidate/" + Hash + "/SurfaceProxies");
        string firstSignature = CreateMeshSignature(firstRoot);
        EditorSceneManager.CloseScene(firstEntity, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        (Scene secondMap, Scene secondEntity, DenseCityGeneratedRootAuthoring secondRoot) =
            CreateScenePair("second");
        using DenseCityGenerationRecordSet secondRecords = CreateRepresentativeRecords();
        DenseCitySurfaceProxyBuilder.Build(
            secondRecords,
            secondRoot,
            OperationMapId,
            MapSurfaceBounds,
            TempRoot + "/Second/" + OperationMapId + "/Candidate/" + Hash + "/SurfaceProxies");

        Assert.That(CreateMeshSignature(secondRoot), Is.EqualTo(firstSignature));
        Assert.That(secondMap.IsValid(), Is.True);
        Assert.That(secondEntity.IsValid(), Is.True);
    }

    [Test]
    public void Build_OutOfBoundsPolygonLeavesNoCandidateOutput()
    {
        var (_, _, mapRoot) = CreateScenePair("bounds");
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(88f),
            1,
            0,
            Vector2Int.zero));
        records.Seal();

        Assert.That(
            () => DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder),
            Throws.InvalidOperationException.With.Message.Contains("map bounds"));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.False);
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Is.Empty);
    }

    [Test]
    public void Build_InvalidConcavePolygonLeavesNoCandidateOutput()
    {
        var (_, _, mapRoot) = CreateScenePair("invalid");
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 4f),
                new Vector2(2f, 2f),
                new Vector2(4f, 4f),
                new Vector2(4f, 0f)
            },
            1,
            0,
            Vector2Int.zero));
        records.Seal();

        Assert.That(
            () => DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder),
            Throws.InvalidOperationException.With.Message.Contains("convex"));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.False);
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Is.Empty);
    }

    private static DenseCityGenerationRecordSet CreateRepresentativeRecords()
    {
        var records = new DenseCityGenerationRecordSet(1, 7, 1);
        records.Add(CreateSurface(7, DenseCitySurfaceRecordKind.Blocker, Rectangle(60f), 0, 0, new Vector2Int(2, 3)));
        records.Add(CreateSurface(2, DenseCitySurfaceRecordKind.Terrain, Rectangle(4f), 1, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(6, DenseCitySurfaceRecordKind.Ramp, Rectangle(50f), 1, 1, new Vector2Int(2, 3)));
        records.Add(CreateSurface(1, DenseCitySurfaceRecordKind.Terrain, Rectangle(0f), 1, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(4, DenseCitySurfaceRecordKind.Road, Rectangle(30f), 3, 0, new Vector2Int(1, 2)));
        records.Add(CreateSurface(3, DenseCitySurfaceRecordKind.Terrain, Rectangle(20f), 3, 0, Vector2Int.zero));
        records.Add(CreateSurface(5, DenseCitySurfaceRecordKind.Bridge, Rectangle(40f), 1, 1, new Vector2Int(2, 3)));
        records.Seal();
        return records;
    }

    private static DenseCitySurfaceBakeRecord CreateSurface(
        int sequence,
        DenseCitySurfaceRecordKind kind,
        IReadOnlyList<Vector2> polygon,
        uint movementMask,
        int layer,
        Vector2Int chunk,
        float? elevation = null) =>
        new(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                3,
                "surface-" + kind.ToString().ToLowerInvariant(),
                sequence,
                SourceGuid,
                sequence),
            kind,
            polygon,
            elevation ?? sequence * 0.25f,
            movementMask,
            layer,
            chunk);

    private static Vector2[] Rectangle(float x) =>
        new[]
        {
            new Vector2(x, 0f),
            new Vector2(x + 4f, 0f),
            new Vector2(x + 4f, 3f),
            new Vector2(x, 3f)
        };

    private static void AssertMeshFacesUp(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        for (int index = 0; index < triangles.Length; index += 3)
        {
            Vector3 normal = Vector3.Cross(
                vertices[triangles[index + 1]] - vertices[triangles[index]],
                vertices[triangles[index + 2]] - vertices[triangles[index]]);
            Assert.That(normal.y, Is.GreaterThan(0f));
        }
    }

    private static string CreateMeshSignature(DenseCityGeneratedRootAuthoring root)
    {
        var signature = new System.Text.StringBuilder();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        Array.Sort(filters, (left, right) => string.CompareOrdinal(left.name, right.name));
        foreach (MeshFilter filter in filters)
        {
            MapBakeGroupAuthoring group = filter.GetComponent<MapBakeGroupAuthoring>();
            signature.Append(filter.name).Append('|').Append(group.Role).Append('|')
                .Append(group.LayerId).Append('|').Append((uint)group.MovementMask).Append('|');
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
                signature.Append(vertex.x).Append(',').Append(vertex.y).Append(',').Append(vertex.z).Append(';');
            signature.Append('|').AppendJoin(',', filter.sharedMesh.triangles).AppendLine();
        }
        return signature.ToString();
    }

    private static int CountPartitions(DenseCityGeneratedRootAuthoring root, string roleName) =>
        root.transform.Find("BakeSources/" + roleName).childCount;

    private static (Scene MapScene, Scene EntityScene, DenseCityGeneratedRootAuthoring MapRoot)
        CreateScenePair(string suffix)
    {
        Scene mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(mapScene, MapScenePath), Is.True);
        Scene entityScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Assert.That(EditorSceneManager.SaveScene(entityScene, EntityScenePath), Is.True);
        var roots = DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            "dense-city:surface-proxy:" + suffix,
            "dense-city-v1",
            1,
            42,
            Hash);
        return (mapScene, entityScene, roots.MapBakeSource);
    }

    private static void CloseSceneIfLoaded(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
