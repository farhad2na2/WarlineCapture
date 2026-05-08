using System.Collections.Generic;
using System.IO;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class UnitMidLodGenerator
{
    private const string SoldierBakedPrefabPath = "Assets/Game/Prefabs/Generated/CharactersBaked/Prefab_SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31.prefab";
    private const string SoldierConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";
    private const string OutputFolder = "Assets/Game/Prefabs/Generated/MidLOD";
    private const string OutputMeshPath = OutputFolder + "/MidLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset";
    private const string OutputPrefabPath = OutputFolder + "/MidLOD_Unit_Chr_Soldier_Male_02_Alt_04.prefab";
    private const string OutputLowMeshPath = OutputFolder + "/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset";
    private const string OutputLowPrefabPath = OutputFolder + "/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.prefab";
    private const float SoldierMidTriangleRatio = 0.35f;
    private const float SoldierLowTriangleRatio = 0.85f;
    private const float VehicleTriangleRatio = 0.25f;
    private const float VehicleLowTriangleRatio = 0.08f;
    private const string VehiclePrefabFolder = "Assets/Game/Prefabs/Vehicles";
    private const string ConfigFolder = "Assets/Game/Configs/Prefabs";
    public static void GenerateSoldierMale02Alt04()
    {
        GameObject bakedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierBakedPrefabPath);
        if (bakedPrefab == null)
        {
            Debug.LogError($"[MidLodGen] Missing baked visible source prefab: {SoldierBakedPrefabPath}");
            return;
        }

        EnsureOutputFolder();

        AssetDatabase.DeleteAsset(OutputMeshPath);
        AssetDatabase.DeleteAsset(OutputLowMeshPath);
        AssetDatabase.DeleteAsset(OutputPrefabPath);
        AssetDatabase.DeleteAsset(OutputLowPrefabPath);

        if (!AssetDatabase.CopyAsset(SoldierBakedPrefabPath, OutputPrefabPath))
        {
            Debug.LogError($"[MidLodGen] Failed to copy mid LOD prefab source={SoldierBakedPrefabPath} target={OutputPrefabPath}");
            return;
        }

        if (!AssetDatabase.CopyAsset(SoldierBakedPrefabPath, OutputLowPrefabPath))
        {
            Debug.LogError($"[MidLodGen] Failed to copy low LOD prefab source={SoldierBakedPrefabPath} target={OutputLowPrefabPath}");
            return;
        }

        AssetDatabase.ImportAsset(OutputPrefabPath);
        AssetDatabase.ImportAsset(OutputLowPrefabPath);

        MeshStats midStats = ApplyReducedMeshToCopiedPrefab(OutputPrefabPath, OutputMeshPath, SoldierMidTriangleRatio);
        MeshStats lowStats = ApplyReducedMeshToCopiedPrefab(OutputLowPrefabPath, OutputLowMeshPath, SoldierLowTriangleRatio);

        GameObject midPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
        GameObject lowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputLowPrefabPath);
        if (midPrefab == null || lowPrefab == null)
        {
            Debug.LogError($"[MidLodGen] Copied LOD prefab failed to load mid={midPrefab != null} low={lowPrefab != null}");
            return;
        }

        AssignMidLodToConfig(midPrefab);
        AssignLowLodToConfig(lowPrefab);

        MeshStats stats = CountPrefabMeshStats(bakedPrefab);
        int materialSlots = CountPrefabMaterialSlots(bakedPrefab);
        Debug.Log($"[MidLodGen] generated unit=Unit_Chr_Soldier_Male_02_Alt_04 mode=copy-baked-reduce sourcePrefab={SoldierBakedPrefabPath} sourceVerts={stats.Vertices} sourceTris={stats.Triangles} sourceSubMeshes={stats.SubMeshes} materialSlots={materialSlots} midVerts={midStats.Vertices} midTris={midStats.Triangles} midSubMeshes={midStats.SubMeshes} midRatio={(midStats.Triangles / Mathf.Max(1f, stats.Triangles)):F2} lowVerts={lowStats.Vertices} lowTris={lowStats.Triangles} lowSubMeshes={lowStats.SubMeshes} lowRatio={(lowStats.Triangles / Mathf.Max(1f, stats.Triangles)):F2} prefab={OutputPrefabPath} lowPrefab={OutputLowPrefabPath}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/WarlineCapture/Rendering/LODs/Generate All Soldier LODs")]
    public static void GenerateAllSoldierLods()
    {
        GenerateSoldierMale02Alt04();
    }

    [MenuItem("Tools/WarlineCapture/Rendering/LODs/Generate All Vehicle LODs")]
    public static void GenerateAllVehicleLods()
    {
        EnsureOutputFolder();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { VehiclePrefabFolder });
        int generated = 0;
        int skipped = 0;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            string unitName = Path.GetFileNameWithoutExtension(prefabPath);
            if (string.IsNullOrWhiteSpace(unitName) || !unitName.StartsWith("Unit_Veh_"))
                continue;

            string configPath = ResolveVehicleConfigPath(unitName);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                Debug.LogWarning($"[VehicleLodGen] skipped unit={unitName} reason=missingConfig prefab={prefabPath}");
                skipped++;
                continue;
            }

            if (GenerateMultiRendererUnitLods(prefabPath, configPath, unitName, VehicleTriangleRatio, VehicleLowTriangleRatio))
                generated++;
            else
                skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VehicleLodGen] complete generated={generated} skipped={skipped}");
    }

    private static bool GenerateMultiRendererUnitLods(string unitPrefabPath, string configPath, string unitName, float midRatio, float lowRatio)
    {
        GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitPrefabPath);
        if (unitPrefab == null)
        {
            Debug.LogError($"[VehicleLodGen] Missing source unit prefab: {unitPrefabPath}");
            return false;
        }

        GameObject loadedUnit = PrefabUtility.LoadPrefabContents(unitPrefabPath);
        try
        {
            Transform modelRoot = loadedUnit.transform.Find("Model");
            if (modelRoot == null)
            {
                Debug.LogWarning($"[VehicleLodGen] skipped unit={unitName} reason=missingModel prefab={unitPrefabPath}");
                return false;
            }

            List<SourceRendererInfo> sources = CollectSourceRenderers(modelRoot);
            if (sources.Count == 0)
            {
                Debug.LogWarning($"[VehicleLodGen] skipped unit={unitName} reason=noMeshRenderers prefab={unitPrefabPath}");
                return false;
            }

            string safeName = SanitizeAssetName(unitName);
            string midMeshPath = $"{OutputFolder}/MidLOD_{safeName}.asset";
            string midPrefabPath = $"{OutputFolder}/MidLOD_{safeName}.prefab";
            string lowMeshPath = $"{OutputFolder}/LowLOD_{safeName}.asset";
            string lowPrefabPath = $"{OutputFolder}/LowLOD_{safeName}.prefab";
            List<Mesh> midMeshes = BuildReducedMeshes(sources, midRatio, "MidLOD");
            List<Mesh> lowMeshes = BuildReducedMeshes(sources, lowRatio, "LowLOD");

            SaveMeshAsset(midMeshPath, midMeshes);
            SaveMeshAsset(lowMeshPath, lowMeshes);

            GameObject midRoot = null;
            GameObject lowRoot = null;
            try
            {
                midRoot = BuildMultiRendererLodPrefabRoot($"MidLOD_{safeName}", sources, midMeshes);
                lowRoot = BuildMultiRendererLodPrefabRoot($"LowLOD_{safeName}", sources, lowMeshes);

                AssetDatabase.DeleteAsset(midPrefabPath);
                GameObject midPrefab = PrefabUtility.SaveAsPrefabAsset(midRoot, midPrefabPath);
                AssetDatabase.DeleteAsset(lowPrefabPath);
                GameObject lowPrefab = PrefabUtility.SaveAsPrefabAsset(lowRoot, lowPrefabPath);
                AssignLodsToConfig(configPath, midPrefab, lowPrefab, unitName);

                MeshStats sourceStats = CountSourceStats(sources);
                MeshStats midStats = CountMeshStats(midMeshes);
                MeshStats lowStats = CountMeshStats(lowMeshes);
                Debug.Log($"[VehicleLodGen] generated unit={unitName} renderers={sources.Count} sourceVerts={sourceStats.Vertices} sourceTris={sourceStats.Triangles} sourceSubMeshes={sourceStats.SubMeshes} midVerts={midStats.Vertices} midTris={midStats.Triangles} midSubMeshes={midStats.SubMeshes} midRatio={(midStats.Triangles / Mathf.Max(1f, sourceStats.Triangles)):F2} lowVerts={lowStats.Vertices} lowTris={lowStats.Triangles} lowSubMeshes={lowStats.SubMeshes} lowRatio={(lowStats.Triangles / Mathf.Max(1f, sourceStats.Triangles)):F2} config={configPath}");
                return true;
            }
            finally
            {
                if (midRoot != null)
                    Object.DestroyImmediate(midRoot);
                if (lowRoot != null)
                    Object.DestroyImmediate(lowRoot);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(loadedUnit);
        }
    }

    private static GameObject BuildLodPrefabRoot(string name, Mesh mesh, MeshRenderer sourceRenderer, MaterialAnimatorIndexAuthoring sourceIndex)
    {
        GameObject root = new(name);
        MeshFilter filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        DisableSmallMeshCulling(renderer);
        root.AddComponent<UnitSafeVisibleCharacterLodAuthoring>();

        if (sourceIndex != null)
        {
            MaterialAnimatorIndexAuthoring index = root.AddComponent<MaterialAnimatorIndexAuthoring>();
            EditorUtility.CopySerialized(sourceIndex, index);
        }

        return root;
    }

    private static bool PrefabHasUnsafeGeneratedLodSettings(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return true;

        if (prefab.GetComponentInChildren<LODGroup>(true) != null)
            return true;

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return true;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].allowOcclusionWhenDynamic)
                return true;
        }

        return false;
    }

    private static void AddSingleRendererLodGroup(GameObject root, Renderer renderer)
    {
        if (root == null || renderer == null)
            return;

        LODGroup lodGroup = root.GetComponent<LODGroup>();
        if (lodGroup == null)
            lodGroup = root.AddComponent<LODGroup>();

        lodGroup.SetLODs(new[]
        {
            new LOD(0f, new[] { renderer })
        });
        lodGroup.fadeMode = LODFadeMode.None;
        lodGroup.animateCrossFading = false;
        lodGroup.RecalculateBounds();
    }

    private static GameObject BuildMultiRendererLodPrefabRoot(string name, List<SourceRendererInfo> sources, List<Mesh> meshes)
    {
        GameObject root = new(name);
        List<Renderer> renderers = new(sources.Count);
        for (int i = 0; i < sources.Count; i++)
        {
            SourceRendererInfo source = sources[i];
            GameObject child = new(source.Name);
            Transform childTransform = child.transform;
            childTransform.SetParent(root.transform, false);
            childTransform.localPosition = source.LocalPosition;
            childTransform.localRotation = source.LocalRotation;
            childTransform.localScale = source.LocalScale;

            MeshFilter filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = meshes[i];
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = ResolveLodMaterials(source.Renderer);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            DisableSmallMeshCulling(renderer);
            renderers.Add(renderer);
        }

        return root;
    }

    private static void DisableSmallMeshCulling(Renderer renderer)
    {
        SerializedObject serializedRenderer = new(renderer);
        SerializedProperty smallMeshCulling = serializedRenderer.FindProperty("m_SmallMeshCulling");
        if (smallMeshCulling != null)
        {
            smallMeshCulling.boolValue = false;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Mesh BuildSafeCharacterLodMesh(Mesh source, float triangleRatio)
    {
        if (triangleRatio >= 0.999f)
            return Object.Instantiate(source);

        Debug.LogWarning($"[MidLodGen] Character triangle reduction is disabled for GPU animated soldiers. Requested ratio={triangleRatio:F2}; generating exact mesh copy instead.");
        return Object.Instantiate(source);
    }

    private static Mesh BuildTriangleReducedMesh(Mesh source, float triangleRatio, bool mergeSubmeshes)
    {
        if (!mergeSubmeshes)
            return BuildIndexReducedMesh(source, triangleRatio);

        Vector3[] sourceVertices = source.vertices;
        Vector3[] sourceNormals = source.normals;
        Vector4[] sourceTangents = source.tangents;
        Color[] sourceColors = source.colors;
        Color32[] sourceColors32 = source.colors32;
        BoneWeight[] sourceBoneWeights = source.boneWeights;
        List<Vector4>[] sourceUvs = ReadUvs(source);

        List<Vector3> vertices = new();
        List<Vector3> normals = sourceNormals.Length == source.vertexCount ? new List<Vector3>() : null;
        List<Vector4> tangents = sourceTangents.Length == source.vertexCount ? new List<Vector4>() : null;
        List<Color> colors = sourceColors.Length == source.vertexCount ? new List<Color>() : null;
        List<Color32> colors32 = sourceColors.Length != source.vertexCount && sourceColors32.Length == source.vertexCount ? new List<Color32>() : null;
        List<BoneWeight> boneWeights = sourceBoneWeights.Length == source.vertexCount ? new List<BoneWeight>() : null;
        List<Vector4>[] uvs = CreateUvWriters(sourceUvs);
        Dictionary<int, int> remap = new();
        List<int>[] submeshTriangles = new List<int>[source.subMeshCount];

        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int[] triangles = source.GetTriangles(submesh);
            List<int> selectedTriangleStarts = SelectTriangleStarts(sourceVertices, triangles, triangleRatio);
            List<int> outputTriangles = new(selectedTriangleStarts.Count * 3);
            for (int i = 0; i < selectedTriangleStarts.Count; i++)
            {
                int start = selectedTriangleStarts[i];
                outputTriangles.Add(CopyVertex(triangles[start], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 1], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 2], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
            }

            submeshTriangles[submesh] = outputTriangles;
        }

        Mesh mesh = new();
        mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        if (normals != null)
            mesh.SetNormals(normals);
        if (tangents != null)
            mesh.SetTangents(tangents);
        if (colors != null)
            mesh.SetColors(colors);
        if (colors32 != null)
            mesh.SetColors(colors32);
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel] != null)
                mesh.SetUVs(channel, uvs[channel]);
        }
        if (boneWeights != null)
            mesh.boneWeights = boneWeights.ToArray();
        mesh.bindposes = source.bindposes;
        if (mergeSubmeshes)
        {
            List<int> mergedTriangles = new();
            for (int submesh = 0; submesh < submeshTriangles.Length; submesh++)
                mergedTriangles.AddRange(submeshTriangles[submesh]);

            mesh.subMeshCount = 1;
            mesh.SetTriangles(mergedTriangles, 0, true);
        }
        else
        {
            mesh.subMeshCount = source.subMeshCount;
            for (int submesh = 0; submesh < submeshTriangles.Length; submesh++)
                mesh.SetTriangles(submeshTriangles[submesh], submesh, true);
        }

        mesh.bounds = source.bounds;
        return mesh;
    }

    private static Mesh BuildIndexReducedMesh(Mesh source, float triangleRatio)
    {
        Mesh mesh = Object.Instantiate(source);
        mesh.name = source.name;
        mesh.subMeshCount = source.subMeshCount;
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int[] triangles = source.GetTriangles(submesh);
            List<int> selectedTriangleStarts = SelectTriangleStarts(source.vertices, triangles, triangleRatio);
            int[] reducedTriangles = new int[selectedTriangleStarts.Count * 3];
            for (int i = 0; i < selectedTriangleStarts.Count; i++)
            {
                int sourceStart = selectedTriangleStarts[i];
                int targetStart = i * 3;
                reducedTriangles[targetStart] = triangles[sourceStart];
                reducedTriangles[targetStart + 1] = triangles[sourceStart + 1];
                reducedTriangles[targetStart + 2] = triangles[sourceStart + 2];
            }

            mesh.SetTriangles(reducedTriangles, submesh, true);
        }

        mesh.bounds = source.bounds;
        return mesh;
    }

    private static Mesh BuildCompactTriangleReducedMesh(Mesh source, float triangleRatio)
    {
        Vector3[] sourceVertices = source.vertices;
        Vector3[] sourceNormals = source.normals;
        Vector4[] sourceTangents = source.tangents;
        Color[] sourceColors = source.colors;
        Color32[] sourceColors32 = source.colors32;
        BoneWeight[] sourceBoneWeights = source.boneWeights;
        List<Vector4>[] sourceUvs = ReadUvs(source);

        List<Vector3> vertices = new();
        List<Vector3> normals = sourceNormals.Length == source.vertexCount ? new List<Vector3>() : null;
        List<Vector4> tangents = sourceTangents.Length == source.vertexCount ? new List<Vector4>() : null;
        List<Color> colors = sourceColors.Length == source.vertexCount ? new List<Color>() : null;
        List<Color32> colors32 = sourceColors.Length != source.vertexCount && sourceColors32.Length == source.vertexCount ? new List<Color32>() : null;
        List<BoneWeight> boneWeights = sourceBoneWeights.Length == source.vertexCount ? new List<BoneWeight>() : null;
        List<Vector4>[] uvs = CreateUvWriters(sourceUvs);
        Dictionary<int, int> remap = new();
        List<int>[] submeshTriangles = new List<int>[source.subMeshCount];

        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int[] triangles = source.GetTriangles(submesh);
            List<int> selectedTriangleStarts = SelectTriangleStarts(sourceVertices, triangles, triangleRatio);
            List<int> outputTriangles = new(selectedTriangleStarts.Count * 3);
            for (int i = 0; i < selectedTriangleStarts.Count; i++)
            {
                int start = selectedTriangleStarts[i];
                outputTriangles.Add(CopyVertex(triangles[start], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 1], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 2], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceBoneWeights, sourceUvs, vertices, normals, tangents, colors, colors32, boneWeights, uvs, remap));
            }

            submeshTriangles[submesh] = outputTriangles;
        }

        Mesh mesh = new();
        mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        if (normals != null)
            mesh.SetNormals(normals);
        if (tangents != null)
            mesh.SetTangents(tangents);
        if (colors != null)
            mesh.SetColors(colors);
        if (colors32 != null)
            mesh.SetColors(colors32);
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel] != null)
                mesh.SetUVs(channel, uvs[channel]);
        }
        if (boneWeights != null)
            mesh.boneWeights = boneWeights.ToArray();
        mesh.bindposes = source.bindposes;
        mesh.subMeshCount = source.subMeshCount;
        for (int submesh = 0; submesh < submeshTriangles.Length; submesh++)
            mesh.SetTriangles(submeshTriangles[submesh], submesh, true);

        mesh.bounds = source.bounds;
        return mesh;
    }

    private static bool CanMergeSubmeshes(MeshRenderer sourceRenderer)
    {
        Material[] materials = sourceRenderer.sharedMaterials;
        Material first = null;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
                return false;

            if (first == null)
            {
                first = material;
                continue;
            }

            if (material != first)
                return false;
        }

        return first != null;
    }

    private static Material[] ResolveLodMaterials(MeshRenderer sourceRenderer)
    {
        Material[] materials = sourceRenderer.sharedMaterials;
        if (CanMergeSubmeshes(sourceRenderer))
            return new[] { materials[0] };

        return materials;
    }

    private static List<SourceRendererInfo> CollectSourceRenderers(Transform modelRoot)
    {
        MeshFilter[] filters = modelRoot.GetComponentsInChildren<MeshFilter>(true);
        List<SourceRendererInfo> sources = new(filters.Length);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || filter.sharedMesh == null)
                continue;

            Matrix4x4 localToModel = modelRoot.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            DecomposeMatrix(localToModel, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            sources.Add(new SourceRendererInfo(filter.gameObject.name, filter.sharedMesh, renderer, position, rotation, scale));
        }

        return sources;
    }

    private static List<Mesh> BuildReducedMeshes(List<SourceRendererInfo> sources, float ratio, string prefix)
    {
        List<Mesh> meshes = new(sources.Count);
        for (int i = 0; i < sources.Count; i++)
        {
            SourceRendererInfo source = sources[i];
            bool mergeSubmeshes = CanMergeSubmeshes(source.Renderer);
            Mesh mesh = BuildTriangleReducedMesh(source.Mesh, ratio, mergeSubmeshes);
            mesh.name = $"{prefix}_{SanitizeAssetName(source.Name)}_{i:00}";
            meshes.Add(mesh);
        }

        return meshes;
    }

    private static void SaveMeshAsset(string path, List<Mesh> meshes)
    {
        AssetDatabase.DeleteAsset(path);
        if (meshes == null || meshes.Count == 0)
            return;

        AssetDatabase.CreateAsset(meshes[0], path);
        for (int i = 1; i < meshes.Count; i++)
            AssetDatabase.AddObjectToAsset(meshes[i], path);
    }

    private static MeshStats ApplyReducedMeshToCopiedPrefab(string prefabPath, string meshAssetPath, float triangleRatio)
    {
        GameObject loadedPrefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            MeshFilter filter = loadedPrefab.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || filter.sharedMesh == null || renderer == null)
            {
                Debug.LogError($"[MidLodGen] Copied prefab has no MeshFilter/MeshRenderer prefab={prefabPath}");
                return new MeshStats();
            }

            Mesh reducedMesh = BuildCompactTriangleReducedMesh(filter.sharedMesh, triangleRatio);
            reducedMesh.name = Path.GetFileNameWithoutExtension(meshAssetPath);
            AssetDatabase.DeleteAsset(meshAssetPath);
            AssetDatabase.CreateAsset(reducedMesh, meshAssetPath);
            filter.sharedMesh = reducedMesh;

            LODGroup lodGroup = loadedPrefab.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.RecalculateBounds();

            PrefabUtility.SaveAsPrefabAsset(loadedPrefab, prefabPath);

            MeshStats stats = new MeshStats();
            AddMeshStats(ref stats, reducedMesh);
            return stats;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(loadedPrefab);
        }
    }

    private static string ResolveVehicleConfigPath(string unitName)
    {
        string configName = unitName.StartsWith("Unit_") ? unitName.Substring("Unit_".Length) : unitName;
        string[] candidates =
        {
            $"{ConfigFolder}/Prefab_UnitGrid_{configName}_Config.asset",
            $"{ConfigFolder}/Prefab_UnitGrid_{configName}.asset"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(candidates[i]) != null)
                return candidates[i];
        }

        return null;
    }

    private static string SanitizeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unnamed";

        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = value;
        for (int i = 0; i < invalid.Length; i++)
            sanitized = sanitized.Replace(invalid[i], '_');
        return sanitized.Replace(' ', '_');
    }

    private static void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetColumn(3);
        Vector3 right = matrix.GetColumn(0);
        Vector3 up = matrix.GetColumn(1);
        Vector3 forward = matrix.GetColumn(2);
        scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);

        if (scale.x > 0.0001f)
            right /= scale.x;
        if (scale.y > 0.0001f)
            up /= scale.y;
        if (scale.z > 0.0001f)
            forward /= scale.z;

        rotation = forward.sqrMagnitude > 0.0001f && up.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward, up)
            : Quaternion.identity;
    }

    private static List<int> SelectTriangleStarts(Vector3[] vertices, int[] triangles, float triangleRatio)
    {
        int triangleCount = triangles.Length / 3;
        int targetCount = Mathf.Clamp(Mathf.RoundToInt(triangleCount * triangleRatio), Mathf.Min(triangleCount, 24), triangleCount);
        if (targetCount >= triangleCount)
        {
            List<int> all = new(triangleCount);
            for (int triangle = 0; triangle < triangleCount; triangle++)
                all.Add(triangle * 3);
            return all;
        }

        // Preserve coverage across the whole mesh. Taking only the largest triangles creates
        // obvious holes in characters, which reads as invisible body parts from gameplay camera.
        List<int> selected = new(targetCount);
        for (int bucket = 0; bucket < targetCount; bucket++)
        {
            int startTriangle = Mathf.FloorToInt(bucket * triangleCount / (float)targetCount);
            int endTriangle = Mathf.FloorToInt((bucket + 1) * triangleCount / (float)targetCount);
            endTriangle = Mathf.Clamp(endTriangle, startTriangle + 1, triangleCount);

            int bestStart = startTriangle * 3;
            float bestArea = -1f;
            for (int triangle = startTriangle; triangle < endTriangle; triangle++)
            {
                int start = triangle * 3;
                Vector3 a = vertices[triangles[start]];
                Vector3 b = vertices[triangles[start + 1]];
                Vector3 c = vertices[triangles[start + 2]];
                float area = Vector3.Cross(b - a, c - a).sqrMagnitude;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestStart = start;
                }
            }

            selected.Add(bestStart);
        }

        selected.Sort();
        return selected;
    }

    private static int CopyVertex(
        int sourceIndex,
        Vector3[] sourceVertices,
        Vector3[] sourceNormals,
        Vector4[] sourceTangents,
        Color[] sourceColors,
        Color32[] sourceColors32,
        BoneWeight[] sourceBoneWeights,
        List<Vector4>[] sourceUvs,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<Color> colors,
        List<Color32> colors32,
        List<BoneWeight> boneWeights,
        List<Vector4>[] uvs,
        Dictionary<int, int> remap)
    {
        if (remap.TryGetValue(sourceIndex, out int mappedIndex))
            return mappedIndex;

        mappedIndex = vertices.Count;
        remap[sourceIndex] = mappedIndex;
        vertices.Add(sourceVertices[sourceIndex]);
        if (normals != null)
            normals.Add(sourceNormals[sourceIndex]);
        if (tangents != null)
            tangents.Add(sourceTangents[sourceIndex]);
        if (colors != null)
            colors.Add(sourceColors[sourceIndex]);
        if (colors32 != null)
            colors32.Add(sourceColors32[sourceIndex]);
        if (boneWeights != null)
            boneWeights.Add(sourceBoneWeights[sourceIndex]);
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel] != null)
                uvs[channel].Add(sourceUvs[channel][sourceIndex]);
        }

        return mappedIndex;
    }

    private static List<Vector4>[] ReadUvs(Mesh source)
    {
        List<Vector4>[] uvs = new List<Vector4>[8];
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            List<Vector4> channelUvs = new();
            source.GetUVs(channel, channelUvs);
            if (channelUvs.Count == source.vertexCount)
                uvs[channel] = channelUvs;
        }

        return uvs;
    }

    private static List<Vector4>[] CreateUvWriters(List<Vector4>[] sourceUvs)
    {
        List<Vector4>[] uvs = new List<Vector4>[sourceUvs.Length];
        for (int channel = 0; channel < sourceUvs.Length; channel++)
        {
            if (sourceUvs[channel] != null)
                uvs[channel] = new List<Vector4>();
        }

        return uvs;
    }

    private static void AssignMidLodToConfig(GameObject midPrefab)
    {
        UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(SoldierConfigPath);
        if (config == null || midPrefab == null)
        {
            Debug.LogError($"[MidLodGen] Cannot assign mid LOD config={SoldierConfigPath} prefab={OutputPrefabPath}");
            return;
        }

        SerializedObject serializedConfig = new(config);
        SerializedProperty midLodPrefab = serializedConfig.FindProperty("midLodPrefab");
        if (midLodPrefab == null)
        {
            Debug.LogError("[MidLodGen] UnitGridAuthoringConfig is missing serialized field midLodPrefab.");
            return;
        }

        midLodPrefab.objectReferenceValue = midPrefab;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void AssignLowLodToConfig(GameObject lowPrefab)
    {
        UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(SoldierConfigPath);
        if (config == null || lowPrefab == null)
        {
            Debug.LogError($"[MidLodGen] Cannot assign low LOD config={SoldierConfigPath} prefab={OutputLowPrefabPath}");
            return;
        }

        SerializedObject serializedConfig = new(config);
        SerializedProperty lowLodPrefab = serializedConfig.FindProperty("lowLodPrefab");
        if (lowLodPrefab == null)
        {
            Debug.LogError("[MidLodGen] UnitGridAuthoringConfig is missing serialized field lowLodPrefab.");
            return;
        }

        lowLodPrefab.objectReferenceValue = lowPrefab;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void AssignLodsToConfig(string configPath, GameObject midPrefab, GameObject lowPrefab, string unitName)
    {
        UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(configPath);
        if (config == null || midPrefab == null || lowPrefab == null)
        {
            Debug.LogError($"[VehicleLodGen] Cannot assign LODs unit={unitName} config={configPath}");
            return;
        }

        SerializedObject serializedConfig = new(config);
        SerializedProperty midLodPrefab = serializedConfig.FindProperty("midLodPrefab");
        SerializedProperty lowLodPrefab = serializedConfig.FindProperty("lowLodPrefab");
        if (midLodPrefab == null || lowLodPrefab == null)
        {
            Debug.LogError($"[VehicleLodGen] UnitGridAuthoringConfig is missing LOD fields unit={unitName} config={configPath}");
            return;
        }

        midLodPrefab.objectReferenceValue = midPrefab;
        lowLodPrefab.objectReferenceValue = lowPrefab;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void EnsureOutputFolder()
    {
        string fullPath = Path.Combine(Application.dataPath, "Game/Prefabs/Generated/MidLOD");
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
        AssetDatabase.Refresh();
    }

    private static int CountTriangles(Mesh mesh)
    {
        int total = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            total += (int)(mesh.GetIndexCount(i) / 3);
        return total;
    }

    private static MeshStats CountSourceStats(List<SourceRendererInfo> sources)
    {
        MeshStats stats = new MeshStats();
        for (int i = 0; i < sources.Count; i++)
            AddMeshStats(ref stats, sources[i].Mesh);
        return stats;
    }

    private static MeshStats CountMeshStats(List<Mesh> meshes)
    {
        MeshStats stats = new MeshStats();
        for (int i = 0; i < meshes.Count; i++)
            AddMeshStats(ref stats, meshes[i]);
        return stats;
    }

    private static MeshStats CountPrefabMeshStats(GameObject prefab)
    {
        MeshStats stats = new MeshStats();
        if (prefab == null)
            return stats;

        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
            AddMeshStats(ref stats, filters[i] != null ? filters[i].sharedMesh : null);

        return stats;
    }

    private static int CountPrefabMaterialSlots(GameObject prefab)
    {
        if (prefab == null)
            return 0;

        int slots = 0;
        MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : null;
            slots += materials != null ? materials.Length : 0;
        }

        return slots;
    }

    private static void AddMeshStats(ref MeshStats stats, Mesh mesh)
    {
        if (mesh == null)
            return;

        stats.Vertices += mesh.vertexCount;
        stats.SubMeshes += mesh.subMeshCount;
        stats.Triangles += CountTriangles(mesh);
    }

    private sealed class SourceRendererInfo
    {
        public readonly string Name;
        public readonly Mesh Mesh;
        public readonly MeshRenderer Renderer;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public SourceRendererInfo(string name, Mesh mesh, MeshRenderer renderer, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            Name = name;
            Mesh = mesh;
            Renderer = renderer;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }
    }

    private struct MeshStats
    {
        public int Vertices;
        public int Triangles;
        public int SubMeshes;
    }

    private readonly struct TriangleChoice
    {
        public readonly int Start;
        public readonly float Area;

        public TriangleChoice(int start, float area)
        {
            Start = start;
            Area = area;
        }
    }
}
