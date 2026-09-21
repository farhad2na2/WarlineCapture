#if UNITY_EDITOR
namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Game.Components;
    using Game.Configs;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Rebakes the walkable surface heights that the E0.1 geometry correction invalidated, scoped strictly to
    /// the City Crossroads playable window.
    ///
    /// <para><see cref="CityCrossroadsShelfGeometryAudit.ApplyAuthoredCorrection"/> lowered the stranded sand
    /// slabs to grade, but the shared bake still described the old shelf tops, so units would have stood
    /// roughly six metres above the ground they now appear to walk on.</para>
    ///
    /// <para><c>Match_Map_MapSurfaceData.asset</c> is shared with the campaign maps, so this pass never
    /// rebuilds the whole blob from scratch. It copies the existing compact payload verbatim, rewrites the
    /// packed height of in-window cells only, and preserves <c>CompactMinHeight</c>/<c>CompactHeightStep</c>
    /// so untouched cells re-encode to identical bytes. Containment is then verified against the pre-change
    /// snapshot, and the asset is only written when nothing outside the window moved.</para>
    /// </summary>
    public static class CityCrossroadsSurfaceRebake
    {
        public const string SurfaceAssetPath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string EvidenceDirectory = CityCrossroadsShelfGeometryAudit.EvidenceDirectory;

        /// <summary>
        /// Only correct a cell that sits meaningfully above the authored ground. The pass never raises a
        /// cell, so it can only ever remove "unit floating above the ground", never create it.
        /// </summary>
        private const float MinCorrection = 0.25f;

        public static void ApplyScopedSurfaceRebake()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfaceAssetPath);
            if (asset == null)
                throw new InvalidOperationException($"Missing surface asset at {SurfaceAssetPath}.");

            // Sample the corrected scene first. Opening a 230 MB SubScene triggers an unused-asset sweep that
            // destroys the ScriptableObject instance, so the asset is re-loaded afterwards.
            var gridDimensions = new int2(asset.Dimensions.x, asset.Dimensions.y);
            float[] targets = BuildTargetHeights(gridDimensions);

            asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfaceAssetPath);
            if (asset == null)
                throw new InvalidOperationException($"Surface asset unloaded after opening the scene.");

            bool generatedFlatEquivalent = asset.GeneratedFlatEquivalent;
            if (!asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> oldBlob))
                throw new InvalidOperationException("Failed to build the runtime surface blob.");

            MapSurfaceCompactSample[] original;
            int2 dimensions;
            float minHeight;
            float heightStep;
            float cellSize;
            Vector3 gridOrigin;
            try
            {
                ref MapSurfaceBlob blob = ref oldBlob.Value;
                if (!MapSurfaceBlobAccess.IsCompactSingleLayer(ref blob))
                {
                    throw new InvalidOperationException(
                        "Expected a compact single-layer surface payload; refusing to rewrite a layered blob.");
                }

                dimensions = blob.Dimensions;
                minHeight = blob.CompactMinHeight;
                heightStep = blob.CompactHeightStep;
                cellSize = blob.CellSize;
                gridOrigin = new Vector3(blob.GridOrigin.x, blob.GridOrigin.y, blob.GridOrigin.z);
                original = new MapSurfaceCompactSample[blob.CompactSamples.Length];
                for (int i = 0; i < original.Length; i++)
                    original[i] = blob.CompactSamples[i];
            }
            finally
            {
                if (oldBlob.IsCreated)
                    oldBlob.Dispose();
            }

            if (dimensions.x != gridDimensions.x || dimensions.y != gridDimensions.y)
                throw new InvalidOperationException("Surface dimensions changed between passes; refusing to write.");

            var updated = new MapSurfaceCompactSample[original.Length];
            Array.Copy(original, updated, original.Length);

            int minX = Mathf.Max(0, Mathf.FloorToInt(CityCrossroadsShelfGeometryAudit.PlayableMin.x));
            int maxX = Mathf.Min(dimensions.x - 1, Mathf.CeilToInt(CityCrossroadsShelfGeometryAudit.PlayableMax.x));
            int minZ = Mathf.Max(0, Mathf.FloorToInt(CityCrossroadsShelfGeometryAudit.PlayableMin.y));
            int maxZ = Mathf.Min(dimensions.y - 1, Mathf.CeilToInt(CityCrossroadsShelfGeometryAudit.PlayableMax.y));

            int planned = 0;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = x + (z * dimensions.x);
                    float target = targets[index];
                    if (float.IsNaN(target))
                        continue;

                    float existing = minHeight + (original[index].PackedHeight * heightStep);
                    if (existing - target <= MinCorrection)
                        continue;

                    ushort packed = Quantize(target, minHeight, heightStep);
                    if (packed == original[index].PackedHeight)
                        continue;

                    updated[index].PackedHeight = packed;
                    planned++;
                }
            }

            WriteBlob(asset, updated, dimensions, minHeight, heightStep, cellSize, gridOrigin, generatedFlatEquivalent);

            // Containment proof against the real re-encoded payload, not against our in-memory intent.
            if (!asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> verifyBlob))
                throw new InvalidOperationException("Failed to rebuild the surface blob for verification.");

            int changedInside = 0;
            int changedOutside = 0;
            int changedNonHeight = 0;
            int bboxMinX = int.MaxValue, bboxMaxX = int.MinValue, bboxMinZ = int.MaxValue, bboxMaxZ = int.MinValue;
            float largestDrop = 0f;
            var outsideExamples = new List<string>();

            try
            {
                ref MapSurfaceBlob verify = ref verifyBlob.Value;
                if (verify.CompactSamples.Length != original.Length)
                    throw new InvalidOperationException("Surface cell count changed; refusing to save.");

                for (int i = 0; i < original.Length; i++)
                {
                    MapSurfaceCompactSample before = original[i];
                    MapSurfaceCompactSample after = verify.CompactSamples[i];
                    bool heightChanged = before.PackedHeight != after.PackedHeight;
                    bool otherChanged = before.LayerId != after.LayerId ||
                                        before.MovementMask != after.MovementMask ||
                                        before.Flags != after.Flags ||
                                        before.SurfaceType != after.SurfaceType ||
                                        before.NormalX != after.NormalX ||
                                        before.NormalY != after.NormalY ||
                                        before.NormalZ != after.NormalZ;
                    if (!heightChanged && !otherChanged)
                        continue;

                    if (otherChanged)
                        changedNonHeight++;

                    int x = i % dimensions.x;
                    int z = i / dimensions.x;
                    bool inside = x >= minX && x <= maxX && z >= minZ && z <= maxZ;
                    if (!inside)
                    {
                        changedOutside++;
                        if (outsideExamples.Count < 10)
                            outsideExamples.Add($"({x},{z})");
                        continue;
                    }

                    changedInside++;
                    bboxMinX = Mathf.Min(bboxMinX, x);
                    bboxMaxX = Mathf.Max(bboxMaxX, x);
                    bboxMinZ = Mathf.Min(bboxMinZ, z);
                    bboxMaxZ = Mathf.Max(bboxMaxZ, z);
                    float drop = (before.PackedHeight - after.PackedHeight) * heightStep;
                    if (drop > largestDrop)
                        largestDrop = drop;
                }
            }
            finally
            {
                if (verifyBlob.IsCreated)
                    verifyBlob.Dispose();
            }

            string bbox = changedInside == 0
                ? "none"
                : $"x {bboxMinX}..{bboxMaxX}, z {bboxMinZ}..{bboxMaxZ}";

            var report = new StringBuilder();
            report.AppendLine("# City Crossroads scoped surface rebake");
            report.AppendLine();
            report.AppendLine($"- Surface asset: `{SurfaceAssetPath}`");
            report.AppendLine($"- Total cells: {original.Length}");
            report.AppendLine($"- Write window: x {minX}..{maxX}, z {minZ}..{maxZ}");
            report.AppendLine($"- Planned edits: {planned}");
            report.AppendLine($"- Cells changed inside window: {changedInside}");
            report.AppendLine($"- Cells changed outside window: {changedOutside}");
            report.AppendLine($"- Cells with non-height field changes: {changedNonHeight}");
            report.AppendLine($"- Changed-cell bounding box: {bbox}");
            report.AppendLine($"- Largest height drop: {largestDrop:0.000} m");
            report.AppendLine($"- Quantisation preserved: min={minHeight:0.00000} step={heightStep:0.00000}");
            if (outsideExamples.Count > 0)
                report.AppendLine($"- Outside examples: {string.Join(", ", outsideExamples)}");

            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllText(Path.Combine(EvidenceDirectory, "cc-surface-rebake.md"), report.ToString());

            if (changedOutside > 0 || changedNonHeight > 0)
            {
                string failure =
                    $"[CityCrossroadsSurfaceRebake] result=Failed reason=containment changedInside={changedInside} " +
                    $"changedOutside={changedOutside} changedNonHeight={changedNonHeight} bbox={bbox}";
                Debug.LogError(failure);
                throw new InvalidOperationException(failure);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[CityCrossroadsSurfaceRebake] result=Applied changedCells={changedInside} changedOutside=0 " +
                $"bbox=[{bbox}] largestDrop={largestDrop:0.000} totalCells={original.Length} " +
                $"asset={SurfaceAssetPath}");
        }

        /// <summary>
        /// Per-cell walkable top taken from the corrected authored ground panels. Cells with no authored
        /// ground above them stay NaN and are left untouched.
        /// </summary>
        /// <summary>
        /// Re-derives <c>surfaceMetadata</c> on every operation map that binds this surface asset.
        ///
        /// <para>This is not bookkeeping. <c>MapSurfaceRuntimeBootstrapSceneSystemHelper.MatchesActiveMap</c>
        /// compares the asset's <c>ComputeRuntimeBlobHash()</c> against the hash recorded on the active map,
        /// and rejects the bake when they differ: "Serialized map surface data does not match the active
        /// operation-map metadata." The match then silently falls back to a flat-equivalent surface, so the
        /// corrected heights never reach gameplay. Every field here is recomputed from the asset itself
        /// rather than transcribed, matching how OperationMapCurrentCompatibilityDefinitionBuilder builds it.</para>
        /// </summary>
        public static void RefreshSurfaceMetadata()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfaceAssetPath);
            if (asset == null)
                throw new InvalidOperationException($"Missing surface asset at {SurfaceAssetPath}.");

            string surfaceGuid = AssetDatabase.AssetPathToGUID(SurfaceAssetPath);
            string contentHash = ComputeFileHash(SurfaceAssetPath);
            string runtimeBlobHash = asset.ComputeRuntimeBlobHash().ToString();

            if (!asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob))
                throw new InvalidOperationException("Failed to build the runtime surface blob.");

            float minimumHeight;
            float maximumHeight;
            try
            {
                ref MapSurfaceBlob value = ref blob.Value;
                minimumHeight = float.MaxValue;
                maximumHeight = float.MinValue;
                int cells = value.Dimensions.x * value.Dimensions.y;
                for (int i = 0; i < cells; i++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref value, i, out MapSurfaceSample sample))
                        continue;
                    if (sample.Height < minimumHeight)
                        minimumHeight = sample.Height;
                    if (sample.Height > maximumHeight)
                        maximumHeight = sample.Height;
                }
            }
            finally
            {
                if (blob.IsCreated)
                    blob.Dispose();
            }

            var updated = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:OperationMapDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
                if (definition == null)
                    continue;

                var serialized = new SerializedObject(definition);
                SerializedProperty metadata = serialized.FindProperty("surfaceMetadata");
                if (metadata == null)
                    continue;
                SerializedProperty assetGuid = metadata.FindPropertyRelative("assetGuid");
                if (assetGuid == null || !string.Equals(assetGuid.stringValue, surfaceGuid, StringComparison.Ordinal))
                    continue;

                metadata.FindPropertyRelative("contentHash").stringValue = contentHash;
                metadata.FindPropertyRelative("runtimeBlobHash").stringValue = runtimeBlobHash;
                metadata.FindPropertyRelative("surfaceCount").intValue = asset.SurfaceCount;
                metadata.FindPropertyRelative("payloadVersion").intValue = asset.PayloadVersion;
                metadata.FindPropertyRelative("payloadEncoding").intValue = asset.PayloadEncoding;
                metadata.FindPropertyRelative("minimumHeight").floatValue = minimumHeight;
                metadata.FindPropertyRelative("maximumHeight").floatValue = maximumHeight;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);

                if (!definition.TryValidateMetadata(out string error))
                    throw new InvalidOperationException($"{path}: {error}");
                updated.Add(Path.GetFileNameWithoutExtension(path));
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[CityCrossroadsSurfaceMetadata] result=Refreshed maps={updated.Count} " +
                $"runtimeBlobHash={runtimeBlobHash} contentHash={contentHash} " +
                $"minHeight={minimumHeight:0.000} maxHeight={maximumHeight:0.000} " +
                $"updated=[{string.Join(", ", updated)}]");
        }

        private static string ComputeFileHash(string assetPath)
        {
            byte[] bytes = File.ReadAllBytes(Path.GetFullPath(assetPath));
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            byte[] hash = algorithm.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        public static float[] BuildAuthoredGroundTops(int2 dimensions) => BuildTargetHeights(dimensions);

        private static float[] BuildTargetHeights(int2 dimensions)
        {
            Scene scene = CityCrossroadsShelfGeometryAudit.OpenPresentationScene();
            List<CityCrossroadsShelfGeometryAudit.GeometryRecord> records =
                CityCrossroadsShelfGeometryAudit.CollectRecordsInPlayableWindow(scene);
            List<CityCrossroadsShelfGeometryAudit.GeometryRecord> panels =
                CityCrossroadsShelfGeometryAudit.CollectGroundPanels(records);

            var targets = new float[dimensions.x * dimensions.y];
            for (int i = 0; i < targets.Length; i++)
                targets[i] = float.NaN;

            for (int p = 0; p < panels.Count; p++)
            {
                Bounds bounds = panels[p].Bounds;
                int fromX = Mathf.Max(0, Mathf.FloorToInt(bounds.min.x));
                int toX = Mathf.Min(dimensions.x - 1, Mathf.CeilToInt(bounds.max.x));
                int fromZ = Mathf.Max(0, Mathf.FloorToInt(bounds.min.z));
                int toZ = Mathf.Min(dimensions.y - 1, Mathf.CeilToInt(bounds.max.z));
                float top = bounds.max.y;

                for (int z = fromZ; z <= toZ; z++)
                {
                    for (int x = fromX; x <= toX; x++)
                    {
                        int index = x + (z * dimensions.x);
                        if (float.IsNaN(targets[index]) || top > targets[index])
                            targets[index] = top;
                    }
                }
            }

            return targets;
        }

        private static void WriteBlob(
            MapSurfaceDataAsset asset,
            MapSurfaceCompactSample[] samples,
            int2 dimensions,
            float minHeight,
            float heightStep,
            float cellSize,
            Vector3 gridOrigin,
            bool generatedFlatEquivalent)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
            root.GridOrigin = new float3(gridOrigin.x, gridOrigin.y, gridOrigin.z);
            root.CellSize = cellSize;
            root.Dimensions = dimensions;
            root.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
            root.CompactMinHeight = minHeight;
            root.CompactHeightStep = heightStep;
            builder.Allocate(ref root.Cells, 0);
            builder.Allocate(ref root.Samples, 0);
            builder.Allocate(ref root.Connections, 0);
            BlobBuilderArray<MapSurfaceCompactSample> compact =
                builder.Allocate(ref root.CompactSamples, samples.Length);
            for (int i = 0; i < samples.Length; i++)
                compact[i] = samples[i];

            BlobAssetReference<MapSurfaceBlob> blob =
                builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Temp);
            try
            {
                asset.ConfigureBakedSurface(
                    gridOrigin,
                    cellSize,
                    new Vector2Int(dimensions.x, dimensions.y),
                    blob,
                    generatedFlatEquivalent);
            }
            finally
            {
                if (blob.IsCreated)
                    blob.Dispose();
            }
        }

        private static ushort Quantize(float height, float minHeight, float heightStep)
        {
            if (heightStep <= 0f)
                return 0;
            int packed = Mathf.RoundToInt((height - minHeight) / heightStep);
            return (ushort)Mathf.Clamp(packed, 0, ushort.MaxValue);
        }
    }
}
#endif
