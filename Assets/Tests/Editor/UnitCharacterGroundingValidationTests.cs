using Game.Configs;
using Game.Authoring;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class UnitCharacterGroundingValidationTests
{
    private const float FootAlignmentTolerance = 0.035f;
    private const float GpuAnimationFootAlignmentTolerance = 0.005f;
    private const float GpuGroundingVertexMaxStaticY = 0.35f;
    private const string CharacterPrefabFolder = "Assets/Game/Prefabs/Characters";
    private const string CharacterConfigFolder = "Assets/Game/Configs/Prefabs";
    private const string SnivelerMainTextureFirst = "_SnivelerMainTextureFirst";
    private const string SnivelerMainTextureSecond = "_SnivelerMainTextureSecond";
    private const string SnivelerMainTextureThird = "_SnivelerMainTextureThird";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitCharacterGroundingValidationTests();
            tests.CharacterModelFeetAlignWithUnitRoot();
            tests.GpuAnimatedCharacterFramesDoNotSinkBelowUnitRoot();
            tests.CharacterLodFeetAlignWithUnitRootAfterRuntimeModelTransform();
            tests.CharacterImpostorAtlasDefaultAnchorDoesNotSinkBelowRoot();
            Debug.Log("[UnitCharacterGroundingValidation] result=Passed tests=4");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[UnitCharacterGroundingValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void GpuAnimatedCharacterFramesDoNotSinkBelowUnitRoot()
    {
        List<string> failures = new();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab Unit_Chr_", new[] { CharacterPrefabFolder });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!Path.GetFileNameWithoutExtension(path).StartsWith("Unit_Chr_Soldier_", System.StringComparison.Ordinal))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            try
            {
                if (!root.TryGetComponent(out UnitGridAuthoring authoring))
                    continue;

                Transform model = ResolveModelRoot(root, authoring);
                if (model == null)
                    continue;

                MaterialAnimatorIndexAuthoring index = model.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
                if (index == null || index.animator == null)
                    continue;

                MaterialAnimatorAuthoring animator = index.animator.GetComponent<MaterialAnimatorAuthoring>();
                if (animator == null || animator.animations == null || animator.animations.Count == 0)
                    continue;

                if (!TryGetGpuAnimatedMinY(root.transform, model, animator, out float minY, out string sample))
                {
                    failures.Add($"{path}: unable to sample GPU animated mesh");
                    continue;
                }

                float groundOffset = ResolveGroundOffset(authoring);
                float groundedMinY = minY + groundOffset;
                if (groundedMinY < -GpuAnimationFootAlignmentTolerance)
                    failures.Add($"{path}: gpuAnimatedMinY={minY:F4} groundOffset={groundOffset:F4} groundedMinY={groundedMinY:F4} sample={sample}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void CharacterModelFeetAlignWithUnitRoot()
    {
        List<string> failures = new();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab Unit_Chr_", new[] { CharacterPrefabFolder });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            try
            {
                if (!root.TryGetComponent(out UnitGridAuthoring authoring))
                    continue;

                Transform model = ResolveModelRoot(root, authoring);
                if (model == null || !TryGetLocalBounds(root.transform, model, out Bounds bounds))
                {
                    failures.Add($"{path}: missing model bounds");
                    continue;
                }

                if (Mathf.Abs(bounds.min.y) > FootAlignmentTolerance)
                    failures.Add($"{path}: footMinY={bounds.min.y:F4} modelLocalY={model.localPosition.y:F4}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void CharacterLodFeetAlignWithUnitRootAfterRuntimeModelTransform()
    {
        List<string> failures = new();
        string[] configGuids = AssetDatabase.FindAssets("t:UnitGridAuthoringConfig Prefab_UnitGrid_Chr_", new[] { CharacterConfigFolder });
        for (int i = 0; i < configGuids.Length; i++)
        {
            string configPath = AssetDatabase.GUIDToAssetPath(configGuids[i]);
            UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(configPath);
            if (config == null)
                continue;

            string prefabPath = ResolveCharacterPrefabPath(configPath);
            GameObject sourceRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (sourceRoot == null)
                continue;

            try
            {
                if (!sourceRoot.TryGetComponent(out UnitGridAuthoring authoring))
                    continue;

                Transform sourceModel = ResolveModelRoot(sourceRoot, authoring);
                if (sourceModel == null)
                    continue;

                Matrix4x4 runtimeModelTransform = Matrix4x4.TRS(
                    sourceModel.localPosition,
                    sourceModel.localRotation,
                    sourceModel.localScale);
                ValidateRuntimeLodBounds(config.MidLodPrefab, runtimeModelTransform, configPath, "mid", failures);
                ValidateRuntimeLodBounds(config.LowLodPrefab, runtimeModelTransform, configPath, "low", failures);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void CharacterImpostorAtlasDefaultAnchorDoesNotSinkBelowRoot()
    {
        var entry = new UnitImpostorAtlasEntry();
        Assert.AreEqual(0f, entry.GroundAnchorNormalized, 0.0001f);
    }

    private static void ValidateRuntimeLodBounds(
        GameObject lodPrefab,
        Matrix4x4 runtimeModelTransform,
        string configPath,
        string label,
        List<string> failures)
    {
        if (lodPrefab == null)
            return;

        string lodPath = AssetDatabase.GetAssetPath(lodPrefab);
        GameObject lodRoot = PrefabUtility.LoadPrefabContents(lodPath);
        if (lodRoot == null)
            return;

        try
        {
            if (!TryGetLocalBounds(lodRoot.transform, lodRoot.transform, out Bounds lodBounds))
            {
                failures.Add($"{configPath}: {label} lod missing bounds path={lodPath}");
                return;
            }

            float minY = TransformBoundsMinY(runtimeModelTransform, lodBounds);
            if (Mathf.Abs(minY) > FootAlignmentTolerance)
                failures.Add($"{configPath}: {label} lod footMinY={minY:F4} path={lodPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lodRoot);
        }
    }

    private static Transform ResolveModelRoot(GameObject root, UnitGridAuthoring authoring)
    {
        SerializedObject serialized = new(authoring);
        SerializedProperty modelRoot = serialized.FindProperty("modelRoot");
        if (modelRoot != null && modelRoot.objectReferenceValue is Transform explicitModelRoot)
            return explicitModelRoot;

        Transform child = root.transform.Find("Model");
        return child != null ? child : root.transform;
    }

    private static string ResolveCharacterPrefabPath(string configPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(configPath);
        const string prefix = "Prefab_UnitGrid_";
        const string suffix = "_Config";
        if (fileName.StartsWith(prefix, System.StringComparison.Ordinal))
            fileName = fileName.Substring(prefix.Length);
        if (fileName.EndsWith(suffix, System.StringComparison.Ordinal))
            fileName = fileName.Substring(0, fileName.Length - suffix.Length);
        if (!fileName.StartsWith("Unit_", System.StringComparison.Ordinal))
            fileName = $"Unit_{fileName}";

        return $"{CharacterPrefabFolder}/{fileName}.prefab";
    }

    private static float ResolveGroundOffset(UnitGridAuthoring authoring)
    {
        if (authoring == null)
            return 0f;

        return authoring.ConfiguredGroundOffset;
    }

    private static bool TryGetGpuAnimatedMinY(
        Transform root,
        Transform model,
        MaterialAnimatorAuthoring animator,
        out float minY,
        out string sample)
    {
        minY = float.PositiveInfinity;
        sample = string.Empty;
        bool hasSample = false;
        int boneCount = Mathf.Max(1, animator.bonesCount);

        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
        {
            MeshFilter filter = filters[filterIndex];
            if (filter == null || filter.sharedMesh == null)
                continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            if (!TryGetSnivelerTextures(renderer.sharedMaterial, out Texture2D row0Texture, out Texture2D row1Texture, out Texture2D row2Texture))
                continue;

            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            List<Vector4> boneIndices = new(vertices.Length);
            List<Vector4> boneWeights = new(vertices.Length);
            mesh.GetUVs(2, boneIndices);
            mesh.GetUVs(3, boneWeights);
            if (boneIndices.Count != vertices.Length || boneWeights.Count != vertices.Length)
                continue;

            Matrix4x4 rendererToRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int animationIndex = 1; animationIndex < animator.animations.Count; animationIndex++)
            {
                MaterialAnimatorBake animation = animator.animations[animationIndex];
                int frameCount = Mathf.Max(1, animation.frames);
                for (int frame = 0; frame < frameCount; frame += ResolveFrameStride(frameCount))
                {
                    SampleGpuAnimatedFrameMinY(
                        vertices,
                        boneIndices,
                        boneWeights,
                        row0Texture,
                        row1Texture,
                        row2Texture,
                        rendererToRoot,
                        animation.start + frame * boneCount,
                        boneCount,
                        filter.name,
                        animationIndex,
                        frame,
                        ref minY,
                        ref sample,
                        ref hasSample);
                }

                int lastFrame = frameCount - 1;
                if (lastFrame > 0)
                {
                    SampleGpuAnimatedFrameMinY(
                        vertices,
                        boneIndices,
                        boneWeights,
                        row0Texture,
                        row1Texture,
                        row2Texture,
                        rendererToRoot,
                        animation.start + lastFrame * boneCount,
                        boneCount,
                        filter.name,
                        animationIndex,
                        lastFrame,
                        ref minY,
                        ref sample,
                        ref hasSample);
                }
            }
        }

        return hasSample;
    }

    private static int ResolveFrameStride(int frameCount)
    {
        return frameCount <= 24 ? 1 : Mathf.Max(1, frameCount / 12);
    }

    private static bool TryGetSnivelerTextures(
        Material material,
        out Texture2D row0Texture,
        out Texture2D row1Texture,
        out Texture2D row2Texture)
    {
        row0Texture = material.GetTexture(SnivelerMainTextureFirst) as Texture2D;
        row1Texture = material.GetTexture(SnivelerMainTextureSecond) as Texture2D;
        row2Texture = material.GetTexture(SnivelerMainTextureThird) as Texture2D;
        return row0Texture != null && row1Texture != null && row2Texture != null;
    }

    private static void SampleGpuAnimatedFrameMinY(
        Vector3[] vertices,
        List<Vector4> boneIndices,
        List<Vector4> boneWeights,
        Texture2D row0Texture,
        Texture2D row1Texture,
        Texture2D row2Texture,
        Matrix4x4 rendererToRoot,
        int frameStart,
        int boneCount,
        string meshName,
        int animationIndex,
        int frame,
        ref float minY,
        ref string sample,
        ref bool hasSample)
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            float staticY = rendererToRoot.MultiplyPoint3x4(vertices[vertexIndex]).y;
            if (staticY > GpuGroundingVertexMaxStaticY)
                continue;

            Vector3 deformed = TransformGpuAnimatedVertex(
                vertices[vertexIndex],
                boneIndices[vertexIndex],
                boneWeights[vertexIndex],
                row0Texture,
                row1Texture,
                row2Texture,
                frameStart,
                boneCount);
            float y = rendererToRoot.MultiplyPoint3x4(deformed).y;
            if (y < minY)
            {
                minY = y;
                sample = $"{meshName} anim={animationIndex} frame={frame} vertex={vertexIndex}";
                hasSample = true;
            }
        }
    }

    private static Vector3 TransformGpuAnimatedVertex(
        Vector3 vertex,
        Vector4 boneIndices,
        Vector4 boneWeights,
        Texture2D row0Texture,
        Texture2D row1Texture,
        Texture2D row2Texture,
        int frameStart,
        int boneCount)
    {
        Vector3 value = Vector3.zero;
        value += TransformGpuAnimatedVertexForBone(vertex, boneIndices.x, boneWeights.x, row0Texture, row1Texture, row2Texture, frameStart, boneCount);
        value += TransformGpuAnimatedVertexForBone(vertex, boneIndices.y, boneWeights.y, row0Texture, row1Texture, row2Texture, frameStart, boneCount);
        value += TransformGpuAnimatedVertexForBone(vertex, boneIndices.z, boneWeights.z, row0Texture, row1Texture, row2Texture, frameStart, boneCount);
        value += TransformGpuAnimatedVertexForBone(vertex, boneIndices.w, boneWeights.w, row0Texture, row1Texture, row2Texture, frameStart, boneCount);
        return value;
    }

    private static Vector3 TransformGpuAnimatedVertexForBone(
        Vector3 vertex,
        float rawBoneIndex,
        float weight,
        Texture2D row0Texture,
        Texture2D row1Texture,
        Texture2D row2Texture,
        int frameStart,
        int boneCount)
    {
        if (weight <= 0f)
            return Vector3.zero;

        int boneIndex = Mathf.Clamp(Mathf.RoundToInt(rawBoneIndex), 0, boneCount - 1);
        int matrixIndex = frameStart + boneIndex;
        Color row0 = ReadAnimationPixel(row0Texture, matrixIndex);
        Color row1 = ReadAnimationPixel(row1Texture, matrixIndex);
        Color row2 = ReadAnimationPixel(row2Texture, matrixIndex);

        float x = row0.r * vertex.x + row0.g * vertex.y + row0.b * vertex.z + row0.a;
        float y = row1.r * vertex.x + row1.g * vertex.y + row1.b * vertex.z + row1.a;
        float z = row2.r * vertex.x + row2.g * vertex.y + row2.b * vertex.z + row2.a;
        return new Vector3(x, y, z) * weight;
    }

    private static Color ReadAnimationPixel(Texture2D texture, int pixelIndex)
    {
        int x = pixelIndex % texture.width;
        int y = pixelIndex / texture.width;
        return texture.GetPixel(x, y);
    }

    private static bool TryGetLocalBounds(Transform root, Transform model, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds localBounds = renderer.localBounds;
            Matrix4x4 rendererToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;
            Encapsulate(rendererToRoot.MultiplyPoint3x4(localBounds.min), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(localBounds.max), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z)), ref bounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static float TransformBoundsMinY(Matrix4x4 matrix, Bounds bounds)
    {
        float minY = float.PositiveInfinity;
        EncapsulateY(matrix.MultiplyPoint3x4(bounds.min), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(bounds.max), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)), ref minY);
        EncapsulateY(matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)), ref minY);
        return minY;
    }

    private static void Encapsulate(Vector3 point, ref Bounds bounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }

    private static void EncapsulateY(Vector3 point, ref float minY)
    {
        if (point.y < minY)
            minY = point.y;
    }
}
#endif
