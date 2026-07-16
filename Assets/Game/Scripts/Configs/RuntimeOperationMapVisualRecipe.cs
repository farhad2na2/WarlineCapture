using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum RuntimeOperationMapVisualStage
    {
        TerrainAndRoads = 0,
        DistrictModules = 1,
        Market = 2,
        Compound = 3,
        Aftermath = 4,
        Horizon = 5
    }

    public enum RuntimeOperationMapVisualEntryKind
    {
        Prefab = 0,
        Box = 1,
        Cylinder = 2,
        PointLight = 3,
        IrregularSurface = 4
    }

    [Serializable]
    public struct RuntimeOperationMapFoundationSettings
    {
        [SerializeField] private Material material;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 scale;
        [SerializeField] private Color color;

        public RuntimeOperationMapFoundationSettings(
            Material foundationMaterial,
            Vector3 foundationPosition,
            Vector3 foundationScale,
            Color foundationColor)
        {
            material = foundationMaterial;
            position = foundationPosition;
            scale = foundationScale;
            color = foundationColor;
        }

        public Material Material => material;
        public Vector3 Position => position;
        public Vector3 Scale => scale;
        public Color Color => color;
        public bool IsConfigured => material != null && scale.x > 0f && scale.y > 0f && scale.z > 0f;
    }

    [Serializable]
    public struct RuntimeOperationMapAlgorithmicDistrictSurfaceSettings
    {
        [SerializeField] private string surfaceName;
        [SerializeField] private Material material;
        [SerializeField] private Vector2 offsetInRoadCells;
        [SerializeField] private Vector2 sizeInRoadCells;
        [SerializeField] private Color color;
        [SerializeField] private uint seedOffset;

        public RuntimeOperationMapAlgorithmicDistrictSurfaceSettings(
            string name,
            Material surfaceMaterial,
            Vector2 roadCellOffset,
            Vector2 roadCellSize,
            Color surfaceColor,
            uint deterministicSeedOffset)
        {
            surfaceName = name;
            material = surfaceMaterial;
            offsetInRoadCells = roadCellOffset;
            sizeInRoadCells = roadCellSize;
            color = surfaceColor;
            seedOffset = deterministicSeedOffset;
        }

        public string SurfaceName => surfaceName;
        public Material Material => material;
        public Vector2 OffsetInRoadCells => offsetInRoadCells;
        public Vector2 SizeInRoadCells => sizeInRoadCells;
        public Color Color => color;
        public uint SeedOffset => seedOffset;
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(surfaceName) &&
            material != null &&
            sizeInRoadCells.x > 0f &&
            sizeInRoadCells.y > 0f;
    }

    [Serializable]
    public struct RuntimeOperationMapAlgorithmicAftermathSettings
    {
        [SerializeField] private string groupName;
        [SerializeField] private List<GameObject> dressingPrefabs;
        [SerializeField, Min(1)] private int maxAnchorGroups;
        [SerializeField, Min(1)] private int itemsPerGroup;
        [SerializeField, Min(0.5f)] private float minRadius;
        [SerializeField, Min(0.5f)] private float maxRadius;
        [SerializeField, Min(0.1f)] private float minScale;
        [SerializeField, Min(0.1f)] private float maxScale;
        [SerializeField] private Vector2 exposureDirection;
        [SerializeField, Range(1f, 360f)] private float exposureArcDegrees;
        [SerializeField] private Vector2 fallbackCenterOffsetInRoadCells;
        [SerializeField, Min(0f)] private float fallbackAnchorSpacingInRoadCells;
        [SerializeField, Min(0)] private int minimumAuthoredAnchorGroups;
        [SerializeField] private uint seedOffset;

        public RuntimeOperationMapAlgorithmicAftermathSettings(
            string name,
            List<GameObject> prefabs,
            int anchorGroupCount,
            int dressingItemsPerGroup,
            float minimumRadius,
            float maximumRadius,
            float minimumScale,
            float maximumScale,
            Vector2 preferredExposureDirection,
            float preferredExposureArcDegrees,
            uint deterministicSeedOffset,
            Vector2 fallbackCenterOffset = default,
            float fallbackSpacingInRoadCells = 0f,
            int minimumAuthoredAnchorGroupCount = 0)
        {
            groupName = name;
            dressingPrefabs = prefabs ?? new List<GameObject>();
            maxAnchorGroups = Mathf.Max(1, anchorGroupCount);
            itemsPerGroup = Mathf.Max(1, dressingItemsPerGroup);
            minRadius = Mathf.Max(0.5f, minimumRadius);
            maxRadius = Mathf.Max(minRadius, maximumRadius);
            minScale = Mathf.Max(0.1f, minimumScale);
            maxScale = Mathf.Max(minScale, maximumScale);
            exposureDirection = preferredExposureDirection;
            exposureArcDegrees = Mathf.Clamp(preferredExposureArcDegrees, 1f, 360f);
            fallbackCenterOffsetInRoadCells = fallbackCenterOffset;
            fallbackAnchorSpacingInRoadCells = Mathf.Max(0f, fallbackSpacingInRoadCells);
            minimumAuthoredAnchorGroups = Mathf.Clamp(minimumAuthoredAnchorGroupCount, 0, maxAnchorGroups);
            seedOffset = deterministicSeedOffset;
        }

        public string GroupName => groupName;
        public IReadOnlyList<GameObject> DressingPrefabs =>
            dressingPrefabs ?? (IReadOnlyList<GameObject>)Array.Empty<GameObject>();
        public int MaxAnchorGroups => Mathf.Max(1, maxAnchorGroups);
        public int ItemsPerGroup => Mathf.Max(1, itemsPerGroup);
        public float MinRadius => Mathf.Max(0.5f, minRadius);
        public float MaxRadius => Mathf.Max(MinRadius, maxRadius);
        public float MinScale => minScale > 0f ? Mathf.Max(0.1f, minScale) : 0.85f;
        public float MaxScale => maxScale > 0f ? Mathf.Max(MinScale, maxScale) : 1.16f;
        public Vector2 ExposureDirection =>
            exposureDirection.sqrMagnitude > 0.0001f ? exposureDirection.normalized : Vector2.zero;
        public float ExposureArcDegrees => ExposureDirection != Vector2.zero
            ? Mathf.Clamp(exposureArcDegrees > 0f ? exposureArcDegrees : 360f, 1f, 360f)
            : 360f;
        public uint SeedOffset => seedOffset;
        public Vector2 FallbackCenterOffsetInRoadCells => fallbackCenterOffsetInRoadCells;
        public float FallbackAnchorSpacingInRoadCells =>
            Mathf.Max(0f, fallbackAnchorSpacingInRoadCells);
        public int MinimumAuthoredAnchorGroups =>
            Mathf.Clamp(minimumAuthoredAnchorGroups, 0, MaxAnchorGroups);
        public int RequestedItemCount => MaxAnchorGroups * ItemsPerGroup;
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(groupName) &&
            DressingPrefabs.Count > 0 &&
            maxAnchorGroups > 0 &&
            itemsPerGroup > 0 &&
            maxRadius >= minRadius &&
            MaxScale >= MinScale;
    }

    [Serializable]
    public struct RuntimeOperationMapRevealSettings
    {
        [SerializeField, Min(0f)] private float terrainAndRoadsSeconds;
        [SerializeField, Min(0f)] private float districtModulesSeconds;
        [SerializeField, Min(0f)] private float marketSeconds;
        [SerializeField, Min(0f)] private float compoundSeconds;
        [SerializeField, Min(0f)] private float aftermathSeconds;
        [SerializeField, Min(0f)] private float horizonSeconds;

        public RuntimeOperationMapRevealSettings(
            float terrainSeconds,
            float districtSeconds,
            float marketRevealSeconds,
            float compoundRevealSeconds,
            float aftermathRevealSeconds,
            float horizonRevealSeconds)
        {
            terrainAndRoadsSeconds = Mathf.Max(0f, terrainSeconds);
            districtModulesSeconds = Mathf.Max(0f, districtSeconds);
            marketSeconds = Mathf.Max(0f, marketRevealSeconds);
            compoundSeconds = Mathf.Max(0f, compoundRevealSeconds);
            aftermathSeconds = Mathf.Max(0f, aftermathRevealSeconds);
            horizonSeconds = Mathf.Max(0f, horizonRevealSeconds);
        }

        public float GetMinimumDuration(RuntimeOperationMapVisualStage stage)
        {
            switch (stage)
            {
                case RuntimeOperationMapVisualStage.TerrainAndRoads:
                    return Mathf.Max(0f, terrainAndRoadsSeconds);
                case RuntimeOperationMapVisualStage.DistrictModules:
                    return Mathf.Max(0f, districtModulesSeconds);
                case RuntimeOperationMapVisualStage.Market:
                    return Mathf.Max(0f, marketSeconds);
                case RuntimeOperationMapVisualStage.Compound:
                    return Mathf.Max(0f, compoundSeconds);
                case RuntimeOperationMapVisualStage.Aftermath:
                    return Mathf.Max(0f, aftermathSeconds);
                case RuntimeOperationMapVisualStage.Horizon:
                    return Mathf.Max(0f, horizonSeconds);
                default:
                    return 0f;
            }
        }
    }

    [Serializable]
    public struct RuntimeOperationMapCameraPose
    {
        [SerializeField] private RuntimeOperationMapVisualStage stage;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 target;
        [SerializeField, Range(1f, 179f)] private float fieldOfView;
        [SerializeField, Min(0f)] private float transitionSeconds;

        public RuntimeOperationMapCameraPose(
            RuntimeOperationMapVisualStage visualStage,
            Vector3 cameraPosition,
            Vector3 lookTarget,
            float cameraFieldOfView,
            float transitionDurationSeconds)
        {
            stage = visualStage;
            position = cameraPosition;
            target = lookTarget;
            fieldOfView = Mathf.Clamp(cameraFieldOfView, 1f, 179f);
            transitionSeconds = Mathf.Max(0f, transitionDurationSeconds);
        }

        public RuntimeOperationMapVisualStage Stage => stage;
        public Vector3 Position => position;
        public Vector3 Target => target;
        public float FieldOfView => Mathf.Clamp(fieldOfView, 1f, 179f);
        public float TransitionSeconds => Mathf.Max(0f, transitionSeconds);
        public bool IsConfigured =>
            (target - position).sqrMagnitude > 0.01f &&
            fieldOfView >= 1f &&
            fieldOfView <= 179f;
    }

    [Serializable]
    public struct RuntimeOperationMapVisualCleanupSettings
    {
        [SerializeField] private bool clipDressingToWorldBounds;
        [SerializeField] private Vector2 worldCenter;
        [SerializeField] private Vector2 worldSize;

        public RuntimeOperationMapVisualCleanupSettings(Vector2 center, Vector2 size)
        {
            clipDressingToWorldBounds = size.x > 0f && size.y > 0f;
            worldCenter = center;
            worldSize = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
        }

        public bool IsConfigured => clipDressingToWorldBounds && worldSize.x > 0f && worldSize.y > 0f;

        public bool Contains(Vector3 worldPosition)
        {
            if (!IsConfigured)
                return true;

            Vector2 halfSize = worldSize * 0.5f;
            return Mathf.Abs(worldPosition.x - worldCenter.x) <= halfSize.x &&
                   Mathf.Abs(worldPosition.z - worldCenter.y) <= halfSize.y;
        }
    }

    [Serializable]
    public sealed class RuntimeOperationMapDistrictSliceRecipe
    {
        [SerializeField] private string name;
        [SerializeField] private int[] siblingIndices = Array.Empty<int>();
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation = Quaternion.identity;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private bool active = true;

        public RuntimeOperationMapDistrictSliceRecipe(
            string sliceName,
            int[] generatedSiblingIndices,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 worldScale,
            bool isActive)
        {
            name = sliceName ?? string.Empty;
            siblingIndices = generatedSiblingIndices ?? Array.Empty<int>();
            position = worldPosition;
            rotation = worldRotation;
            scale = worldScale;
            active = isActive;
        }

        public string Name => name;
        public IReadOnlyList<int> SiblingIndices =>
            siblingIndices ?? (IReadOnlyList<int>)Array.Empty<int>();
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Vector3 Scale => scale;
        public bool Active => active;
        public bool IsConfigured => SiblingIndices.Count > 0;
    }

    [Serializable]
    public sealed class RuntimeOperationMapDistrictModuleRecipe
    {
        [SerializeField] private string name;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private Vector3 scale;
        [SerializeField] private bool active;
        [SerializeField] private bool realizeCompletePrefab;
        [SerializeField] private RuntimeOperationMapVisualCleanupSettings cleanup;
        [SerializeField] private List<RuntimeOperationMapDistrictSliceRecipe> slices = new();

        public RuntimeOperationMapDistrictModuleRecipe(
            string moduleName,
            GameObject modulePrefab,
            Vector3 modulePosition,
            Quaternion moduleRotation,
            Vector3 moduleScale,
            bool moduleActive,
            RuntimeOperationMapVisualCleanupSettings cleanupSettings,
            List<RuntimeOperationMapDistrictSliceRecipe> generatedSlices,
            bool realizeAsCompletePrefab = false)
        {
            name = moduleName ?? string.Empty;
            prefab = modulePrefab;
            position = modulePosition;
            rotation = moduleRotation;
            scale = moduleScale;
            active = moduleActive;
            realizeCompletePrefab = realizeAsCompletePrefab;
            cleanup = cleanupSettings;
            slices = generatedSlices ?? new List<RuntimeOperationMapDistrictSliceRecipe>();
        }

        public string Name => name;
        public GameObject Prefab => prefab;
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Vector3 Scale => scale;
        public bool Active => active;
        public bool RealizeCompletePrefab => realizeCompletePrefab;
        public RuntimeOperationMapVisualCleanupSettings Cleanup => cleanup;
        public IReadOnlyList<RuntimeOperationMapDistrictSliceRecipe> Slices =>
            slices ?? (IReadOnlyList<RuntimeOperationMapDistrictSliceRecipe>)Array.Empty<RuntimeOperationMapDistrictSliceRecipe>();
        public bool IsConfigured =>
            prefab != null &&
            (realizeCompletePrefab || Slices.Count > 0);
    }

    [Serializable]
    public struct RuntimeOperationMapVisualEntry
    {
        [SerializeField] private string name;
        [SerializeField] private RuntimeOperationMapVisualStage stage;
        [SerializeField] private RuntimeOperationMapVisualEntryKind kind;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Material material;
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private Vector3 scale;
        [SerializeField] private bool active;
        [SerializeField] private bool allowParticles;
        [SerializeField] private Color lightColor;
        [SerializeField] private float lightIntensity;
        [SerializeField] private float lightRange;
        [SerializeField] private LightShadows lightShadows;
        [SerializeField] private RuntimeOperationMapVisualCleanupSettings cleanup;

        public RuntimeOperationMapVisualEntry(
            string name,
            RuntimeOperationMapVisualStage stage,
            RuntimeOperationMapVisualEntryKind kind,
            GameObject prefab,
            Material material,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            bool active,
            bool allowParticles,
            Color lightColor = default,
            float lightIntensity = 0f,
            float lightRange = 0f,
            LightShadows lightShadows = LightShadows.None,
            RuntimeOperationMapVisualCleanupSettings cleanupSettings = default)
        {
            this.name = name;
            this.stage = stage;
            this.kind = kind;
            this.prefab = prefab;
            this.material = material;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.active = active;
            this.allowParticles = allowParticles;
            this.lightColor = lightColor;
            this.lightIntensity = lightIntensity;
            this.lightRange = lightRange;
            this.lightShadows = lightShadows;
            cleanup = cleanupSettings;
        }

        public string Name => name;
        public RuntimeOperationMapVisualStage Stage => stage;
        public RuntimeOperationMapVisualEntryKind Kind => kind;
        public GameObject Prefab => prefab;
        public Material Material => material;
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Vector3 Scale => scale;
        public bool Active => active;
        public bool AllowParticles => allowParticles;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity;
        public float LightRange => lightRange;
        public LightShadows LightShadows => lightShadows;
        public RuntimeOperationMapVisualCleanupSettings Cleanup => cleanup;
    }

    [CreateAssetMenu(menuName = "Game/Map Prototypes/Runtime Operation Map Visual Recipe")]
    public sealed class RuntimeOperationMapVisualRecipe : ScriptableObject
    {
        [SerializeField] private string recipeVersion;
        [SerializeField] private uint seed;
        [SerializeField] private RuntimeOperationMapFoundationSettings foundation;
        [SerializeField] private RuntimeOperationMapRevealSettings reveal;
        [SerializeField] private List<RuntimeOperationMapCameraPose> cameraPoses = new();
        [SerializeField] private List<RuntimeOperationMapDistrictModuleRecipe> districtModules = new();
        [SerializeField] private List<RuntimeOperationMapVisualEntry> entries = new();

        public string RecipeVersion => recipeVersion;
        public uint Seed => seed;
        public RuntimeOperationMapFoundationSettings Foundation => foundation;
        public RuntimeOperationMapRevealSettings Reveal => reveal;
        public IReadOnlyList<RuntimeOperationMapCameraPose> CameraPoses =>
            cameraPoses ?? (IReadOnlyList<RuntimeOperationMapCameraPose>)Array.Empty<RuntimeOperationMapCameraPose>();
        public IReadOnlyList<RuntimeOperationMapDistrictModuleRecipe> DistrictModules =>
            districtModules ?? (IReadOnlyList<RuntimeOperationMapDistrictModuleRecipe>)Array.Empty<RuntimeOperationMapDistrictModuleRecipe>();
        public IReadOnlyList<RuntimeOperationMapVisualEntry> Entries => entries;

        public void ReplaceGeneratedEntries(
            string version,
            uint generationSeed,
            RuntimeOperationMapFoundationSettings foundationSettings,
            RuntimeOperationMapRevealSettings revealSettings,
            List<RuntimeOperationMapCameraPose> generatedCameraPoses,
            List<RuntimeOperationMapDistrictModuleRecipe> generatedDistrictModules,
            List<RuntimeOperationMapVisualEntry> generatedEntries)
        {
            recipeVersion = version ?? string.Empty;
            seed = generationSeed;
            foundation = foundationSettings;
            reveal = revealSettings;
            cameraPoses = generatedCameraPoses ?? new List<RuntimeOperationMapCameraPose>();
            districtModules = generatedDistrictModules ?? new List<RuntimeOperationMapDistrictModuleRecipe>();
            entries = generatedEntries ?? new List<RuntimeOperationMapVisualEntry>();
        }
    }
}
