using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class UnitSoldierProxyLodGenerator
{
    private const string OutputFolder = "Assets/Game/Prefabs/Generated/MidLOD";
    private const string MeshPath = OutputFolder + "/ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset";
    private const string MaterialPath = OutputFolder + "/ProxySoldierMaterial.mat";
    private const string PrefabPath = OutputFolder + "/ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04.prefab";
    private const string ConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";

    public static void GenerateSoldierMale02Alt04Proxy()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Game/Prefabs/Generated", "MidLOD");
        }

        Mesh mesh = BuildSoldierProxyMesh();
        mesh.name = "ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04";
        AssetDatabase.DeleteAsset(MeshPath);
        AssetDatabase.CreateAsset(mesh, MeshPath);

        Material material = BuildProxyMaterial();
        AssetDatabase.DeleteAsset(MaterialPath);
        AssetDatabase.CreateAsset(material, MaterialPath);

        GameObject root = new("ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04");
        try
        {
            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            DisableSmallMeshCulling(renderer);
            root.AddComponent<UnitSafeVisibleCharacterLodAuthoring>();

            LODGroup lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[] { new LOD(0f, new[] { renderer }) });
            lodGroup.fadeMode = LODFadeMode.None;
            lodGroup.animateCrossFading = false;
            lodGroup.RecalculateBounds();
            lodGroup.size = 2.7f;
            lodGroup.localReferencePoint = new Vector3(0f, 1.35f, 0f);

            AssetDatabase.DeleteAsset(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssignProxyToConfig(prefab);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SoldierProxyLodGen] generated prefab={PrefabPath} verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}");
    }

    private static Mesh BuildSoldierProxyMesh()
    {
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> indices = new();

        AddBox(vertices, normals, uvs, indices, new Vector3(0f, 1.36f, 0f), new Vector3(0.40f, 0.66f, 0.26f)); // torso
        AddBox(vertices, normals, uvs, indices, new Vector3(0f, 1.88f, 0f), new Vector3(0.28f, 0.28f, 0.25f)); // head
        AddBox(vertices, normals, uvs, indices, new Vector3(-0.31f, 1.28f, 0f), new Vector3(0.15f, 0.58f, 0.14f)); // left arm
        AddBox(vertices, normals, uvs, indices, new Vector3(0.31f, 1.28f, 0f), new Vector3(0.15f, 0.58f, 0.14f)); // right arm
        AddBox(vertices, normals, uvs, indices, new Vector3(-0.12f, 0.66f, 0f), new Vector3(0.16f, 0.72f, 0.16f)); // left leg
        AddBox(vertices, normals, uvs, indices, new Vector3(0.12f, 0.66f, 0f), new Vector3(0.16f, 0.72f, 0.16f)); // right leg
        AddBox(vertices, normals, uvs, indices, new Vector3(0.40f, 1.32f, 0.25f), new Vector3(0.11f, 0.11f, 0.72f)); // rifle silhouette

        Mesh mesh = new();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material BuildProxyMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new(shader);
        material.name = "ProxySoldierMaterial";
        material.enableInstancing = true;
        material.color = new Color(0.42f, 0.56f, 0.34f, 1f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.42f, 0.56f, 0.34f, 1f));
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.15f);
        return material;
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

    private static void AddBox(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices,
        Vector3 center,
        Vector3 size)
    {
        Vector3 half = size * 0.5f;
        AddFace(vertices, normals, uvs, indices, center, new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z), new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z), Vector3.forward);
        AddFace(vertices, normals, uvs, indices, center, new Vector3(half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, half.y, -half.z), new Vector3(half.x, half.y, -half.z), Vector3.back);
        AddFace(vertices, normals, uvs, indices, center, new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, half.z), new Vector3(-half.x, half.y, half.z), new Vector3(-half.x, half.y, -half.z), Vector3.left);
        AddFace(vertices, normals, uvs, indices, center, new Vector3(half.x, -half.y, half.z), new Vector3(half.x, -half.y, -half.z), new Vector3(half.x, half.y, -half.z), new Vector3(half.x, half.y, half.z), Vector3.right);
        AddFace(vertices, normals, uvs, indices, center, new Vector3(-half.x, half.y, half.z), new Vector3(half.x, half.y, half.z), new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z), Vector3.up);
        AddFace(vertices, normals, uvs, indices, center, new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z), new Vector3(half.x, -half.y, half.z), new Vector3(-half.x, -half.y, half.z), Vector3.down);
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices,
        Vector3 center,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector3 normal)
    {
        int start = vertices.Count;
        vertices.Add(center + a);
        vertices.Add(center + b);
        vertices.Add(center + c);
        vertices.Add(center + d);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0f, 1f));
        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static void AssignProxyToConfig(GameObject prefab)
    {
        UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(ConfigPath);
        if (config == null || prefab == null)
        {
            Debug.LogError($"[SoldierProxyLodGen] Cannot assign config={ConfigPath} prefab={PrefabPath}");
            return;
        }

        SerializedObject serializedConfig = new(config);
        SerializedProperty midLodPrefab = serializedConfig.FindProperty("midLodPrefab");
        if (midLodPrefab == null)
        {
            Debug.LogError("[SoldierProxyLodGen] Missing UnitGridAuthoringConfig.midLodPrefab.");
            return;
        }

        midLodPrefab.objectReferenceValue = prefab;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }
}
