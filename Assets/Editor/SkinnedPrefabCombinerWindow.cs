using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SkinnedPrefabCombinerWindow : EditorWindow
{
    [SerializeField] private List<GameObject> _sourcePrefabs = new();
    [SerializeField] private string _outputFolder = "Assets/Game/Prefabs/Generated";
    [SerializeField] private string _outputPrefabName = "";
    [SerializeField] private string _lastSourcePrefabPath = "";

    [MenuItem("Tools/DOTS/Skinned Prefab Combiner")]
    public static void Open()
    {
        GetWindow<SkinnedPrefabCombinerWindow>("Skinned Prefab Combiner");
    }

    private void OnGUI()
    {
        var serializedObject = new SerializedObject(this);
        serializedObject.Update();

        EditorGUILayout.Space();
        DrawSourcePrefabList(serializedObject.FindProperty(nameof(_sourcePrefabs)));

        serializedObject.ApplyModifiedProperties();

        SyncOutputDefaultsFromSource();

        _outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder"), _outputFolder);
        _outputPrefabName = EditorGUILayout.TextField(new GUIContent("Output Prefab Name"), _outputPrefabName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Creates a new prefab with the original animator and skeleton, but only one combined SkinnedMeshRenderer. " +
            "Any active SkinnedMeshRenderers are merged together, and attached MeshRenderers are converted to bone-weighted geometry when present. " +
            "If you assign multiple source prefabs, the tool creates one combined prefab per source.",
            MessageType.Info);

        GameObject previewPrefab = GetPrimarySourcePrefab();
        if (previewPrefab != null)
        {
            var allSkinned = CollectActiveSkinnedMeshRenderers(previewPrefab.transform);
            for (int i = 0; i < allSkinned.Length; i++)
            {
                var sharedMesh = allSkinned[i] != null ? allSkinned[i].sharedMesh : null;
                if (sharedMesh == null || sharedMesh.blendShapeCount <= 0)
                    continue;

                EditorGUILayout.HelpBox(
                    "One of the source skinned meshes uses blend shapes. This converter does not preserve blend shapes in the combined mesh asset.",
                    MessageType.Warning);
                break;
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanConvert(out _)))
        {
            if (GUILayout.Button("Create Combined Skinned Prefab", GUILayout.Height(32f)))
                ConvertPrefab();
        }

        if (!CanConvert(out var reason))
            EditorGUILayout.HelpBox(reason, MessageType.Warning);
    }

    private bool CanConvert(out string reason)
    {
        if (_sourcePrefabs == null || _sourcePrefabs.Count == 0)
        {
            reason = "Assign at least one source prefab.";
            return false;
        }

        for (int sourceIndex = 0; sourceIndex < _sourcePrefabs.Count; sourceIndex++)
        {
            GameObject sourcePrefab = _sourcePrefabs[sourceIndex];
            if (sourcePrefab == null)
            {
                reason = $"Source prefab entry {sourceIndex + 1} is empty.";
                return false;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
            if (string.IsNullOrWhiteSpace(sourcePath) || !sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Source prefab '{sourcePrefab.name}' must be a prefab asset.";
                return false;
            }

            var animator = sourcePrefab.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                reason = $"Source prefab '{sourcePrefab.name}' must contain an Animator.";
                return false;
            }

            var allSkinned = CollectActiveSkinnedMeshRenderers(sourcePrefab.transform);
            if (allSkinned.Length == 0)
            {
                reason = $"Source prefab '{sourcePrefab.name}' must contain at least one SkinnedMeshRenderer.";
                return false;
            }

            for (int i = 0; i < allSkinned.Length; i++)
            {
                var skinned = allSkinned[i];
                if (skinned.sharedMesh == null)
                {
                    reason = $"Skinned mesh '{skinned.name}' in '{sourcePrefab.name}' is missing its mesh.";
                    return false;
                }

                if (!skinned.sharedMesh.isReadable)
                {
                    reason = $"Enable Read/Write on '{skinned.name}' mesh import settings, then reimport.";
                    return false;
                }
            }

            var primarySkinned = allSkinned[0];
            var attachments = CollectAttachmentRenderers(sourcePrefab.transform, primarySkinned);
            for (int i = 0; i < attachments.Count; i++)
            {
                Mesh mesh = attachments[i].MeshFilter.sharedMesh;
                if (mesh == null)
                {
                    reason = $"Attachment '{attachments[i].Renderer.name}' in '{sourcePrefab.name}' is missing a mesh.";
                    return false;
                }

                if (!mesh.isReadable)
                {
                    reason = $"Enable Read/Write on '{attachments[i].Renderer.name}' mesh import settings, then reimport.";
                    return false;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(_outputFolder) || !_outputFolder.Trim().StartsWith("Assets", StringComparison.Ordinal))
        {
            reason = "Output Folder must be under Assets/.";
            return false;
        }

        reason = "";
        return true;
    }

    private void ConvertPrefab()
    {
        if (!CanConvert(out var reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        EnsureFolderExists(_outputFolder.Trim());

        int createdCount = 0;
        for (int sourceIndex = 0; sourceIndex < _sourcePrefabs.Count; sourceIndex++)
        {
            GameObject sourcePrefab = _sourcePrefabs[sourceIndex];
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                var allSkinned = CollectActiveSkinnedMeshRenderers(instance.transform);
                if (allSkinned.Length == 0)
                    throw new InvalidOperationException($"Failed to find an active source SkinnedMeshRenderer on '{sourcePrefab.name}'.");

                var primarySkinned = allSkinned[0];
                var attachments = CollectAttachmentRenderers(instance.transform, primarySkinned);

                string baseName = BuildOutputBaseName(sourcePrefab);
                Mesh combinedMesh = BuildCombinedMesh(primarySkinned, allSkinned, attachments, baseName, out Transform[] combinedBones, out Material[] combinedMaterials);

                string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(_outputFolder.Trim(), baseName + ".asset").Replace('\\', '/'));
                AssetDatabase.CreateAsset(combinedMesh, meshPath);

                instance.name = baseName;
                GameObject primaryRendererObject = primarySkinned.gameObject;
                SkinnedMeshRenderer combinedRenderer = ReplaceWithCombinedRenderer(primarySkinned, combinedMesh, combinedBones, combinedMaterials);
                combinedRenderer.localBounds = combinedMesh.bounds;

                RemoveExtraSkinnedObjects(allSkinned, primaryRendererObject);
                RemoveAttachmentObjects(attachments);

                string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(_outputFolder.Trim(), baseName + ".prefab").Replace('\\', '/'));
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

                var createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Selection.activeObject = createdPrefab;
                EditorGUIUtility.PingObject(createdPrefab);
                Debug.Log($"Created combined skinned prefab at {prefabPath} with combined mesh asset at {meshPath}", createdPrefab);
                createdCount++;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                if (instance != null)
                    DestroyImmediate(instance);
            }
        }

        if (createdCount > 0)
            AssetDatabase.SaveAssets();
    }

    private static SkinnedMeshRenderer[] CollectActiveSkinnedMeshRenderers(Transform root)
    {
        if (root == null)
            return Array.Empty<SkinnedMeshRenderer>();

        var all = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var active = new List<SkinnedMeshRenderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            SkinnedMeshRenderer renderer = all[i];
            if (renderer == null)
                continue;
            if (!IsActiveInHierarchyWithinRoot(renderer.transform, root))
                continue;

            active.Add(renderer);
        }

        return active.ToArray();
    }

    private static SkinnedMeshRenderer ReplaceWithCombinedRenderer(
        SkinnedMeshRenderer sourceSkinned,
        Mesh combinedMesh,
        Transform[] combinedBones,
        Material[] combinedMaterials)
    {
        Transform sourceTransform = sourceSkinned.transform;
        Transform parent = sourceTransform.parent;

        var combinedObject = new GameObject(sourceTransform.name);
        combinedObject.transform.SetParent(parent, false);
        combinedObject.transform.localPosition = sourceTransform.localPosition;
        combinedObject.transform.localRotation = sourceTransform.localRotation;
        combinedObject.transform.localScale = sourceTransform.localScale;
        combinedObject.SetActive(sourceSkinned.gameObject.activeSelf);

        var combinedRenderer = combinedObject.AddComponent<SkinnedMeshRenderer>();
        CopyRendererSettings(sourceSkinned, combinedRenderer);
        combinedRenderer.sharedMesh = combinedMesh;
        combinedRenderer.bones = combinedBones;
        combinedRenderer.rootBone = sourceSkinned.rootBone;
        combinedRenderer.sharedMaterials = combinedMaterials;

        DestroyImmediate(sourceSkinned.gameObject);
        return combinedRenderer;
    }

    private static void CopyRendererSettings(SkinnedMeshRenderer source, SkinnedMeshRenderer destination)
    {
        destination.enabled = source.enabled;
        destination.shadowCastingMode = source.shadowCastingMode;
        destination.receiveShadows = source.receiveShadows;
        destination.lightProbeUsage = source.lightProbeUsage;
        destination.reflectionProbeUsage = source.reflectionProbeUsage;
        destination.probeAnchor = source.probeAnchor;
        destination.lightProbeProxyVolumeOverride = source.lightProbeProxyVolumeOverride;
        destination.motionVectorGenerationMode = source.motionVectorGenerationMode;
        destination.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        destination.renderingLayerMask = source.renderingLayerMask;
        destination.rendererPriority = source.rendererPriority;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.quality = source.quality;
        destination.updateWhenOffscreen = source.updateWhenOffscreen;
        destination.skinnedMotionVectors = source.skinnedMotionVectors;
        destination.rootBone = source.rootBone;
        destination.localBounds = source.localBounds;
        destination.sharedMaterials = source.sharedMaterials;
    }

    private void SyncOutputDefaultsFromSource()
    {
        GameObject sourcePrefab = GetPrimarySourcePrefab();
        if (sourcePrefab == null)
        {
            _lastSourcePrefabPath = "";
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        if (string.Equals(_lastSourcePrefabPath, sourcePath, StringComparison.Ordinal))
            return;

        string sourceFolder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(sourceFolder))
            _outputFolder = sourceFolder;

        _outputPrefabName = $"{sourcePrefab.name}_CombinedSkinned";
        _lastSourcePrefabPath = sourcePath;
    }

    private static void DrawSourcePrefabList(SerializedProperty sourcePrefabsProperty)
    {
        EditorGUILayout.LabelField("Source Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Drag one or more character prefab assets here. The first prefab also drives the default output folder/name.", MessageType.None);
        EditorGUILayout.PropertyField(
            sourcePrefabsProperty,
            new GUIContent("Prefabs", "Character prefabs with one or more SkinnedMeshRenderers and optional attached MeshRenderers."),
            true);
    }

    private GameObject GetPrimarySourcePrefab()
    {
        if (_sourcePrefabs == null)
            return null;

        for (int i = 0; i < _sourcePrefabs.Count; i++)
        {
            if (_sourcePrefabs[i] != null)
                return _sourcePrefabs[i];
        }

        return null;
    }

    private string BuildOutputBaseName(GameObject sourcePrefab)
    {
        if (_sourcePrefabs.Count == 1)
            return string.IsNullOrWhiteSpace(_outputPrefabName) ? $"{sourcePrefab.name}_CombinedSkinned" : _outputPrefabName.Trim();

        return $"{sourcePrefab.name}_CombinedSkinned";
    }

    private static List<AttachmentRendererInfo> CollectAttachmentRenderers(Transform root, SkinnedMeshRenderer sourceSkinned)
    {
        var results = new List<AttachmentRendererInfo>();
        if (root == null || sourceSkinned == null)
            return results;

        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (!IsActiveInHierarchyWithinRoot(renderer.transform, root))
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            Transform attachmentBone = FindAttachmentBone(renderer.transform, sourceSkinned);
            if (attachmentBone == null)
                continue;

            results.Add(new AttachmentRendererInfo(renderer, meshFilter, attachmentBone));
        }

        return results;
    }

    private static bool IsActiveInHierarchyWithinRoot(Transform transform, Transform root)
    {
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                return false;
            if (current == root)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static Transform FindAttachmentBone(Transform attachment, SkinnedMeshRenderer sourceSkinned)
    {
        if (attachment == null || sourceSkinned == null)
            return null;

        var validBones = sourceSkinned.bones;
        Transform current = attachment.parent;
        while (current != null)
        {
            if (current == sourceSkinned.rootBone)
                return current;

            for (int i = 0; i < validBones.Length; i++)
            {
                if (validBones[i] == current)
                    return current;
            }

            current = current.parent;
        }

        return sourceSkinned.rootBone != null ? sourceSkinned.rootBone : sourceSkinned.transform;
    }

    private static Mesh BuildCombinedMesh(
        SkinnedMeshRenderer primarySkinned,
        IReadOnlyList<SkinnedMeshRenderer> sourceSkinnedRenderers,
        List<AttachmentRendererInfo> attachments,
        string meshName,
        out Transform[] combinedBones,
        out Material[] combinedMaterials)
    {
        int sourceVertexCount = 0;
        for (int i = 0; i < sourceSkinnedRenderers.Count; i++)
            sourceVertexCount += sourceSkinnedRenderers[i].sharedMesh.vertexCount;

        int attachmentVertexCount = 0;
        for (int i = 0; i < attachments.Count; i++)
            attachmentVertexCount += attachments[i].MeshFilter.sharedMesh.vertexCount;

        int totalVertexCount = sourceVertexCount + attachmentVertexCount;
        var vertices = new List<Vector3>(totalVertexCount);
        var normals = new List<Vector3>(totalVertexCount);
        var tangents = new List<Vector4>(totalVertexCount);
        var colors = new List<Color>(totalVertexCount);
        var uv0 = new List<Vector2>(totalVertexCount);
        var uv1 = new List<Vector2>(totalVertexCount);
        var boneWeights = new List<BoneWeight>(totalVertexCount);
        var subMeshes = new List<List<int>>();
        var materials = new List<Material>();
        var subMeshIndexByMaterial = new Dictionary<Material, int>();

        Mesh primaryMesh = primarySkinned.sharedMesh;
        int primaryVertexCount = primaryMesh.vertexCount;
        bool useNormals = primaryMesh.normals != null && primaryMesh.normals.Length == primaryVertexCount;
        bool useTangents = primaryMesh.tangents != null && primaryMesh.tangents.Length == primaryVertexCount;
        bool useColors = primaryMesh.colors != null && primaryMesh.colors.Length == primaryVertexCount;
        bool useUv0 = primaryMesh.uv != null && primaryMesh.uv.Length == primaryVertexCount;
        bool useUv1 = primaryMesh.uv2 != null && primaryMesh.uv2.Length == primaryVertexCount;

        UpdateChannelUsageFromSkinnedSources(sourceSkinnedRenderers, ref useNormals, ref useTangents, ref useColors, ref useUv0, ref useUv1);
        UpdateChannelUsageFromAttachments(attachments, ref useNormals, ref useTangents, ref useColors, ref useUv0, ref useUv1);

        var bones = new List<Transform>();
        var bindposes = new List<Matrix4x4>();
        var boneIndexByTransform = new Dictionary<Transform, int>();

        for (int i = 0; i < sourceSkinnedRenderers.Count; i++)
        {
            AppendSkinnedSourceMesh(
                primarySkinned,
                sourceSkinnedRenderers[i],
                useNormals,
                useTangents,
                useColors,
                useUv0,
                useUv1,
                bones,
                bindposes,
                boneIndexByTransform,
                vertices,
                normals,
                tangents,
                colors,
                uv0,
                uv1,
                boneWeights,
                subMeshes,
                materials,
                subMeshIndexByMaterial);
        }

        for (int i = 0; i < attachments.Count; i++)
        {
            AppendAttachmentMesh(
                primarySkinned,
                attachments[i],
                useNormals,
                useTangents,
                useColors,
                useUv0,
                useUv1,
                bones,
                bindposes,
                boneIndexByTransform,
                vertices,
                normals,
                tangents,
                colors,
                uv0,
                uv1,
                boneWeights,
                subMeshes,
                materials,
                subMeshIndexByMaterial);
        }

        var combined = new Mesh
        {
            name = meshName,
            indexFormat = totalVertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
        };

        combined.SetVertices(vertices);
        if (useNormals)
            combined.SetNormals(normals);
        if (useTangents)
            combined.SetTangents(tangents);
        if (useColors)
            combined.SetColors(colors);
        if (useUv0)
            combined.SetUVs(0, uv0);
        if (useUv1)
            combined.SetUVs(1, uv1);

        combined.boneWeights = boneWeights.ToArray();
        combined.bindposes = bindposes.ToArray();
        combined.subMeshCount = subMeshes.Count;
        for (int i = 0; i < subMeshes.Count; i++)
            combined.SetTriangles(subMeshes[i], i, true);

        if (!useNormals)
            combined.RecalculateNormals();
        if (!useTangents)
            combined.RecalculateTangents();

        combined.RecalculateBounds();
        combinedBones = bones.ToArray();
        combinedMaterials = materials.ToArray();
        return combined;
    }

    private static void AppendSkinnedSourceMesh(
        SkinnedMeshRenderer primarySkinned,
        SkinnedMeshRenderer sourceSkinned,
        bool useNormals,
        bool useTangents,
        bool useColors,
        bool useUv0,
        bool useUv1,
        List<Transform> bones,
        List<Matrix4x4> bindposes,
        Dictionary<Transform, int> boneIndexByTransform,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<Color> colors,
        List<Vector2> uv0,
        List<Vector2> uv1,
        List<BoneWeight> boneWeights,
        List<List<int>> subMeshes,
        List<Material> materials,
        Dictionary<Material, int> subMeshIndexByMaterial)
    {
        Mesh sourceMesh = sourceSkinned.sharedMesh;
        int vertexOffset = vertices.Count;
        Matrix4x4 toPrimaryLocal = primarySkinned.transform.worldToLocalMatrix * sourceSkinned.transform.localToWorldMatrix;
        Vector3[] sourceVertices = sourceMesh.vertices;
        Vector3[] sourceNormals = sourceMesh.normals;
        Vector4[] sourceTangents = sourceMesh.tangents;
        Color[] sourceColors = sourceMesh.colors;
        Vector2[] sourceUv0 = sourceMesh.uv;
        Vector2[] sourceUv1 = sourceMesh.uv2;
        BoneWeight[] sourceBoneWeights = sourceMesh.boneWeights;
        Transform[] sourceBones = sourceSkinned.bones;
        int[] boneRemap = BuildBoneRemap(primarySkinned, sourceSkinned, sourceBones, bones, bindposes, boneIndexByTransform);

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            vertices.Add(toPrimaryLocal.MultiplyPoint3x4(sourceVertices[i]));
            if (useNormals)
            {
                Vector3 normal = sourceNormals != null && sourceNormals.Length == sourceVertices.Length
                    ? toPrimaryLocal.MultiplyVector(sourceNormals[i]).normalized
                    : Vector3.zero;
                normals.Add(normal);
            }
            if (useTangents)
            {
                if (sourceTangents != null && sourceTangents.Length == sourceVertices.Length)
                {
                    Vector3 tangentDirection = toPrimaryLocal.MultiplyVector(new Vector3(sourceTangents[i].x, sourceTangents[i].y, sourceTangents[i].z)).normalized;
                    tangents.Add(new Vector4(tangentDirection.x, tangentDirection.y, tangentDirection.z, sourceTangents[i].w));
                }
                else
                {
                    tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                }
            }
            if (useColors)
                colors.Add(sourceColors != null && sourceColors.Length == sourceVertices.Length ? sourceColors[i] : Color.white);
            if (useUv0)
                uv0.Add(sourceUv0 != null && sourceUv0.Length == sourceVertices.Length ? sourceUv0[i] : Vector2.zero);
            if (useUv1)
                uv1.Add(sourceUv1 != null && sourceUv1.Length == sourceVertices.Length ? sourceUv1[i] : Vector2.zero);

            boneWeights.Add(sourceBoneWeights != null && sourceBoneWeights.Length == sourceVertices.Length
                ? RemapBoneWeight(sourceBoneWeights[i], boneRemap)
                : DefaultBoneWeight());
        }

        for (int subMeshIndex = 0; subMeshIndex < sourceMesh.subMeshCount; subMeshIndex++)
        {
            Material material = GetMaterial(sourceSkinned.sharedMaterials, subMeshIndex);
            List<int> combinedSubMesh = GetOrCreateSubMeshTriangles(material, subMeshes, materials, subMeshIndexByMaterial);
            int[] triangles = sourceMesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < triangles.Length; i++)
                combinedSubMesh.Add(vertexOffset + triangles[i]);
        }
    }

    private static int[] BuildBoneRemap(
        SkinnedMeshRenderer primarySkinned,
        SkinnedMeshRenderer sourceSkinned,
        Transform[] sourceBones,
        List<Transform> bones,
        List<Matrix4x4> bindposes,
        Dictionary<Transform, int> boneIndexByTransform)
    {
        if (sourceBones == null || sourceBones.Length == 0)
            return Array.Empty<int>();

        var remap = new int[sourceBones.Length];
        for (int i = 0; i < sourceBones.Length; i++)
        {
            Transform sourceBone = sourceBones[i] != null ? sourceBones[i] : (sourceSkinned.rootBone != null ? sourceSkinned.rootBone : sourceSkinned.transform);
            remap[i] = GetOrCreateBoneIndex(primarySkinned, sourceBone, bones, bindposes, boneIndexByTransform);
        }

        return remap;
    }

    private static BoneWeight RemapBoneWeight(BoneWeight sourceWeight, int[] boneRemap)
    {
        return new BoneWeight
        {
            boneIndex0 = RemapBoneIndex(sourceWeight.boneIndex0, boneRemap),
            boneIndex1 = RemapBoneIndex(sourceWeight.boneIndex1, boneRemap),
            boneIndex2 = RemapBoneIndex(sourceWeight.boneIndex2, boneRemap),
            boneIndex3 = RemapBoneIndex(sourceWeight.boneIndex3, boneRemap),
            weight0 = sourceWeight.weight0,
            weight1 = sourceWeight.weight1,
            weight2 = sourceWeight.weight2,
            weight3 = sourceWeight.weight3
        };
    }

    private static int RemapBoneIndex(int sourceIndex, int[] boneRemap)
    {
        if (boneRemap == null || boneRemap.Length == 0)
            return 0;
        if ((uint)sourceIndex >= (uint)boneRemap.Length)
            return boneRemap[0];

        return boneRemap[sourceIndex];
    }

    private static void AppendAttachmentMesh(
        SkinnedMeshRenderer sourceSkinned,
        AttachmentRendererInfo attachment,
        bool useNormals,
        bool useTangents,
        bool useColors,
        bool useUv0,
        bool useUv1,
        List<Transform> bones,
        List<Matrix4x4> bindposes,
        Dictionary<Transform, int> boneIndexByTransform,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<Color> colors,
        List<Vector2> uv0,
        List<Vector2> uv1,
        List<BoneWeight> boneWeights,
        List<List<int>> subMeshes,
        List<Material> materials,
        Dictionary<Material, int> subMeshIndexByMaterial)
    {
        Mesh mesh = attachment.MeshFilter.sharedMesh;
        int vertexOffset = vertices.Count;
        int boneIndex = GetOrCreateBoneIndex(sourceSkinned, attachment.Bone, bones, bindposes, boneIndexByTransform);
        Matrix4x4 toSkinnedLocal = sourceSkinned.transform.worldToLocalMatrix * attachment.Renderer.transform.localToWorldMatrix;

        Vector3[] sourceVertices = mesh.vertices;
        Vector3[] sourceNormals = mesh.normals;
        Vector4[] sourceTangents = mesh.tangents;
        Color[] sourceColors = mesh.colors;
        Vector2[] sourceUv0 = mesh.uv;
        Vector2[] sourceUv1 = mesh.uv2;

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            vertices.Add(toSkinnedLocal.MultiplyPoint3x4(sourceVertices[i]));

            if (useNormals)
            {
                Vector3 normal = sourceNormals != null && sourceNormals.Length == sourceVertices.Length
                    ? toSkinnedLocal.MultiplyVector(sourceNormals[i]).normalized
                    : Vector3.zero;
                normals.Add(normal);
            }

            if (useTangents)
            {
                if (sourceTangents != null && sourceTangents.Length == sourceVertices.Length)
                {
                    Vector3 tangentDirection = toSkinnedLocal.MultiplyVector(new Vector3(sourceTangents[i].x, sourceTangents[i].y, sourceTangents[i].z)).normalized;
                    tangents.Add(new Vector4(tangentDirection.x, tangentDirection.y, tangentDirection.z, sourceTangents[i].w));
                }
                else
                {
                    tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                }
            }

            if (useColors)
                colors.Add(sourceColors != null && sourceColors.Length == sourceVertices.Length ? sourceColors[i] : Color.white);
            if (useUv0)
                uv0.Add(sourceUv0 != null && sourceUv0.Length == sourceVertices.Length ? sourceUv0[i] : Vector2.zero);
            if (useUv1)
                uv1.Add(sourceUv1 != null && sourceUv1.Length == sourceVertices.Length ? sourceUv1[i] : Vector2.zero);

            boneWeights.Add(new BoneWeight
            {
                boneIndex0 = boneIndex,
                weight0 = 1f
            });
        }

        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            Material material = GetMaterial(attachment.Renderer.sharedMaterials, subMeshIndex);
            List<int> combinedSubMesh = GetOrCreateSubMeshTriangles(material, subMeshes, materials, subMeshIndexByMaterial);
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < triangles.Length; i++)
                combinedSubMesh.Add(vertexOffset + triangles[i]);
        }
    }

    private static List<int> GetOrCreateSubMeshTriangles(
        Material material,
        List<List<int>> subMeshes,
        List<Material> materials,
        Dictionary<Material, int> subMeshIndexByMaterial)
    {
        if (subMeshIndexByMaterial.TryGetValue(material, out int existingIndex))
            return subMeshes[existingIndex];

        int newIndex = subMeshes.Count;
        var triangles = new List<int>();
        subMeshes.Add(triangles);
        materials.Add(material);
        subMeshIndexByMaterial[material] = newIndex;
        return triangles;
    }

    private static int GetOrCreateBoneIndex(
        SkinnedMeshRenderer sourceSkinned,
        Transform bone,
        List<Transform> bones,
        List<Matrix4x4> bindposes,
        Dictionary<Transform, int> boneIndexByTransform)
    {
        if (boneIndexByTransform.TryGetValue(bone, out int existingIndex))
            return existingIndex;

        int newIndex = bones.Count;
        bones.Add(bone);
        bindposes.Add(bone.worldToLocalMatrix * sourceSkinned.transform.localToWorldMatrix);
        boneIndexByTransform[bone] = newIndex;
        return newIndex;
    }

    private static void UpdateChannelUsageFromAttachments(
        List<AttachmentRendererInfo> attachments,
        ref bool useNormals,
        ref bool useTangents,
        ref bool useColors,
        ref bool useUv0,
        ref bool useUv1)
    {
        for (int i = 0; i < attachments.Count; i++)
        {
            Mesh mesh = attachments[i].MeshFilter.sharedMesh;
            int vertexCount = mesh.vertexCount;
            useNormals |= mesh.normals != null && mesh.normals.Length == vertexCount;
            useTangents |= mesh.tangents != null && mesh.tangents.Length == vertexCount;
            useColors |= mesh.colors != null && mesh.colors.Length == vertexCount;
            useUv0 |= mesh.uv != null && mesh.uv.Length == vertexCount;
            useUv1 |= mesh.uv2 != null && mesh.uv2.Length == vertexCount;
        }
    }

    private static void UpdateChannelUsageFromSkinnedSources(
        IReadOnlyList<SkinnedMeshRenderer> sourceSkinnedRenderers,
        ref bool useNormals,
        ref bool useTangents,
        ref bool useColors,
        ref bool useUv0,
        ref bool useUv1)
    {
        for (int i = 0; i < sourceSkinnedRenderers.Count; i++)
        {
            Mesh mesh = sourceSkinnedRenderers[i].sharedMesh;
            int vertexCount = mesh.vertexCount;
            useNormals |= mesh.normals != null && mesh.normals.Length == vertexCount;
            useTangents |= mesh.tangents != null && mesh.tangents.Length == vertexCount;
            useColors |= mesh.colors != null && mesh.colors.Length == vertexCount;
            useUv0 |= mesh.uv != null && mesh.uv.Length == vertexCount;
            useUv1 |= mesh.uv2 != null && mesh.uv2.Length == vertexCount;
        }
    }

    private static void RemoveAttachmentObjects(List<AttachmentRendererInfo> attachments)
    {
        for (int i = 0; i < attachments.Count; i++)
        {
            MeshRenderer renderer = attachments[i].Renderer;
            if (renderer == null)
                continue;

            GameObject attachmentObject = renderer.gameObject;
            if (attachmentObject == null)
                continue;

            DestroyImmediate(attachmentObject);
        }
    }

    private static void RemoveExtraSkinnedObjects(IReadOnlyList<SkinnedMeshRenderer> allSkinned, GameObject primaryRendererObject)
    {
        var removedObjects = new HashSet<GameObject>();
        for (int i = 0; i < allSkinned.Count; i++)
        {
            SkinnedMeshRenderer renderer = allSkinned[i];
            if (renderer == null)
                continue;

            GameObject rendererObject = renderer.gameObject;
            if (rendererObject == null || rendererObject == primaryRendererObject)
                continue;
            if (!removedObjects.Add(rendererObject))
                continue;

            DestroyImmediate(rendererObject);
        }
    }

    private static Material GetMaterial(Material[] materials, int index)
    {
        if (materials == null || materials.Length == 0)
            return null;
        if (index < materials.Length && materials[index] != null)
            return materials[index];

        return materials[materials.Length - 1];
    }

    private static BoneWeight DefaultBoneWeight()
    {
        return new BoneWeight
        {
            boneIndex0 = 0,
            weight0 = 1f
        };
    }

    private static void EnsureFolderExists(string assetFolder)
    {
        if (string.IsNullOrWhiteSpace(assetFolder))
            return;
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private readonly struct AttachmentRendererInfo
    {
        public AttachmentRendererInfo(MeshRenderer renderer, MeshFilter meshFilter, Transform bone)
        {
            Renderer = renderer;
            MeshFilter = meshFilter;
            Bone = bone;
        }

        public MeshRenderer Renderer { get; }
        public MeshFilter MeshFilter { get; }
        public Transform Bone { get; }
    }
}
