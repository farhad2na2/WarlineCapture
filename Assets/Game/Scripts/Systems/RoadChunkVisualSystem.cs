using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CombinedRoadVisualData = RoadFootprintQuerySystem.CombinedRoadVisualData;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using VariantData = RoadVisualVariantSystem.VariantData;

public sealed class RoadChunkVisualSystem
{
    public readonly struct Context
    {
        public readonly Dictionary<Vector2Int, RoadTileData> RoadTiles;
        public readonly Dictionary<RoadVisualType, CombinedRoadVisualData> VisualData;
        public readonly HashSet<Vector2Int> AutobahnCells;
        public readonly HashSet<Vector2Int> AutobahnConnectorCells;
        public readonly Transform RoadRoot;
        public readonly Vector3 GridOrigin;
        public readonly float BuildPlaneY;
        public readonly float RoadGridSize;
        public readonly int ChunkSizeInCells;

        public Context(
            Dictionary<Vector2Int, RoadTileData> roadTiles,
            Dictionary<RoadVisualType, CombinedRoadVisualData> visualData,
            HashSet<Vector2Int> autobahnCells,
            HashSet<Vector2Int> autobahnConnectorCells,
            Transform roadRoot,
            Vector3 gridOrigin,
            float buildPlaneY,
            float roadGridSize,
            int chunkSizeInCells)
        {
            RoadTiles = roadTiles;
            VisualData = visualData;
            AutobahnCells = autobahnCells;
            AutobahnConnectorCells = autobahnConnectorCells;
            RoadRoot = roadRoot;
            GridOrigin = gridOrigin;
            BuildPlaneY = buildPlaneY;
            RoadGridSize = roadGridSize;
            ChunkSizeInCells = chunkSizeInCells;
        }
    }

    private sealed class ChunkRenderData
    {
        public GameObject GameObject;
        public Mesh Mesh;
    }

    private readonly Dictionary<Vector2Int, ChunkRenderData> _chunks = new();
    private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _chunkCells = new();
    private readonly HashSet<Vector2Int> _dirtyChunks = new();

    public void DisposeChunks()
    {
        foreach (var chunk in _chunks.Values)
            DestroyChunk(chunk);

        _chunks.Clear();
        _chunkCells.Clear();
        _dirtyChunks.Clear();
    }

    public void ClearChunks()
    {
        DisposeChunks();
    }

    public void AddCellToChunk(Context context, Vector2Int cell)
    {
        Vector2Int chunkCoord = GetChunkCoord(context, cell);
        if (!_chunkCells.TryGetValue(chunkCoord, out var cells))
        {
            cells = new HashSet<Vector2Int>();
            _chunkCells.Add(chunkCoord, cells);
        }

        cells.Add(cell);
        MarkChunkDirty(chunkCoord);
    }

    public void RemoveCellFromChunk(Context context, Vector2Int cell)
    {
        Vector2Int chunkCoord = GetChunkCoord(context, cell);
        if (_chunkCells.TryGetValue(chunkCoord, out var cells))
        {
            cells.Remove(cell);
            if (cells.Count == 0)
                _chunkCells.Remove(chunkCoord);
        }

        MarkChunkDirty(chunkCoord);
    }

    public void RebuildDirtyChunks(Context context)
    {
        if (_dirtyChunks.Count == 0 || context.RoadRoot == null)
            return;

        foreach (var chunkCoord in _dirtyChunks)
            RebuildChunk(context, chunkCoord);

        _dirtyChunks.Clear();
    }

    public static Vector3 GetPlacementPosition(Context context, Vector2Int cell, VariantData variant)
    {
        Vector3 basePosition = context.GridOrigin + new Vector3(
            cell.x * context.RoadGridSize,
            context.BuildPlaneY,
            cell.y * context.RoadGridSize);
        Vector3[] corners =
        {
            new(0f, 0f, 0f),
            new(context.RoadGridSize, 0f, 0f),
            new(0f, 0f, context.RoadGridSize),
            new(context.RoadGridSize, 0f, context.RoadGridSize)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 transformed = variant.Rotation * Vector3.Scale(corners[i], variant.Scale);
            if (transformed.x < minX)
                minX = transformed.x;
            if (transformed.z < minZ)
                minZ = transformed.z;
        }

        return basePosition - new Vector3(minX, 0f, minZ);
    }

    private void MarkChunkDirty(Vector2Int chunkCoord)
    {
        _dirtyChunks.Add(chunkCoord);
    }

    private void RebuildChunk(Context context, Vector2Int chunkCoord)
    {
        var materialOrder = new List<Material>();
        var combinesByMaterial = new Dictionary<Material, List<CombineInstance>>();

        if (!_chunkCells.TryGetValue(chunkCoord, out var chunkCellSet) || chunkCellSet.Count == 0)
        {
            RemoveRenderableChunk(chunkCoord);
            return;
        }

        foreach (var cell in chunkCellSet)
        {
            if (!context.RoadTiles.TryGetValue(cell, out var tile))
                continue;

            if (IsSpecialRoadCell(context, cell))
                continue;

            if (!context.VisualData.TryGetValue(tile.Type, out var visualData) ||
                visualData.Mesh == null ||
                visualData.Materials == null)
            {
                continue;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(
                GetPlacementPosition(context, cell, new VariantData(tile.Rotation, tile.Scale)),
                tile.Rotation,
                tile.Scale);

            for (int subMeshIndex = 0; subMeshIndex < visualData.Materials.Length; subMeshIndex++)
            {
                Material material = visualData.Materials[subMeshIndex];
                if (material == null)
                    continue;

                if (!combinesByMaterial.TryGetValue(material, out var combines))
                {
                    combines = new List<CombineInstance>();
                    combinesByMaterial.Add(material, combines);
                    materialOrder.Add(material);
                }

                combines.Add(new CombineInstance
                {
                    mesh = visualData.Mesh,
                    subMeshIndex = subMeshIndex,
                    transform = matrix
                });
            }
        }

        if (materialOrder.Count == 0)
        {
            RemoveRenderableChunk(chunkCoord);
            return;
        }

        Mesh combinedMesh = BuildChunkMesh(chunkCoord, materialOrder, combinesByMaterial);
        if (combinedMesh == null)
            return;

        if (!_chunks.TryGetValue(chunkCoord, out var chunk))
        {
            chunk = new ChunkRenderData
            {
                GameObject = new GameObject($"RoadChunk_{chunkCoord.x}_{chunkCoord.y}")
            };
            chunk.GameObject.transform.SetParent(context.RoadRoot, false);
            chunk.GameObject.AddComponent<MeshFilter>();
            chunk.GameObject.AddComponent<MeshRenderer>();
            _chunks.Add(chunkCoord, chunk);
        }

        if (chunk.Mesh != null)
            UnityEngine.Object.Destroy(chunk.Mesh);

        chunk.Mesh = combinedMesh;
        var filter = chunk.GameObject.GetComponent<MeshFilter>();
        filter.sharedMesh = chunk.Mesh;

        var renderer = chunk.GameObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = materialOrder.ToArray();
    }

    private void RemoveRenderableChunk(Vector2Int chunkCoord)
    {
        if (!_chunks.TryGetValue(chunkCoord, out var chunk))
            return;

        DestroyChunk(chunk);
        _chunks.Remove(chunkCoord);
    }

    private static void DestroyChunk(ChunkRenderData chunk)
    {
        if (chunk.Mesh != null)
            UnityEngine.Object.Destroy(chunk.Mesh);
        if (chunk.GameObject != null)
            UnityEngine.Object.Destroy(chunk.GameObject);
    }

    private static bool IsSpecialRoadCell(Context context, Vector2Int cell) =>
        context.AutobahnCells.Contains(cell) || context.AutobahnConnectorCells.Contains(cell);

    private static Mesh BuildChunkMesh(
        Vector2Int chunkCoord,
        List<Material> materialOrder,
        Dictionary<Material, List<CombineInstance>> combinesByMaterial)
    {
        var submeshCombines = new CombineInstance[materialOrder.Count];
        for (int i = 0; i < materialOrder.Count; i++)
        {
            var combines = combinesByMaterial[materialOrder[i]];
            Mesh submeshMesh = new()
            {
                name = $"RoadChunk_{chunkCoord.x}_{chunkCoord.y}_{i}"
            };
            submeshMesh.indexFormat = IndexFormat.UInt32;
            submeshMesh.CombineMeshes(combines.ToArray(), true, true, false);
            submeshCombines[i] = new CombineInstance
            {
                mesh = submeshMesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }

        Mesh combinedMesh = new()
        {
            name = $"RoadChunk_{chunkCoord.x}_{chunkCoord.y}_Combined",
            indexFormat = IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(submeshCombines, false, false, false);

        for (int i = 0; i < submeshCombines.Length; i++)
            UnityEngine.Object.Destroy(submeshCombines[i].mesh);

        return combinedMesh;
    }

    private static Vector2Int GetChunkCoord(Context context, Vector2Int cell)
    {
        int chunkSize = Mathf.Max(1, context.ChunkSizeInCells);
        return new Vector2Int(
            Mathf.FloorToInt((float)cell.x / chunkSize),
            Mathf.FloorToInt((float)cell.y / chunkSize));
    }
}
