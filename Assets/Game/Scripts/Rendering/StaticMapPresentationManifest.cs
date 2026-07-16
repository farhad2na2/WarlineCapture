using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering
{
    [Serializable]
    public sealed class StaticMapPresentationMaterialEntry
    {
        [SerializeField] private Material material;
        [SerializeField] private string assetGuid;
        [SerializeField] private long localId;

        public Material Material => material;
        public string AssetGuid => assetGuid;
        public long LocalId => localId;

        public StaticMapPresentationMaterialEntry(Material material, string assetGuid, long localId)
        {
            this.material = material;
            this.assetGuid = assetGuid;
            this.localId = localId;
        }
    }

    [Serializable]
    public sealed class StaticMapPresentationSourceEntry
    {
        [SerializeField] private string sourceGlobalObjectId;
        [SerializeField] private string sourceHierarchyPath;
        [SerializeField] private string sourceDependencyHash;
        [SerializeField] private string chunkId;
        [SerializeField] private string generatedObjectName;
        [SerializeField] private Bounds worldBounds;
        [SerializeField] private Mesh mesh;
        [SerializeField] private string meshAssetGuid;
        [SerializeField] private long meshLocalId;
        [SerializeField] private List<StaticMapPresentationMaterialEntry> materials = new();
        [SerializeField] private bool overlaySource;

        public string SourceGlobalObjectId => sourceGlobalObjectId;
        public string SourceHierarchyPath => sourceHierarchyPath;
        public string SourceDependencyHash => sourceDependencyHash;
        public string ChunkId => chunkId;
        public string GeneratedObjectName => generatedObjectName;
        public Bounds WorldBounds => worldBounds;
        public Mesh Mesh => mesh;
        public string MeshAssetGuid => meshAssetGuid;
        public long MeshLocalId => meshLocalId;
        public IReadOnlyList<StaticMapPresentationMaterialEntry> Materials => materials;
        public bool OverlaySource => overlaySource;

        public StaticMapPresentationSourceEntry(
            string sourceGlobalObjectId,
            string sourceHierarchyPath,
            string sourceDependencyHash,
            string chunkId,
            string generatedObjectName,
            Bounds worldBounds,
            Mesh mesh,
            string meshAssetGuid,
            long meshLocalId,
            List<StaticMapPresentationMaterialEntry> materials,
            bool overlaySource)
        {
            this.sourceGlobalObjectId = sourceGlobalObjectId;
            this.sourceHierarchyPath = sourceHierarchyPath;
            this.sourceDependencyHash = sourceDependencyHash;
            this.chunkId = chunkId;
            this.generatedObjectName = generatedObjectName;
            this.worldBounds = worldBounds;
            this.mesh = mesh;
            this.meshAssetGuid = meshAssetGuid;
            this.meshLocalId = meshLocalId;
            this.materials = materials ?? new List<StaticMapPresentationMaterialEntry>();
            this.overlaySource = overlaySource;
        }
    }

    [Serializable]
    public sealed class StaticMapPresentationChunkEntry
    {
        [SerializeField] private string chunkId;
        [SerializeField] private string scenePath;
        [SerializeField] private Bounds worldBounds;
        [SerializeField] private int sourceStartIndex;
        [SerializeField] private int sourceCount;

        public string ChunkId => chunkId;
        public string ScenePath => scenePath;
        public Bounds WorldBounds => worldBounds;
        public int SourceStartIndex => sourceStartIndex;
        public int SourceCount => sourceCount;

        public StaticMapPresentationChunkEntry(
            string chunkId,
            string scenePath,
            Bounds worldBounds,
            int sourceStartIndex,
            int sourceCount)
        {
            this.chunkId = chunkId;
            this.scenePath = scenePath;
            this.worldBounds = worldBounds;
            this.sourceStartIndex = sourceStartIndex;
            this.sourceCount = sourceCount;
        }
    }

    public sealed class StaticMapPresentationManifest : ScriptableObject
    {
        public const int MinimumReadableSchemaVersion = 1;
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string canonicalScenePath;
        [SerializeField] private string canonicalSceneDependencyHash;
        [SerializeField] private float chunkSize;
        [SerializeField] private string contentHash;
        [SerializeField] private List<StaticMapPresentationChunkEntry> chunks = new();
        [SerializeField] private List<StaticMapPresentationSourceEntry> sources = new();

        public int SchemaVersion => schemaVersion;
        public string CanonicalScenePath => canonicalScenePath;
        public string CanonicalSceneDependencyHash => canonicalSceneDependencyHash;
        public float ChunkSize => chunkSize;
        public string ContentHash => contentHash;
        public IReadOnlyList<StaticMapPresentationChunkEntry> Chunks => chunks;
        public IReadOnlyList<StaticMapPresentationSourceEntry> Sources => sources;

        public static bool IsSchemaReadable(int value) =>
            value >= MinimumReadableSchemaVersion && value <= CurrentSchemaVersion;

#if UNITY_EDITOR
        public void EditorSetData(
            string sourceScenePath,
            string sourceSceneDependencyHash,
            float generatedChunkSize,
            string generatedContentHash,
            List<StaticMapPresentationChunkEntry> generatedChunks,
            List<StaticMapPresentationSourceEntry> generatedSources)
        {
            schemaVersion = CurrentSchemaVersion;
            canonicalScenePath = sourceScenePath;
            canonicalSceneDependencyHash = sourceSceneDependencyHash;
            chunkSize = generatedChunkSize;
            contentHash = generatedContentHash;
            chunks = generatedChunks ?? new List<StaticMapPresentationChunkEntry>();
            sources = generatedSources ?? new List<StaticMapPresentationSourceEntry>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
