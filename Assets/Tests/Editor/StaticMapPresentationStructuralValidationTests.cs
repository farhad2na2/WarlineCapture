using System;
using System.Collections.Generic;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[TestFixture]
public sealed class StaticMapPresentationStructuralValidationTests
{
    private const float TransformTolerance = 0.0005f;
    private const float BoundsTolerance = 0.005f;
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset";

    [Test]
    public void Manifest_DefinesExactSourceChunkBijection()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        ValidationFailures failures = new();

        ValidateManifestBijection(manifest, failures);

        failures.AssertNone("Static map presentation manifest bijection failed.");
    }

    [Test]
    [Timeout(1800000)]
    public void GeneratedChunks_PreserveCanonicalRendererStateAndPresentationOnlyContract()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        ValidationFailures failures = new();
        ValidateManifestBijection(manifest, failures);
        failures.AssertNone("Static map presentation manifest must be valid before scene parity can run.");

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene canonicalScene = EditorSceneManager.OpenScene(manifest.CanonicalScenePath, OpenSceneMode.Single);
            failures.Require(canonicalScene.IsValid() && canonicalScene.isLoaded,
                $"Canonical scene did not load: {manifest.CanonicalScenePath}");

            Dictionary<string, MeshRenderer> canonicalRenderers =
                ResolveCanonicalRenderers(manifest, canonicalScene, failures);

            for (int chunkIndex = 0; chunkIndex < manifest.Chunks.Count; chunkIndex++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[chunkIndex];
                Scene chunkScene = default;
                try
                {
                    chunkScene = EditorSceneManager.OpenScene(chunk.ScenePath, OpenSceneMode.Additive);
                    ValidateChunkScene(manifest, chunk, chunkScene, canonicalRenderers, failures);
                }
                catch (Exception exception)
                {
                    failures.Add($"{chunk.ChunkId}: failed to validate scene {chunk.ScenePath}: {exception}");
                }
                finally
                {
                    if (chunkScene.IsValid() && chunkScene.isLoaded)
                        EditorSceneManager.CloseScene(chunkScene, true);
                }
            }
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        failures.AssertNone("Static map presentation structural parity failed.");
    }

    private static StaticMapPresentationManifest LoadManifest()
    {
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
        Assert.NotNull(manifest, $"Missing static map presentation manifest at {ManifestPath}.");
        return manifest;
    }

    private static void ValidateManifestBijection(
        StaticMapPresentationManifest manifest,
        ValidationFailures failures)
    {
        failures.Require(manifest.SchemaVersion == StaticMapPresentationManifest.CurrentSchemaVersion,
            $"Manifest schema is {manifest.SchemaVersion}; expected {StaticMapPresentationManifest.CurrentSchemaVersion}.");
        failures.Require(!string.IsNullOrWhiteSpace(manifest.CanonicalScenePath),
            "Manifest canonical scene path is empty.");
        failures.Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(manifest.CanonicalScenePath) != null,
            $"Manifest canonical scene does not exist: {manifest.CanonicalScenePath}");
        failures.Require(manifest.ChunkSize > 0f, "Manifest chunk size must be positive.");
        failures.Require(manifest.Chunks.Count > 0, "Manifest contains no chunks.");
        failures.Require(manifest.Sources.Count > 0, "Manifest contains no sources.");

        HashSet<string> chunkIds = new(StringComparer.Ordinal);
        HashSet<string> scenePaths = new(StringComparer.Ordinal);
        HashSet<string> sourceIds = new(StringComparer.Ordinal);
        int expectedStartIndex = 0;

        for (int chunkIndex = 0; chunkIndex < manifest.Chunks.Count; chunkIndex++)
        {
            StaticMapPresentationChunkEntry chunk = manifest.Chunks[chunkIndex];
            string label = $"chunk[{chunkIndex}] {chunk.ChunkId}";
            failures.Require(!string.IsNullOrWhiteSpace(chunk.ChunkId), $"{label}: chunk ID is empty.");
            failures.Require(chunkIds.Add(chunk.ChunkId), $"{label}: duplicate chunk ID.");
            failures.Require(!string.IsNullOrWhiteSpace(chunk.ScenePath), $"{label}: scene path is empty.");
            failures.Require(scenePaths.Add(chunk.ScenePath), $"{label}: duplicate scene path {chunk.ScenePath}.");
            failures.Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(chunk.ScenePath) != null,
                $"{label}: scene asset does not exist at {chunk.ScenePath}.");
            failures.Require(chunk.SourceStartIndex == expectedStartIndex,
                $"{label}: sourceStartIndex is {chunk.SourceStartIndex}; expected contiguous index {expectedStartIndex}.");
            failures.Require(chunk.SourceCount > 0, $"{label}: sourceCount must be positive.");

            if (!TryGetSourceRange(manifest, chunk, failures, out int start, out int end))
                continue;

            HashSet<string> generatedNames = new(StringComparer.Ordinal);
            Bounds expectedBounds = manifest.Sources[start].WorldBounds;
            for (int sourceIndex = start; sourceIndex < end; sourceIndex++)
            {
                StaticMapPresentationSourceEntry source = manifest.Sources[sourceIndex];
                string sourceLabel = $"source[{sourceIndex}] {source.SourceGlobalObjectId}";
                failures.Require(string.Equals(source.ChunkId, chunk.ChunkId, StringComparison.Ordinal),
                    $"{sourceLabel}: references {source.ChunkId}, outside owning range {chunk.ChunkId}.");
                failures.Require(!string.IsNullOrWhiteSpace(source.SourceGlobalObjectId),
                    $"{sourceLabel}: global object ID is empty.");
                failures.Require(sourceIds.Add(source.SourceGlobalObjectId),
                    $"{sourceLabel}: duplicate source global object ID.");
                failures.Require(!string.IsNullOrWhiteSpace(source.GeneratedObjectName),
                    $"{sourceLabel}: generated object name is empty.");
                failures.Require(generatedNames.Add(source.GeneratedObjectName),
                    $"{sourceLabel}: duplicate generated object name in {chunk.ChunkId}: {source.GeneratedObjectName}.");
                failures.Require(source.Mesh != null, $"{sourceLabel}: manifest mesh reference is missing.");
                failures.Require(!string.IsNullOrWhiteSpace(source.MeshAssetGuid),
                    $"{sourceLabel}: manifest mesh GUID is empty.");
                failures.Require(source.Materials.Count > 0, $"{sourceLabel}: manifest has no materials.");

                if (sourceIndex > start)
                    expectedBounds.Encapsulate(source.WorldBounds);
            }

            RequireExactBounds(expectedBounds, chunk.WorldBounds, $"{label}: manifest chunk bounds", failures);
            expectedStartIndex = end;
        }

        failures.Require(expectedStartIndex == manifest.Sources.Count,
            $"Chunk ranges cover {expectedStartIndex} sources; manifest contains {manifest.Sources.Count}.");
        failures.Require(sourceIds.Count == manifest.Sources.Count,
            $"Manifest has {sourceIds.Count} unique source IDs for {manifest.Sources.Count} entries.");
    }

    private static Dictionary<string, MeshRenderer> ResolveCanonicalRenderers(
        StaticMapPresentationManifest manifest,
        Scene canonicalScene,
        ValidationFailures failures)
    {
        Dictionary<string, MeshRenderer> renderers = new(manifest.Sources.Count, StringComparer.Ordinal);
        for (int sourceIndex = 0; sourceIndex < manifest.Sources.Count; sourceIndex++)
        {
            StaticMapPresentationSourceEntry source = manifest.Sources[sourceIndex];
            string label = $"source[{sourceIndex}] {source.SourceGlobalObjectId}";
            if (!GlobalObjectId.TryParse(source.SourceGlobalObjectId, out GlobalObjectId globalObjectId))
            {
                failures.Add($"{label}: global object ID cannot be parsed.");
                continue;
            }

            Object sourceObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            if (sourceObject is not MeshRenderer renderer)
            {
                failures.Add($"{label}: did not resolve to a MeshRenderer.");
                continue;
            }

            failures.Require(renderer.gameObject.scene == canonicalScene,
                $"{label}: resolved outside canonical scene to {renderer.gameObject.scene.path}.");
            renderers[source.SourceGlobalObjectId] = renderer;
        }

        failures.Require(renderers.Count == manifest.Sources.Count,
            $"Resolved {renderers.Count} canonical renderers for {manifest.Sources.Count} manifest sources.");
        return renderers;
    }

    private static void ValidateChunkScene(
        StaticMapPresentationManifest manifest,
        StaticMapPresentationChunkEntry chunk,
        Scene chunkScene,
        IReadOnlyDictionary<string, MeshRenderer> canonicalRenderers,
        ValidationFailures failures)
    {
        string label = $"{chunk.ChunkId} ({chunk.ScenePath})";
        failures.Require(chunkScene.IsValid() && chunkScene.isLoaded, $"{label}: scene is not loaded.");
        if (!chunkScene.IsValid() || !chunkScene.isLoaded)
            return;

        GameObject[] roots = chunkScene.GetRootGameObjects();
        failures.Require(roots.Length == 1, $"{label}: expected one root, found {roots.Length}.");
        if (roots.Length != 1)
            return;

        GameObject root = roots[0];
        failures.Require(string.Equals(root.name, $"StaticMapPresentation_{chunk.ChunkId}", StringComparison.Ordinal),
            $"{label}: unexpected root name {root.name}.");
        failures.Require(root.activeSelf, $"{label}: root must be active.");
        failures.Require(root.transform.parent == null, $"{label}: root must not have a parent.");
        RequireExactVector3(Vector3.zero, root.transform.localPosition, $"{label}: root position", failures);
        RequireExactQuaternion(Quaternion.identity, root.transform.localRotation, $"{label}: root rotation", failures);
        RequireExactVector3(Vector3.one, root.transform.localScale, $"{label}: root scale", failures);
        ValidateAllowedComponents(root, true, label, failures);

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        failures.Require(colliders.Length == 0,
            $"{label}: presentation chunks must contain zero colliders; found {colliders.Length}.");
        failures.Require(behaviours.Length == 0,
            $"{label}: presentation chunks must contain zero gameplay scripts/MonoBehaviours; found {behaviours.Length}.");

        if (!TryGetSourceRange(manifest, chunk, failures, out int start, out int end))
            return;

        Dictionary<string, GameObject> generatedObjects = new(StringComparer.Ordinal);
        for (int childIndex = 0; childIndex < root.transform.childCount; childIndex++)
        {
            GameObject child = root.transform.GetChild(childIndex).gameObject;
            failures.Require(generatedObjects.TryAdd(child.name, child),
                $"{label}: duplicate generated child name {child.name}.");
            failures.Require(child.transform.childCount == 0,
                $"{label}/{child.name}: generated renderer objects must be direct, leaf children.");
            failures.Require(child.activeSelf, $"{label}/{child.name}: generated renderer must be active.");
            failures.Require(!PrefabUtility.IsPartOfAnyPrefab(child),
                $"{label}/{child.name}: generated renderer must not be a prefab instance.");
            ValidateAllowedComponents(child, false, $"{label}/{child.name}", failures);
        }

        failures.Require(root.transform.childCount == chunk.SourceCount,
            $"{label}: root has {root.transform.childCount} children; manifest expects {chunk.SourceCount}.");
        failures.Require(root.GetComponentsInChildren<Transform>(true).Length == chunk.SourceCount + 1,
            $"{label}: hierarchy contains nested or extra transforms.");

        bool hasSourceBounds = false;
        bool hasGeneratedBounds = false;
        Bounds sourceChunkBounds = default;
        Bounds generatedChunkBounds = default;
        for (int sourceIndex = start; sourceIndex < end; sourceIndex++)
        {
            StaticMapPresentationSourceEntry entry = manifest.Sources[sourceIndex];
            string sourceLabel = $"{label}/{entry.GeneratedObjectName} <- {entry.SourceGlobalObjectId}";
            if (!generatedObjects.TryGetValue(entry.GeneratedObjectName, out GameObject generatedObject))
            {
                failures.Add($"{sourceLabel}: generated object is missing.");
                continue;
            }

            if (!canonicalRenderers.TryGetValue(entry.SourceGlobalObjectId, out MeshRenderer sourceRenderer))
            {
                failures.Add($"{sourceLabel}: canonical renderer was not resolved.");
                continue;
            }

            MeshRenderer generatedRenderer = generatedObject.GetComponent<MeshRenderer>();
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            MeshFilter generatedFilter = generatedObject.GetComponent<MeshFilter>();
            if (generatedRenderer == null || sourceFilter == null || generatedFilter == null)
            {
                failures.Add($"{sourceLabel}: source/generated MeshFilter or MeshRenderer is missing.");
                continue;
            }

            ValidateRendererParity(entry, sourceRenderer, sourceFilter, generatedRenderer, generatedFilter, failures);
            AddBounds(ref sourceChunkBounds, ref hasSourceBounds, sourceRenderer.bounds);
            AddBounds(ref generatedChunkBounds, ref hasGeneratedBounds, generatedRenderer.bounds);
        }

        failures.Require(generatedObjects.Count == chunk.SourceCount,
            $"{label}: found {generatedObjects.Count} uniquely named generated objects; expected {chunk.SourceCount}.");
        if (hasSourceBounds)
            RequireExactBounds(chunk.WorldBounds, sourceChunkBounds, $"{label}: canonical source union bounds", failures);
        if (hasGeneratedBounds)
            RequireExactBounds(chunk.WorldBounds, generatedChunkBounds, $"{label}: generated union bounds", failures);
    }

    private static void ValidateAllowedComponents(
        GameObject gameObject,
        bool root,
        string label,
        ValidationFailures failures)
    {
        Component[] components = gameObject.GetComponents<Component>();
        int expectedCount = root ? 1 : 3;
        failures.Require(components.Length == expectedCount,
            $"{label}: expected {expectedCount} components, found {components.Length}.");

        for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
        {
            Component component = components[componentIndex];
            bool allowed = component is Transform ||
                           (!root && (component is MeshFilter || component is MeshRenderer));
            failures.Require(allowed,
                $"{label}: contains forbidden component {(component != null ? component.GetType().FullName : "<missing script>")}.");
        }

        failures.Require(gameObject.GetComponents<Transform>().Length == 1,
            $"{label}: must contain exactly one Transform.");
        if (!root)
        {
            failures.Require(gameObject.GetComponents<MeshFilter>().Length == 1,
                $"{label}: must contain exactly one MeshFilter.");
            failures.Require(gameObject.GetComponents<MeshRenderer>().Length == 1,
                $"{label}: must contain exactly one MeshRenderer.");
        }
    }

    private static void ValidateRendererParity(
        StaticMapPresentationSourceEntry entry,
        MeshRenderer sourceRenderer,
        MeshFilter sourceFilter,
        MeshRenderer generatedRenderer,
        MeshFilter generatedFilter,
        ValidationFailures failures)
    {
        string label = $"{entry.ChunkId}/{entry.GeneratedObjectName}";
        Transform sourceTransform = sourceRenderer.transform;
        Transform generatedTransform = generatedRenderer.transform;

        RequireExactVector3(sourceTransform.position, generatedTransform.position,
            $"{label}: world position", failures);
        RequireExactQuaternion(sourceTransform.rotation, generatedTransform.rotation,
            $"{label}: world rotation", failures);
        RequireExactVector3(sourceTransform.lossyScale, generatedTransform.lossyScale,
            $"{label}: world scale", failures);
        RequireExactVector3(sourceTransform.position, generatedTransform.localPosition,
            $"{label}: flattened local position", failures);
        RequireExactQuaternion(sourceTransform.rotation, generatedTransform.localRotation,
            $"{label}: flattened local rotation", failures);
        RequireExactVector3(sourceTransform.lossyScale, generatedTransform.localScale,
            $"{label}: flattened local scale", failures);

        failures.Require(sourceRenderer.gameObject.layer == generatedRenderer.gameObject.layer,
            $"{label}: layer changed from {sourceRenderer.gameObject.layer} to {generatedRenderer.gameObject.layer}.");
        failures.Require(sourceRenderer.enabled == generatedRenderer.enabled,
            $"{label}: renderer enabled state changed.");
        failures.Require(sourceRenderer.forceRenderingOff == generatedRenderer.forceRenderingOff,
            $"{label}: forceRenderingOff changed.");

        failures.Require(sourceFilter.sharedMesh == generatedFilter.sharedMesh,
            $"{label}: generated MeshFilter does not share the canonical mesh object.");
        failures.Require(entry.Mesh == sourceFilter.sharedMesh && entry.Mesh == generatedFilter.sharedMesh,
            $"{label}: manifest/source/generated mesh references are not identical.");
        RequireAssetIdentity(entry.MeshAssetGuid, entry.MeshLocalId, sourceFilter.sharedMesh,
            $"{label}: canonical mesh", failures);
        RequireAssetIdentity(entry.MeshAssetGuid, entry.MeshLocalId, generatedFilter.sharedMesh,
            $"{label}: generated mesh", failures);

        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        Material[] generatedMaterials = generatedRenderer.sharedMaterials;
        failures.Require(sourceMaterials.Length == generatedMaterials.Length,
            $"{label}: material slot count changed from {sourceMaterials.Length} to {generatedMaterials.Length}.");
        failures.Require(entry.Materials.Count == sourceMaterials.Length,
            $"{label}: manifest has {entry.Materials.Count} material slots; canonical renderer has {sourceMaterials.Length}.");
        int materialCount = Math.Min(entry.Materials.Count, Math.Min(sourceMaterials.Length, generatedMaterials.Length));
        for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
        {
            StaticMapPresentationMaterialEntry materialEntry = entry.Materials[materialIndex];
            failures.Require(sourceMaterials[materialIndex] == generatedMaterials[materialIndex],
                $"{label}: material slot {materialIndex} does not share the canonical material object.");
            failures.Require(materialEntry.Material == sourceMaterials[materialIndex] &&
                             materialEntry.Material == generatedMaterials[materialIndex],
                $"{label}: manifest/source/generated material slot {materialIndex} references are not identical.");
            RequireAssetIdentity(materialEntry.AssetGuid, materialEntry.LocalId, sourceMaterials[materialIndex],
                $"{label}: canonical material[{materialIndex}]", failures);
            RequireAssetIdentity(materialEntry.AssetGuid, materialEntry.LocalId, generatedMaterials[materialIndex],
                $"{label}: generated material[{materialIndex}]", failures);
        }

        failures.Require(sourceRenderer.lightmapIndex == generatedRenderer.lightmapIndex,
            $"{label}: lightmapIndex changed from {sourceRenderer.lightmapIndex} to {generatedRenderer.lightmapIndex}.");
        RequireExactVector4(sourceRenderer.lightmapScaleOffset, generatedRenderer.lightmapScaleOffset,
            $"{label}: lightmapScaleOffset", failures);
        failures.Require(sourceRenderer.realtimeLightmapIndex == generatedRenderer.realtimeLightmapIndex,
            $"{label}: realtimeLightmapIndex changed from {sourceRenderer.realtimeLightmapIndex} to {generatedRenderer.realtimeLightmapIndex}.");
        RequireExactVector4(sourceRenderer.realtimeLightmapScaleOffset, generatedRenderer.realtimeLightmapScaleOffset,
            $"{label}: realtimeLightmapScaleOffset", failures);

        failures.Require(sourceRenderer.shadowCastingMode == generatedRenderer.shadowCastingMode,
            $"{label}: shadowCastingMode changed.");
        failures.Require(sourceRenderer.receiveShadows == generatedRenderer.receiveShadows,
            $"{label}: receiveShadows changed.");
        failures.Require(sourceRenderer.lightProbeUsage == generatedRenderer.lightProbeUsage,
            $"{label}: lightProbeUsage changed.");
        failures.Require(sourceRenderer.reflectionProbeUsage == generatedRenderer.reflectionProbeUsage,
            $"{label}: reflectionProbeUsage changed.");
        failures.Require(sourceRenderer.probeAnchor == generatedRenderer.probeAnchor,
            $"{label}: probeAnchor changed.");
        failures.Require(sourceRenderer.lightProbeProxyVolumeOverride == generatedRenderer.lightProbeProxyVolumeOverride,
            $"{label}: light probe proxy volume override changed.");

        failures.Require(sourceRenderer.motionVectorGenerationMode == generatedRenderer.motionVectorGenerationMode,
            $"{label}: motionVectorGenerationMode changed.");
        failures.Require(sourceRenderer.renderingLayerMask == generatedRenderer.renderingLayerMask,
            $"{label}: renderingLayerMask changed.");
        failures.Require(sourceRenderer.rendererPriority == generatedRenderer.rendererPriority,
            $"{label}: rendererPriority changed.");
        failures.Require(sourceRenderer.sortingLayerID == generatedRenderer.sortingLayerID,
            $"{label}: sortingLayerID changed.");
        failures.Require(sourceRenderer.sortingOrder == generatedRenderer.sortingOrder,
            $"{label}: sortingOrder changed.");
        failures.Require(sourceRenderer.allowOcclusionWhenDynamic == generatedRenderer.allowOcclusionWhenDynamic,
            $"{label}: allowOcclusionWhenDynamic changed.");

        RequireExactBounds(entry.WorldBounds, sourceRenderer.bounds, $"{label}: manifest/canonical bounds", failures);
        RequireExactBounds(entry.WorldBounds, generatedRenderer.bounds, $"{label}: manifest/generated bounds", failures);
        RequireExactBounds(sourceRenderer.bounds, generatedRenderer.bounds, $"{label}: canonical/generated bounds", failures);
    }

    private static bool TryGetSourceRange(
        StaticMapPresentationManifest manifest,
        StaticMapPresentationChunkEntry chunk,
        ValidationFailures failures,
        out int start,
        out int end)
    {
        start = chunk.SourceStartIndex;
        long endLong = (long)chunk.SourceStartIndex + chunk.SourceCount;
        end = endLong >= int.MinValue && endLong <= int.MaxValue ? (int)endLong : -1;
        bool valid = start >= 0 && chunk.SourceCount > 0 && end >= start && end <= manifest.Sources.Count;
        failures.Require(valid,
            $"{chunk.ChunkId}: invalid source range [{chunk.SourceStartIndex}, {endLong}) for {manifest.Sources.Count} sources.");
        return valid;
    }

    private static void RequireAssetIdentity(
        string expectedGuid,
        long expectedLocalId,
        Object asset,
        string label,
        ValidationFailures failures)
    {
        string actualGuid = string.Empty;
        long actualLocalId = 0;
        bool resolved = asset != null &&
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out actualGuid, out actualLocalId);
        failures.Require(resolved, $"{label}: asset identity could not be resolved.");
        if (!resolved)
            return;

        failures.Require(string.Equals(expectedGuid, actualGuid, StringComparison.Ordinal) &&
                         expectedLocalId == actualLocalId,
            $"{label}: expected {expectedGuid}:{expectedLocalId}, found {actualGuid}:{actualLocalId}.");
    }

    private static void AddBounds(ref Bounds aggregate, ref bool initialized, Bounds value)
    {
        if (!initialized)
        {
            aggregate = value;
            initialized = true;
            return;
        }

        aggregate.Encapsulate(value);
    }

    private static void RequireExactBounds(
        Bounds expected,
        Bounds actual,
        string label,
        ValidationFailures failures)
    {
        if (!WithinTolerance(expected.center, actual.center, BoundsTolerance) ||
            !WithinTolerance(expected.size, actual.size, BoundsTolerance))
        {
            failures.Add(
                $"{label} changed beyond {BoundsTolerance:R}: " +
                $"expected center={expected.center:R} size={expected.size:R}, " +
                $"found center={actual.center:R} size={actual.size:R}.");
        }
    }

    private static bool WithinTolerance(Vector3 expected, Vector3 actual, float tolerance)
    {
        Vector3 delta = expected - actual;
        return Mathf.Abs(delta.x) <= tolerance &&
               Mathf.Abs(delta.y) <= tolerance &&
               Mathf.Abs(delta.z) <= tolerance;
    }

    private static void RequireExactVector3(
        Vector3 expected,
        Vector3 actual,
        string label,
        ValidationFailures failures)
    {
        if (!WithinTolerance(expected, actual, TransformTolerance))
        {
            failures.Add(
                $"{label} changed beyond {TransformTolerance:R}: " +
                $"expected {expected:R}, found {actual:R}.");
        }
    }

    private static void RequireExactVector4(
        Vector4 expected,
        Vector4 actual,
        string label,
        ValidationFailures failures)
    {
        Vector4 delta = expected - actual;
        if (Mathf.Abs(delta.x) > TransformTolerance ||
            Mathf.Abs(delta.y) > TransformTolerance ||
            Mathf.Abs(delta.z) > TransformTolerance ||
            Mathf.Abs(delta.w) > TransformTolerance)
        {
            failures.Add(
                $"{label} changed beyond {TransformTolerance:R}: " +
                $"expected {expected:R}, found {actual:R}.");
        }
    }

    private static void RequireExactQuaternion(
        Quaternion expected,
        Quaternion actual,
        string label,
        ValidationFailures failures)
    {
        Vector4 expectedVector = new(expected.x, expected.y, expected.z, expected.w);
        Vector4 actualVector = new(actual.x, actual.y, actual.z, actual.w);
        Vector4 negatedActual = -actualVector;
        Vector4 directDelta = expectedVector - actualVector;
        Vector4 negatedDelta = expectedVector - negatedActual;
        bool directMatch = Mathf.Abs(directDelta.x) <= TransformTolerance &&
                           Mathf.Abs(directDelta.y) <= TransformTolerance &&
                           Mathf.Abs(directDelta.z) <= TransformTolerance &&
                           Mathf.Abs(directDelta.w) <= TransformTolerance;
        bool negatedMatch = Mathf.Abs(negatedDelta.x) <= TransformTolerance &&
                            Mathf.Abs(negatedDelta.y) <= TransformTolerance &&
                            Mathf.Abs(negatedDelta.z) <= TransformTolerance &&
                            Mathf.Abs(negatedDelta.w) <= TransformTolerance;
        if (!directMatch && !negatedMatch)
        {
            failures.Add(
                $"{label} changed beyond {TransformTolerance:R}: " +
                $"expected {expected:R}, found {actual:R}.");
        }
    }

    private sealed class ValidationFailures
    {
        private const int ReportLimit = 200;
        private readonly List<string> messages = new();

        public int Count { get; private set; }

        public void Require(bool condition, string message)
        {
            if (!condition)
                Add(message);
        }

        public void Add(string message)
        {
            Count++;
            if (messages.Count < ReportLimit)
                messages.Add(message);
        }

        public void AssertNone(string heading)
        {
            if (Count == 0)
                return;

            string omitted = Count > messages.Count
                ? $"\n... {Count - messages.Count} additional failures omitted."
                : string.Empty;
            Assert.Fail($"{heading} Failure count: {Count}.\n{string.Join("\n", messages)}{omitted}");
        }
    }
}
