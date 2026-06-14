#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class PremiumWorldMarkerPrefabBuilder
{
    private static readonly System.Collections.Generic.HashSet<GameObject> LoadedPrefabRoots = new();
    private const string MeshDirectory = "Assets/Game/Rendering/Meshes/Selection";
    private const string MaterialDirectory = "Assets/Game/Rendering/Materials/Selection";
    private const string HologramShaderPath = "Assets/Game/Rendering/Shaders/SelectionHologram.shader";
    private const string BuildingSelectionPrefabPath = "Assets/Game/Prefabs/Buildings/BuildingSelectionMarker.prefab";
    private const string VehicleSelectionPrefabPath = "Assets/Game/Prefabs/Vehicles/VehicleSelectionMarker.prefab";
    private const string MoveMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/Target_Move.prefab";
    private const string AttackMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/Target_Attack.prefab";
    private const string AttackTargetMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/AttackTargetSelectionMarker.prefab";

    private static readonly Color SelectionColor = new(0.05f, 0.88f, 1f, 0.94f);
    private static readonly Color SelectionAccentColor = new(0.86f, 1f, 1f, 1f);
    private static readonly Color VehicleSelectionColor = new(0.02f, 0.88f, 1f, 0.92f);
    private static readonly Color VehicleSelectionAccentColor = new(0.56f, 0.98f, 1f, 0.92f);
    private static readonly Color MoveColor = new(0.03f, 0.78f, 1f, 0.94f);
    private static readonly Color MoveAccentColor = new(0.74f, 1f, 1f, 1f);
    private static readonly Color AttackColor = new(1f, 0.08f, 0.04f, 0.96f);
    private static readonly Color AttackAccentColor = new(1f, 0.82f, 0.42f, 1f);

    [MenuItem("Game/Markers/Rebuild Premium World Marker Prefabs")]
    public static void Run()
    {
        Directory.CreateDirectory(MeshDirectory);
        Directory.CreateDirectory(MaterialDirectory);

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(HologramShaderPath);
        if (shader == null)
            shader = Shader.Find("WarlineCapture/Markers/SelectionHologram");
        if (shader == null)
            throw new System.InvalidOperationException("Selection hologram shader is missing.");

        Material selectionMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Selection_Player_Hologram.mat",
            shader,
            SelectionColor,
            SelectionColor * 1.35f,
            SelectionAccentColor,
            alpha: 0.5f,
            pulse: 0.18f,
            scan: 0.22f,
            edgeSoftness: 0.025f);
        Material selectionFillMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Selection_Player_Fill_Hologram.mat",
            shader,
            new Color(0.05f, 0.88f, 1f, 0.18f),
            new Color(0.02f, 0.22f, 0.26f, 1f),
            new Color(0.5f, 0.95f, 1f, 1f),
            alpha: 0.18f,
            pulse: 0.08f,
            scan: 0.08f,
            edgeSoftness: 0.42f);
        Material vehicleMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Selection_Vehicle_Hologram.mat",
            shader,
            VehicleSelectionColor,
            new Color(0.01f, 0.22f, 0.28f, 1f),
            VehicleSelectionAccentColor,
            alpha: 0.82f,
            pulse: 0.1f,
            scan: 0.14f,
            edgeSoftness: 0.04f);
        Material vehicleFillMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Selection_Vehicle_Fill_Hologram.mat",
            shader,
            new Color(0.02f, 0.88f, 1f, 0.12f),
            new Color(0.01f, 0.08f, 0.1f, 1f),
            VehicleSelectionAccentColor,
            alpha: 0.12f,
            pulse: 0.03f,
            scan: 0.1f,
            edgeSoftness: 0.5f);
        Material moveMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Command_Move_Hologram.mat",
            shader,
            MoveColor,
            new Color(0.0375f, 0.975f, 1.25f, 1f),
            MoveAccentColor,
            alpha: 0.96f,
            pulse: 0.12f,
            scan: 0.04f,
            edgeSoftness: 0.025f);
        Material moveFillMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Command_Move_Fill_Hologram.mat",
            shader,
            new Color(0.03f, 0.78f, 1f, 0.2f),
            new Color(0.02f, 0.18f, 0.24f, 1f),
            MoveAccentColor,
            alpha: 0.2f,
            pulse: 0.04f,
            scan: 0.04f,
            edgeSoftness: 0.48f);
        Material attackMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Command_Attack_Hologram.mat",
            shader,
            AttackColor,
            new Color(0.4f, 0.035f, 0.02f, 1f),
            AttackAccentColor,
            alpha: 0.9f,
            pulse: 0.44f,
            scan: 0.42f,
            edgeSoftness: 0.025f);
        Material attackFillMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_Command_Attack_Fill_Hologram.mat",
            shader,
            new Color(1f, 0.12f, 0.035f, 0.13f),
            new Color(0.18f, 0.02f, 0.01f, 1f),
            AttackAccentColor,
            alpha: 0.13f,
            pulse: 0.08f,
            scan: 0.16f,
            edgeSoftness: 0.5f);
        Material targetLockMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_TargetLock_Attack_Hologram.mat",
            shader,
            AttackColor,
            AttackColor * 1.3f,
            AttackAccentColor,
            alpha: 0.94f,
            pulse: 0.58f,
            scan: 0.5f,
            edgeSoftness: 0.025f);
        Material targetLockFillMaterial = EnsureMaterial(
            $"{MaterialDirectory}/Mat_TargetLock_Attack_Fill_Hologram.mat",
            shader,
            new Color(1f, 0.08f, 0.04f, 0.16f),
            new Color(0.24f, 0.03f, 0.02f, 1f),
            AttackAccentColor,
            alpha: 0.16f,
            pulse: 0.1f,
            scan: 0.08f,
            edgeSoftness: 0.42f);

        Mesh rectFrame = SaveMesh("Premium_Rect_FootprintFrame", CreateRectFrameMesh(1f, 1f, 0.018f));
        Mesh rectFill = SaveMesh("Premium_Rect_FootprintFill", CreateRectFillMesh(1f, 1f));
        Mesh rectBrackets = SaveMesh("Premium_Rect_CornerBrackets", CreateRectCornerBracketMesh(1f, 1f, 0.22f, 0.026f, 0.34f));
        SaveMesh("Premium_Unit_CapsuleAura", CreateEllipseRingMesh(0.55f, 0.55f, 0.05f, 128, 360f));
        SaveMesh("Premium_Unit_OuterArcs", CreateEllipseRingMesh(0.62f, 0.62f, 0.02f, 128, 360f));
        Mesh vehicleFill = SaveMesh("Premium_Vehicle_FootprintFill", CreateRectFillMesh(1.18f, 0.76f));
        Mesh vehiclePlate = SaveMesh("Premium_Vehicle_FootprintPlate", CreateRectFrameMesh(1.18f, 0.76f, 0.026f));
        Mesh vehicleBrackets = SaveMesh("Premium_Vehicle_BoundsBrackets", CreateRectCornerBracketMesh(1.2f, 0.78f, 0.3f, 0.04f, 0.42f));
        Mesh moveFill = SaveMesh("Premium_Move_CleanDestinationFill", CreateEllipseFillMesh(0.72f, 0.5f, 128));
        Mesh moveOuterRing = SaveMesh("Premium_Move_CleanConnectedOuterRing", CreateEllipseRingMesh(0.72f, 0.5f, 0.055f, 192, 360f));
        Mesh moveInnerRing = SaveMesh("Premium_Move_CleanConnectedInnerRing", CreateEllipseRingMesh(0.36f, 0.25f, 0.028f, 160, 360f));
        Mesh moveCenterDot = SaveMesh("Premium_Move_CleanCenterDot", CreateEllipseFillMesh(0.085f, 0.085f, 48));
        Mesh attackFill = SaveMesh("Premium_Attack_StrikeScanFill", CreateEllipseFillMesh(0.78f, 0.54f, 96));
        Mesh attackCrosshair = SaveMesh("Premium_Attack_StrikeCrosshair", CreateSegmentedEllipseArcMesh(0.74f, 0.74f, 0.045f, 112));
        Mesh attackChevrons = SaveMesh("Premium_Attack_StrikeChevrons", CreateTargetChevronMesh(0.86f, 0.14f, 0.13f));
        Mesh attackBrackets = SaveMesh("Premium_Attack_HostileBrackets", CreateRectCornerBracketMesh(1.32f, 1.02f, 0.24f, 0.034f, 0.26f));
        Mesh attackBeacon = SaveMesh("Premium_Attack_LockBeacon", CreateBeaconPinMesh(0.72f, 0.024f));
        Mesh targetFrame = SaveMesh("Premium_TargetLock_BoundsFrame", CreateRectFrameMesh(1f, 1f, 0.034f));
        Mesh targetBrackets = SaveMesh("Premium_TargetLock_CornerBrackets", CreateRectCornerBracketMesh(1f, 1f, 0.24f, 0.046f, 0.38f));

        BuildBuildingSelectionPrefab(selectionMaterial, selectionFillMaterial, rectFrame, rectFill, rectBrackets);
        BuildVehicleSelectionPrefab(vehicleMaterial, vehicleFillMaterial, vehicleFill, vehiclePlate, vehicleBrackets);
        BuildMoveMarkerPrefab(moveMaterial, moveFillMaterial, moveFill, moveOuterRing, moveInnerRing, moveCenterDot);
        BuildAttackMarkerPrefab(attackMaterial, attackFillMaterial, attackFill, attackCrosshair, attackChevrons, attackBrackets, attackBeacon);
        BuildAttackTargetPrefab(targetLockMaterial, targetLockFillMaterial, targetFrame, rectFill, targetBrackets);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PremiumWorldMarkerPrefabBuilder] rebuilt premium world marker prefabs.");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void BuildBuildingSelectionPrefab(Material material, Material fillMaterial, Mesh frame, Mesh fill, Mesh brackets)
    {
        GameObject root = LoadOrCreatePrefabRoot(BuildingSelectionPrefabPath, "BuildingSelectionMarker");
        try
        {
            ClearChildren(root.transform);
            AddMeshChild(root.transform, "FootprintFill_Subtle", fill, fillMaterial, new Vector3(0f, 0.06f, 0f), Vector3.one * 0.96f, sortingOrder: -2);
            AddMeshChild(root.transform, "ExactFootprintFrame", frame, material, new Vector3(0f, 0.085f, 0f), Vector3.one, sortingOrder: 0);
            AddMeshChild(root.transform, "CornerLockBrackets", brackets, material, new Vector3(0f, 0.12f, 0f), Vector3.one, sortingOrder: 2);
            if (root.GetComponent<PremiumWorldSelectionBoundaryView>() == null)
                root.AddComponent<PremiumWorldSelectionBoundaryView>();
            if (root.GetComponent<PremiumWorldSelectionObjectOutlineView>() == null)
                root.AddComponent<PremiumWorldSelectionObjectOutlineView>();
            SavePrefabRoot(root, BuildingSelectionPrefabPath);
        }
        finally
        {
            UnloadPrefabRoot(root);
        }
    }

    private static void BuildVehicleSelectionPrefab(
        Material material,
        Material fillMaterial,
        Mesh vehicleFill,
        Mesh vehiclePlate,
        Mesh vehicleBrackets)
    {
        GameObject root = LoadOrCreatePrefabRoot(VehicleSelectionPrefabPath, "VehicleSelectionMarker");
        try
        {
            ClearChildren(root.transform);
            Transform model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localPosition = new Vector3(0f, 0.12f, 0f);
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;
            AddMeshChild(model, "VehicleFootprintFill", vehicleFill, fillMaterial, new Vector3(0f, 0.02f, 0f), Vector3.one, sortingOrder: -1);
            AddMeshChild(model, "VehicleBoundsFrame", vehiclePlate, material, new Vector3(0f, 0.04f, 0f), Vector3.one, sortingOrder: 2);
            AddMeshChild(model, "VehicleCornerBrackets", vehicleBrackets, material, new Vector3(0f, 0.075f, 0f), Vector3.one, sortingOrder: 3);
            SavePrefabRoot(root, VehicleSelectionPrefabPath);
        }
        finally
        {
            UnloadPrefabRoot(root);
        }
    }

    private static void BuildMoveMarkerPrefab(
        Material material,
        Material fillMaterial,
        Mesh fill,
        Mesh outerRing,
        Mesh innerRing,
        Mesh centerDot)
    {
        GameObject root = LoadOrCreatePrefabRoot(MoveMarkerPrefabPath, "Target_Move");
        try
        {
            ClearChildren(root.transform);
            AddMeshChild(root.transform, "WaypointConnectedFill", fill, fillMaterial, new Vector3(0f, 0.075f, 0f), Vector3.one, sortingOrder: -2);
            AddMeshChild(root.transform, "WaypointConnectedOuterRing", outerRing, material, new Vector3(0f, 0.115f, 0f), Vector3.one, sortingOrder: 0);
            AddMeshChild(root.transform, "WaypointConnectedInnerRing", innerRing, material, new Vector3(0f, 0.145f, 0f), Vector3.one, sortingOrder: 1);
            AddMeshChild(root.transform, "WaypointCenterDot", centerDot, material, new Vector3(0f, 0.17f, 0f), Vector3.one, sortingOrder: 2);
            SavePrefabRoot(root, MoveMarkerPrefabPath);
        }
        finally
        {
            UnloadPrefabRoot(root);
        }
    }

    private static void BuildAttackMarkerPrefab(
        Material material,
        Material fillMaterial,
        Mesh fill,
        Mesh crosshair,
        Mesh chevrons,
        Mesh brackets,
        Mesh beacon)
    {
        GameObject root = LoadOrCreatePrefabRoot(AttackMarkerPrefabPath, "Target_Attack");
        try
        {
            ClearChildren(root.transform);
            AddMeshChild(root.transform, "StrikeScanFill_Subtle", fill, fillMaterial, new Vector3(0f, 0.075f, 0f), Vector3.one, sortingOrder: -2);
            AddMeshChild(root.transform, "StrikeReticleArcs", crosshair, material, new Vector3(0f, 0.105f, 0f), Vector3.one, sortingOrder: 0);
            AddMeshChild(root.transform, "StrikeHostileBrackets", brackets, material, new Vector3(0f, 0.14f, 0f), Vector3.one, sortingOrder: 1);
            AddMeshChild(root.transform, "StrikeLockChevrons", chevrons, material, new Vector3(0f, 0.17f, 0f), Vector3.one, sortingOrder: 2);
            AddMeshChild(root.transform, "StrikeLockBeacon", beacon, material, new Vector3(0f, 0.13f, 0f), Vector3.one, sortingOrder: 3);
            SavePrefabRoot(root, AttackMarkerPrefabPath);
        }
        finally
        {
            UnloadPrefabRoot(root);
        }
    }

    private static void BuildAttackTargetPrefab(Material material, Material fillMaterial, Mesh frame, Mesh fill, Mesh brackets)
    {
        GameObject root = LoadOrCreatePrefabRoot(AttackTargetMarkerPrefabPath, "AttackTargetSelectionMarker");
        try
        {
            ClearChildren(root.transform);
            AddMeshChild(root.transform, "TargetLockFill_Subtle", fill, fillMaterial, new Vector3(0f, 0.075f, 0f), Vector3.one * 0.95f, sortingOrder: -2);
            AddMeshChild(root.transform, "TargetLockBoundsFrame", frame, material, new Vector3(0f, 0.12f, 0f), Vector3.one, sortingOrder: 0);
            AddMeshChild(root.transform, "TargetLockCornerBrackets", brackets, material, new Vector3(0f, 0.17f, 0f), Vector3.one, sortingOrder: 2);
            if (root.GetComponent<PremiumWorldSelectionBoundaryView>() == null)
                root.AddComponent<PremiumWorldSelectionBoundaryView>();
            SavePrefabRoot(root, AttackTargetMarkerPrefabPath);
        }
        finally
        {
            UnloadPrefabRoot(root);
        }
    }

    private static Material EnsureMaterial(
        string path,
        Shader shader,
        Color baseColor,
        Color emissionColor,
        Color accentColor,
        float alpha,
        float pulse,
        float scan,
        float edgeSoftness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.enableInstancing = true;
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetColor("_EmissionColor", emissionColor);
        material.SetColor("_AccentColor", accentColor);
        material.SetFloat("_Alpha", alpha);
        material.SetFloat("_PulseStrength", pulse);
        material.SetFloat("_PulseSpeed", 2.6f + pulse * 2.6f);
        material.SetFloat("_ScanStrength", scan);
        material.SetFloat("_ScanSpeed", 0.38f + scan * 1.15f);
        material.SetFloat("_EdgeSoftness", edgeSoftness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject LoadOrCreatePrefabRoot(string path, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            LoadedPrefabRoots.Add(root);
            return root;
        }

        return new GameObject(name);
    }

    private static void SavePrefabRoot(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
    }

    private static void UnloadPrefabRoot(GameObject root)
    {
        if (LoadedPrefabRoots.Remove(root))
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    private static GameObject AddMeshChild(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 localPosition,
        Vector3 localScale,
        int sortingOrder)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = localScale;
        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.sortingOrder = sortingOrder;
        DisableSmallMeshCulling(renderer);
        return child;
    }

    private static void DisableSmallMeshCulling(Renderer renderer)
    {
        if (renderer == null)
            return;

        SerializedObject serializedRenderer = new(renderer);
        SerializedProperty smallMeshCulling = serializedRenderer.FindProperty("m_SmallMeshCulling");
        if (smallMeshCulling == null)
            return;

        smallMeshCulling.intValue = 0;
        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Mesh SaveMesh(string name, Mesh mesh)
    {
        string path = $"{MeshDirectory}/{name}.asset";
        mesh.name = name;
        mesh.RecalculateBounds();

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, existing);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static Mesh CreateRectFillMesh(float width, float depth)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        Mesh mesh = new();
        mesh.vertices = new[]
        {
            new Vector3(-hx, 0f, -hz),
            new Vector3(-hx, 0f, hz),
            new Vector3(hx, 0f, hz),
            new Vector3(hx, 0f, -hz)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        return mesh;
    }

    private static Mesh CreateEllipseFillMesh(float radiusX, float radiusZ, int segments)
    {
        Mesh mesh = new();
        var vertices = new Vector3[segments + 1];
        var uvs = new Vector2[segments + 1];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float x = Mathf.Cos(angle) * radiusX;
            float z = Mathf.Sin(angle) * radiusZ;
            vertices[i + 1] = new Vector3(x, 0f, z);
            uvs[i + 1] = new Vector2(0.5f + x / (radiusX * 2f), 0.5f + z / (radiusZ * 2f));
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == segments - 1 ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        return mesh;
    }

    private static Mesh CreateWaypointGridMesh(float width, float depth, float lineWidth, int columns, int rows)
    {
        MeshBuilder builder = new();
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        AddGroundLine(builder, new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), lineWidth);
        AddGroundLine(builder, new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz), lineWidth);
        AddGroundLine(builder, new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz), lineWidth);
        AddGroundLine(builder, new Vector3(-hx, 0f, hz), new Vector3(-hx, 0f, -hz), lineWidth);

        for (int i = 1; i < columns; i++)
        {
            float x = Mathf.Lerp(-hx, hx, i / (float)columns);
            AddGroundLine(builder, new Vector3(x, 0f, -hz * 0.86f), new Vector3(x, 0f, hz * 0.86f), lineWidth * 0.55f);
        }

        for (int i = 1; i < rows; i++)
        {
            float z = Mathf.Lerp(-hz, hz, i / (float)rows);
            AddGroundLine(builder, new Vector3(-hx * 0.86f, 0f, z), new Vector3(hx * 0.86f, 0f, z), lineWidth * 0.55f);
        }

        return builder.ToMesh();
    }

    private static Mesh CreateRectFrameMesh(float width, float depth, float stripWidth)
    {
        MeshBuilder builder = new();
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        float halfStrip = stripWidth * 0.5f;
        builder.AddQuad(new Vector3(-hx, 0f, hz - halfStrip), new Vector3(hx, 0f, hz - halfStrip), new Vector3(hx, 0f, hz + halfStrip), new Vector3(-hx, 0f, hz + halfStrip));
        builder.AddQuad(new Vector3(-hx, 0f, -hz - halfStrip), new Vector3(-hx, 0f, -hz + halfStrip), new Vector3(hx, 0f, -hz + halfStrip), new Vector3(hx, 0f, -hz - halfStrip));
        builder.AddQuad(new Vector3(-hx - halfStrip, 0f, -hz), new Vector3(-hx + halfStrip, 0f, -hz), new Vector3(-hx + halfStrip, 0f, hz), new Vector3(-hx - halfStrip, 0f, hz));
        builder.AddQuad(new Vector3(hx - halfStrip, 0f, -hz), new Vector3(hx + halfStrip, 0f, -hz), new Vector3(hx + halfStrip, 0f, hz), new Vector3(hx - halfStrip, 0f, hz));
        return builder.ToMesh();
    }

    private static Mesh CreateRectCornerBracketMesh(float width, float depth, float length, float stripWidth, float postHeight)
    {
        MeshBuilder builder = new();
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        AddCorner(builder, hx, hz, -1f, -1f, length, stripWidth, postHeight);
        AddCorner(builder, hx, hz, 1f, -1f, length, stripWidth, postHeight);
        AddCorner(builder, hx, hz, -1f, 1f, length, stripWidth, postHeight);
        AddCorner(builder, hx, hz, 1f, 1f, length, stripWidth, postHeight);
        return builder.ToMesh();
    }

    private static void AddCorner(MeshBuilder builder, float hx, float hz, float sx, float sz, float length, float strip, float postHeight)
    {
        float x = hx * sx;
        float z = hz * sz;
        float half = strip * 0.5f;
        builder.AddQuad(new Vector3(x, 0f, z - half * sz), new Vector3(x - length * sx, 0f, z - half * sz), new Vector3(x - length * sx, 0f, z + half * sz), new Vector3(x, 0f, z + half * sz));
        builder.AddQuad(new Vector3(x - half * sx, 0f, z), new Vector3(x + half * sx, 0f, z), new Vector3(x + half * sx, 0f, z - length * sz), new Vector3(x - half * sx, 0f, z - length * sz));
        builder.AddQuad(new Vector3(x - half, 0f, z), new Vector3(x + half, 0f, z), new Vector3(x + half, postHeight, z), new Vector3(x - half, postHeight, z));
        builder.AddQuad(new Vector3(x, 0f, z - half), new Vector3(x, 0f, z + half), new Vector3(x, postHeight, z + half), new Vector3(x, postHeight, z - half));
    }

    private static Mesh CreateEllipseRingMesh(float radiusX, float radiusZ, float width, int segments, float degrees)
    {
        MeshBuilder builder = new();
        float start = -degrees * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.Deg2Rad * (start + degrees * i / segments);
            float a1 = Mathf.Deg2Rad * (start + degrees * (i + 1) / segments);
            AddRingSegment(builder, radiusX, radiusZ, width, a0, a1);
        }

        return builder.ToMesh();
    }

    private static Mesh CreateSegmentedEllipseArcMesh(float radiusX, float radiusZ, float width, int segments)
    {
        MeshBuilder builder = new();
        AddArc(builder, radiusX, radiusZ, width, segments, -38f, 38f);
        AddArc(builder, radiusX, radiusZ, width, segments, 142f, 218f);
        AddArc(builder, radiusX, radiusZ, width, segments, 62f, 118f);
        AddArc(builder, radiusX, radiusZ, width, segments, 242f, 298f);
        return builder.ToMesh();
    }

    private static Mesh CreateConcentricRingsMesh(float radiusA, float radiusB, float radiusC, float width, int segments)
    {
        MeshBuilder builder = new();
        AddArc(builder, radiusA, radiusA * 0.56f, width, segments, 0f, 360f);
        AddArc(builder, radiusB, radiusB * 0.56f, width * 0.72f, segments, 0f, 360f);
        AddArc(builder, radiusC, radiusC * 0.56f, width * 0.46f, segments, 205f, 335f);
        return builder.ToMesh();
    }

    private static Mesh CreateArrowMesh(float width, float length, float stemWidth)
    {
        MeshBuilder builder = new();
        float half = width * 0.5f;
        builder.AddTriangle(new Vector3(0f, 0f, length), new Vector3(-half, 0f, length * 0.28f), new Vector3(half, 0f, length * 0.28f));
        float stemHalf = stemWidth * 0.5f;
        builder.AddQuad(new Vector3(-stemHalf, 0f, -length * 0.32f), new Vector3(stemHalf, 0f, -length * 0.32f), new Vector3(stemHalf, 0f, length * 0.32f), new Vector3(-stemHalf, 0f, length * 0.32f));
        return builder.ToMesh();
    }

    private static Mesh CreateMoveChevronStackMesh(float forwardOffset, float length, float width, float spacing)
    {
        MeshBuilder builder = new();
        AddMoveChevron(builder, new Vector3(0f, 0f, forwardOffset), length, width);
        AddMoveChevron(builder, new Vector3(0f, 0f, forwardOffset - spacing), length * 0.82f, width * 0.8f);
        AddMoveChevron(builder, new Vector3(0f, 0f, -forwardOffset), length, width);
        AddMoveChevron(builder, new Vector3(0f, 0f, -forwardOffset + spacing), length * 0.82f, width * 0.8f, inverted: true);
        return builder.ToMesh();
    }

    private static Mesh CreateBeaconPinMesh(float height, float width)
    {
        MeshBuilder builder = new();
        float half = width * 0.5f;
        builder.AddQuad(
            new Vector3(-half, 0f, 0f),
            new Vector3(half, 0f, 0f),
            new Vector3(half, height, 0f),
            new Vector3(-half, height, 0f));
        builder.AddQuad(
            new Vector3(0f, 0f, -half),
            new Vector3(0f, 0f, half),
            new Vector3(0f, height, half),
            new Vector3(0f, height, -half));
        builder.AddTriangle(
            new Vector3(0f, height + width * 3f, 0f),
            new Vector3(-width * 2f, height - width * 1.5f, 0f),
            new Vector3(width * 2f, height - width * 1.5f, 0f));
        builder.AddTriangle(
            new Vector3(0f, height + width * 3f, 0f),
            new Vector3(0f, height - width * 1.5f, -width * 2f),
            new Vector3(0f, height - width * 1.5f, width * 2f));
        return builder.ToMesh();
    }

    private static Mesh CreateTargetChevronMesh(float radius, float length, float width)
    {
        MeshBuilder builder = new();
        AddChevron(builder, Vector3.forward * radius, Vector3.back, length, width);
        AddChevron(builder, Vector3.back * radius, Vector3.forward, length, width);
        AddChevron(builder, Vector3.right * radius, Vector3.left, length, width);
        AddChevron(builder, Vector3.left * radius, Vector3.right, length, width);
        return builder.ToMesh();
    }

    private static void AddChevron(MeshBuilder builder, Vector3 tip, Vector3 inward, float length, float width)
    {
        Vector3 side = Vector3.Cross(Vector3.up, inward).normalized;
        Vector3 baseCenter = tip + inward.normalized * length;
        builder.AddTriangle(tip, baseCenter + side * width, baseCenter - side * width);
    }

    private static void AddMoveChevron(MeshBuilder builder, Vector3 tip, float length, float width, bool inverted = false)
    {
        float direction = inverted ? -1f : 1f;
        Vector3 leftBase = tip + new Vector3(-length * 0.8f, 0f, -length * direction);
        Vector3 rightBase = tip + new Vector3(length * 0.8f, 0f, -length * direction);
        AddGroundLine(builder, tip, leftBase, width);
        AddGroundLine(builder, tip, rightBase, width);
    }

    private static void AddGroundLine(MeshBuilder builder, Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude <= 0.000001f)
            return;

        Vector3 side = Vector3.Cross(Vector3.up, direction.normalized) * (width * 0.5f);
        builder.AddQuad(start - side, end - side, end + side, start + side);
    }

    private static void AddArc(MeshBuilder builder, float radiusX, float radiusZ, float width, int segments, float startDegrees, float endDegrees)
    {
        int segmentCount = Mathf.Max(2, Mathf.RoundToInt(segments * Mathf.Abs(endDegrees - startDegrees) / 360f));
        for (int i = 0; i < segmentCount; i++)
        {
            float a0 = Mathf.Deg2Rad * Mathf.Lerp(startDegrees, endDegrees, i / (float)segmentCount);
            float a1 = Mathf.Deg2Rad * Mathf.Lerp(startDegrees, endDegrees, (i + 1) / (float)segmentCount);
            AddRingSegment(builder, radiusX, radiusZ, width, a0, a1);
        }
    }

    private static void AddRingSegment(MeshBuilder builder, float radiusX, float radiusZ, float width, float a0, float a1)
    {
        float innerX = Mathf.Max(0.001f, radiusX - width);
        float innerZ = Mathf.Max(0.001f, radiusZ - width);
        float outerX = radiusX + width;
        float outerZ = radiusZ + width;
        builder.AddQuad(
            new Vector3(Mathf.Cos(a0) * innerX, 0f, Mathf.Sin(a0) * innerZ),
            new Vector3(Mathf.Cos(a1) * innerX, 0f, Mathf.Sin(a1) * innerZ),
            new Vector3(Mathf.Cos(a1) * outerX, 0f, Mathf.Sin(a1) * outerZ),
            new Vector3(Mathf.Cos(a0) * outerX, 0f, Mathf.Sin(a0) * outerZ));
    }

    private sealed class MeshBuilder
    {
        private readonly System.Collections.Generic.List<Vector3> _vertices = new();
        private readonly System.Collections.Generic.List<Vector2> _uvs = new();
        private readonly System.Collections.Generic.List<int> _triangles = new();

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            _uvs.Add(new Vector2(0f, 0f));
            _uvs.Add(new Vector2(1f, 0f));
            _uvs.Add(new Vector2(1f, 1f));
            _uvs.Add(new Vector2(0f, 1f));
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 3);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _uvs.Add(new Vector2(0.5f, 1f));
            _uvs.Add(new Vector2(0f, 0f));
            _uvs.Add(new Vector2(1f, 0f));
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new();
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#endif
