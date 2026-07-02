using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;
using Game.Configs;

namespace Game.Rendering
{
    public static class SharedPrefabPreviewCache
    {
        private const int PreviewVersion = 7;
        private static readonly Vector3 DefaultCharacterPreviewModelPosition = new(-2f, 0f, 0f);
        private static readonly Quaternion DefaultCharacterPreviewModelRotation = Quaternion.identity;
        private static readonly Vector3 DefaultCharacterPreviewCameraPosition = new(-2.019521f, 1.569489f, 0.6451559f);
        private static readonly Quaternion DefaultCharacterPreviewCameraRotation = Quaternion.Euler(-5.722f, 178.278f, 0f);
        private static readonly Vector3 DefaultVehiclePreviewModelPosition = Vector3.zero;
        private static readonly Quaternion DefaultVehiclePreviewModelRotation = Quaternion.identity;
        private static readonly Vector3 DefaultVehiclePreviewCameraPosition = new(0f, 2f, 6f);
        private static readonly Quaternion DefaultVehiclePreviewCameraRotation = Quaternion.Euler(10f, 180f, 0f);
        private static readonly Vector3 DefaultBuildingPreviewModelPosition = Vector3.zero;
        private static readonly Quaternion DefaultBuildingPreviewModelRotation = Quaternion.identity;
        private static readonly Vector3 DefaultBuildingPreviewCameraPosition = new(0f, 4f, 10f);
        private static readonly Quaternion DefaultBuildingPreviewCameraRotation = Quaternion.Euler(18f, 145f, 0f);
        private readonly struct PreviewFraming
        {
            public readonly Vector3 CameraPosition;
            public readonly Quaternion Rotation;

            public PreviewFraming(Vector3 cameraPosition, Quaternion rotation)
            {
                CameraPosition = cameraPosition;
                Rotation = rotation;
            }
        }

        private readonly struct PreviewKey
        {
            public readonly GameObject Prefab;
            public readonly int DistanceMilli;
            public readonly int Version;
            public readonly byte Mode;
            public readonly byte DirectionIndex;
            public readonly byte DirectionCount;

            public PreviewKey(GameObject prefab, float distanceMultiplier, bool impostorMode, int directionIndex, int directionCount)
            {
                Prefab = prefab;
                DistanceMilli = Mathf.RoundToInt(Mathf.Max(0.1f, distanceMultiplier) * 1000f);
                Version = PreviewVersion;
                Mode = impostorMode ? (byte)1 : (byte)0;
                DirectionIndex = (byte)Mathf.Clamp(directionIndex, 0, 255);
                DirectionCount = (byte)Mathf.Clamp(directionCount, 0, 255);
            }
        }

        private sealed class PreviewKeyComparer : IEqualityComparer<PreviewKey>
        {
            public bool Equals(PreviewKey x, PreviewKey y) => x.Prefab == y.Prefab && x.DistanceMilli == y.DistanceMilli && x.Version == y.Version && x.Mode == y.Mode && x.DirectionIndex == y.DirectionIndex && x.DirectionCount == y.DirectionCount;
            public int GetHashCode(PreviewKey obj) => ((((((obj.Prefab != null ? obj.Prefab.GetHashCode() : 0) * 397) ^ obj.DistanceMilli) * 397 ^ obj.Version) * 397 ^ obj.Mode) * 397 ^ obj.DirectionIndex) * 397 ^ obj.DirectionCount;
        }

        private const int PreviewLayer = 30;
        private static readonly int SnivelerModelShownId = Shader.PropertyToID("_SnivelerModelShown");
        private static readonly int SnivelerRenderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");
        private static readonly Dictionary<PreviewKey, RenderTexture> Cache = new(new PreviewKeyComparer());
        private static GameObject _previewRoot;
        private static Camera _previewCamera;
        private static GameObject _previewCameraObject;
        private static PrefabPreviewCameraConfig _previewConfig;
        private static int _revision;
        private static Vector3 _characterPreviewModelPosition = DefaultCharacterPreviewModelPosition;
        private static Quaternion _characterPreviewModelRotation = DefaultCharacterPreviewModelRotation;
        private static Vector3 _characterPreviewCameraPosition = DefaultCharacterPreviewCameraPosition;
        private static Quaternion _characterPreviewCameraRotation = DefaultCharacterPreviewCameraRotation;
        private static Vector3 _vehiclePreviewModelPosition = DefaultVehiclePreviewModelPosition;
        private static Quaternion _vehiclePreviewModelRotation = DefaultVehiclePreviewModelRotation;
        private static Vector3 _vehiclePreviewCameraPosition = DefaultVehiclePreviewCameraPosition;
        private static Quaternion _vehiclePreviewCameraRotation = DefaultVehiclePreviewCameraRotation;
        private static Vector3 _buildingPreviewModelPosition = DefaultBuildingPreviewModelPosition;
        private static Quaternion _buildingPreviewModelRotation = DefaultBuildingPreviewModelRotation;
        private static Vector3 _buildingPreviewCameraPosition = DefaultBuildingPreviewCameraPosition;
        private static Quaternion _buildingPreviewCameraRotation = DefaultBuildingPreviewCameraRotation;
        private static TryGetUnitRenderingMetadataDelegate _tryGetUnitRenderingMetadata;
        public static int Revision => _revision;

        public static void RefreshConfig()
        {
            if (_previewConfig != null)
                ApplyPreviewConfig(_previewConfig);
        }

        public static void Init(PrefabPreviewCameraConfig config)
        {
            _previewConfig = config;
            ApplyPreviewConfig(config);
        }

        public static void ConfigureUnitRenderingMetadataResolver(TryGetUnitRenderingMetadataDelegate tryGetUnitRenderingMetadata)
        {
            _tryGetUnitRenderingMetadata = tryGetUnitRenderingMetadata;
        }

        public static bool TryGetOrCreate(GameObject prefab, float distanceMultiplier, out RenderTexture texture)
        {
            return TryGetOrCreateInternal(prefab, distanceMultiplier, false, 0, 1, out texture);
        }

        public static bool TryGetOrCreateImpostor(GameObject prefab, out RenderTexture texture)
        {
            return TryGetOrCreateInternal(prefab, 1f, true, 0, 1, out texture);
        }

        public static bool TryGetOrCreateDirectionalImpostor(GameObject prefab, int directionIndex, int directionCount, out RenderTexture texture)
        {
            return TryGetOrCreateInternal(prefab, 1f, true, directionIndex, Mathf.Max(1, directionCount), out texture);
        }

        private static bool TryGetOrCreateInternal(GameObject prefab, float distanceMultiplier, bool impostorMode, int directionIndex, int directionCount, out RenderTexture texture)
        {
            texture = null;
            if (prefab == null)
                return false;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return false;

            RefreshConfig();

            PreviewKey key = new(prefab, distanceMultiplier, impostorMode, directionIndex, directionCount);
            if (Cache.TryGetValue(key, out texture) && texture != null)
                return true;

            EnsureResources();
            if (_previewRoot == null || _previewCamera == null)
                return false;

            if (!TryCreatePreviewTexture(prefab, distanceMultiplier, impostorMode, directionIndex, directionCount, out texture))
                return false;

            Cache[key] = texture;
            return true;
        }

        private static void ApplyPreviewConfig(PrefabPreviewCameraConfig config)
        {
            Vector3 nextModelPosition = DefaultCharacterPreviewModelPosition;
            Quaternion nextModelRotation = DefaultCharacterPreviewModelRotation;
            Vector3 nextCameraPosition = DefaultCharacterPreviewCameraPosition;
            Quaternion nextCameraRotation = DefaultCharacterPreviewCameraRotation;
            Vector3 nextVehicleModelPosition = DefaultVehiclePreviewModelPosition;
            Quaternion nextVehicleModelRotation = DefaultVehiclePreviewModelRotation;
            Vector3 nextVehicleCameraPosition = DefaultVehiclePreviewCameraPosition;
            Quaternion nextVehicleCameraRotation = DefaultVehiclePreviewCameraRotation;
            Vector3 nextBuildingModelPosition = DefaultBuildingPreviewModelPosition;
            Quaternion nextBuildingModelRotation = DefaultBuildingPreviewModelRotation;
            Vector3 nextBuildingCameraPosition = DefaultBuildingPreviewCameraPosition;
            Quaternion nextBuildingCameraRotation = DefaultBuildingPreviewCameraRotation;
            if (config != null)
            {
                nextModelPosition = config.CharacterModelPosition;
                nextModelRotation = config.CharacterModelRotation;
                nextCameraPosition = config.CharacterCameraPosition;
                nextCameraRotation = config.CharacterCameraRotation;
                nextVehicleModelPosition = config.VehicleModelPosition;
                nextVehicleModelRotation = config.VehicleModelRotation;
                nextVehicleCameraPosition = config.VehicleCameraPosition;
                nextVehicleCameraRotation = config.VehicleCameraRotation;
                nextBuildingModelPosition = config.BuildingModelPosition;
                nextBuildingModelRotation = config.BuildingModelRotation;
                nextBuildingCameraPosition = config.BuildingCameraPosition;
                nextBuildingCameraRotation = config.BuildingCameraRotation;
            }

            if (_characterPreviewModelPosition == nextModelPosition &&
                Quaternion.Dot(_characterPreviewModelRotation, nextModelRotation) > 0.999999f &&
                _characterPreviewCameraPosition == nextCameraPosition &&
                Quaternion.Dot(_characterPreviewCameraRotation, nextCameraRotation) > 0.999999f &&
                _vehiclePreviewModelPosition == nextVehicleModelPosition &&
                Quaternion.Dot(_vehiclePreviewModelRotation, nextVehicleModelRotation) > 0.999999f &&
                _vehiclePreviewCameraPosition == nextVehicleCameraPosition &&
                Quaternion.Dot(_vehiclePreviewCameraRotation, nextVehicleCameraRotation) > 0.999999f &&
                _buildingPreviewModelPosition == nextBuildingModelPosition &&
                Quaternion.Dot(_buildingPreviewModelRotation, nextBuildingModelRotation) > 0.999999f &&
                _buildingPreviewCameraPosition == nextBuildingCameraPosition &&
                Quaternion.Dot(_buildingPreviewCameraRotation, nextBuildingCameraRotation) > 0.999999f)
                return;

            _characterPreviewModelPosition = nextModelPosition;
            _characterPreviewModelRotation = nextModelRotation;
            _characterPreviewCameraPosition = nextCameraPosition;
            _characterPreviewCameraRotation = nextCameraRotation;
            _vehiclePreviewModelPosition = nextVehicleModelPosition;
            _vehiclePreviewModelRotation = nextVehicleModelRotation;
            _vehiclePreviewCameraPosition = nextVehicleCameraPosition;
            _vehiclePreviewCameraRotation = nextVehicleCameraRotation;
            _buildingPreviewModelPosition = nextBuildingModelPosition;
            _buildingPreviewModelRotation = nextBuildingModelRotation;
            _buildingPreviewCameraPosition = nextBuildingCameraPosition;
            _buildingPreviewCameraRotation = nextBuildingCameraRotation;
            _revision++;
            ReleaseAll();
        }

        public static void ReleaseAll()
        {
            foreach (RenderTexture texture in Cache.Values)
            {
                if (texture == null)
                    continue;

                texture.Release();
                DestroyPreviewObject(texture);
            }

            Cache.Clear();

            if (_previewCameraObject != null)
            {
                DestroyPreviewObject(_previewCameraObject);
                _previewCameraObject = null;
                _previewCamera = null;
            }

            if (_previewRoot != null)
            {
                DestroyPreviewObject(_previewRoot);
                _previewRoot = null;
            }
        }

        private static void DestroyPreviewObject(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private static void EnsureResources()
        {
            if (_previewRoot == null)
            {
                _previewRoot = new GameObject("SharedPrefabPreviewRoot");
                _previewRoot.hideFlags = HideFlags.HideAndDontSave;
                _previewRoot.transform.position = new Vector3(0f, -10000f, 0f);
            }

            if (_previewCameraObject == null)
            {
                _previewCameraObject = new GameObject("SharedPrefabPreviewCamera");
                _previewCameraObject.hideFlags = HideFlags.HideAndDontSave;
                _previewCameraObject.transform.SetParent(_previewRoot.transform, false);
                _previewCamera = _previewCameraObject.AddComponent<Camera>();
                _previewCamera.enabled = false;
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _previewCamera.cullingMask = 1 << PreviewLayer;
                _previewCamera.nearClipPlane = 0.01f;
                _previewCamera.farClipPlane = 200f;
                _previewCamera.allowHDR = false;
                _previewCamera.allowMSAA = true;
                _previewCamera.orthographic = false;
                _previewCamera.fieldOfView = 24f;
            }
        }

        private static bool TryCreatePreviewTexture(GameObject prefab, float distanceMultiplier, bool impostorMode, int directionIndex, int directionCount, out RenderTexture texture)
        {
            texture = null;

            GameObject sourceInstance = Instantiate(prefab, _previewRoot.transform);
            sourceInstance.hideFlags = HideFlags.HideAndDontSave;
            sourceInstance.transform.localPosition = Vector3.zero;
            sourceInstance.transform.localRotation = Quaternion.identity;
            sourceInstance.transform.localScale = Vector3.one;

            GameObject previewInstance = null;

            try
            {
                Transform sourceModelRoot = FindPreviewModelRoot(sourceInstance.transform);
                GameObject previewSource = sourceModelRoot != null ? sourceModelRoot.gameObject : sourceInstance;
                Transform previewStopAncestor = sourceModelRoot != null ? null : sourceInstance.transform;

                previewInstance = Instantiate(previewSource, _previewRoot.transform);
                previewInstance.hideFlags = HideFlags.HideAndDontSave;
                previewInstance.name = $"{prefab.name}_PreviewModel";
                previewInstance.transform.position = Vector3.zero;
                previewInstance.transform.rotation = Quaternion.identity;
                previewInstance.transform.localScale = Vector3.one;

                ApplyFixedPreviewPose(prefab, previewInstance.transform);
                if (impostorMode)
                    ApplyImpostorPreviewPose(prefab, previewInstance, directionIndex, directionCount);

                SetLayerRecursively(previewInstance.transform, PreviewLayer);
                Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = default;
                bool hasBounds = false;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    bool keepRenderer = renderer != null && !HasBlockedPreviewAncestor(renderer.transform, previewStopAncestor);
                    renderer.enabled = keepRenderer;
                    if (!keepRenderer)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (hasBounds && IsTentBuildingPreview(prefab))
                {
                    if (TryGetDominantTentBounds(previewInstance.transform, out Bounds dominantTentBounds))
                    {
                        RecenterPreviewRootToBounds(previewInstance.transform, dominantTentBounds);
                        if (TryCollectEnabledRendererBounds(previewInstance.transform, out Bounds recenteredBounds))
                            bounds = recenteredBounds;
                        else
                            bounds = dominantTentBounds;
                    }
                }

                if (!hasBounds)
                    return false;

                texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 4
                };

                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                PreviewFraming framing = BuildPreviewFraming(prefab, previewInstance.transform, bounds, size, distanceMultiplier, impostorMode);
                _previewCamera.transform.position = framing.CameraPosition;
                _previewCamera.transform.rotation = framing.Rotation;
                _previewCamera.targetTexture = texture;
                _previewCamera.Render();
                _previewCamera.targetTexture = null;
                return true;
            }
            finally
            {
                if (previewInstance != null)
                    DestroyImmediate(previewInstance);
                DestroyImmediate(sourceInstance);
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private static Transform FindPreviewModelRoot(Transform root)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (HasBlockedPreviewAncestor(child, root))
                    continue;

                if (child.name == "Model")
                    return child;

                Transform descendant = FindPreviewModelRoot(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private static bool HasBlockedPreviewAncestor(Transform transform, Transform stopAncestor)
        {
            Transform current = transform;
            while (current != null)
            {
                string name = current.name;
                if (name == "SelectionMarker" || name == "Destroyed")
                    return true;

                if (current == stopAncestor)
                    break;

                current = current.parent;
            }

            return false;
        }

        private static PreviewFraming BuildPreviewFraming(GameObject prefab, Transform previewRoot, Bounds bounds, float size, float distanceMultiplier, bool impostorMode)
        {
            if (impostorMode)
                return BuildImpostorFraming(prefab, bounds);

            float clampedDistanceMultiplier = Mathf.Max(0.1f, distanceMultiplier);
            if (IsCharacterPreview(prefab))
                return new PreviewFraming(_characterPreviewCameraPosition, _characterPreviewCameraRotation);
            if (IsVehiclePreview(prefab))
                return new PreviewFraming(FitCameraPositionToBounds(bounds, _vehiclePreviewCameraPosition, _vehiclePreviewCameraRotation, clampedDistanceMultiplier), _vehiclePreviewCameraRotation);
            if (IsBuildingPreview(prefab))
                return new PreviewFraming(FitCameraPositionToBounds(bounds, _vehiclePreviewCameraPosition, _vehiclePreviewCameraRotation, clampedDistanceMultiplier), _vehiclePreviewCameraRotation);

            Quaternion rotation = Quaternion.Euler(18f, 145f, 0f);
            Vector3 focus = bounds.center + Vector3.up * (size * 0.06f);
            float distance = Mathf.Max(3.2f, size * 2.9f) * clampedDistanceMultiplier;
            return new PreviewFraming(focus - (rotation * Vector3.forward * distance), rotation);
        }

        private static PreviewFraming BuildImpostorFraming(GameObject prefab, Bounds bounds)
        {
            if (IsCharacterPreview(prefab))
            {
                Quaternion characterRotation = Quaternion.Euler(12f, 180f, 0f);
                Vector3 characterFocus = bounds.center + Vector3.up * (bounds.size.y * 0.02f);
                float height = Mathf.Max(1.6f, bounds.size.y);
                float width = Mathf.Max(bounds.size.x, bounds.size.z);
                float characterDistance = Mathf.Max(5f, Mathf.Max(height * 2.4f, width * 3.2f));
                return new PreviewFraming(characterFocus - (characterRotation * Vector3.forward * characterDistance), characterRotation);
            }
            if (IsVehiclePreview(prefab))
                return new PreviewFraming(FitCameraPositionToBounds(bounds, _vehiclePreviewCameraPosition, _vehiclePreviewCameraRotation, 1.05f), _vehiclePreviewCameraRotation);
            if (IsBuildingPreview(prefab))
                return new PreviewFraming(FitCameraPositionToBounds(bounds, _buildingPreviewCameraPosition, _buildingPreviewCameraRotation, 1.05f), _buildingPreviewCameraRotation);

            Quaternion rotation = Quaternion.Euler(14f, 165f, 0f);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 focus = bounds.center + Vector3.up * (size * 0.08f);
            float distance = Mathf.Max(3.6f, size * 2.4f);
            return new PreviewFraming(focus - (rotation * Vector3.forward * distance), rotation);
        }

        private static void ApplyImpostorPreviewPose(GameObject prefab, GameObject previewInstance, int directionIndex, int directionCount)
        {
            if (prefab == null || previewInstance == null || !IsCharacterPreview(prefab))
                return;

            float angle = directionCount > 1 ? 360f * Mathf.Clamp(directionIndex, 0, directionCount - 1) / directionCount : 0f;
            previewInstance.transform.Rotate(0f, 180f + angle, 0f, Space.World);

            MaterialAnimatorIndexAuthoring indexAuthoring = previewInstance.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
            if (indexAuthoring == null || indexAuthoring.animator == null)
                return;

            MaterialAnimatorAuthoring animatorAuthoring = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
            if (animatorAuthoring == null || animatorAuthoring.animations == null || animatorAuthoring.animations.Count == 0)
                return;

            IReadOnlyList<UnitAnimationKind> animationOrder =
                _tryGetUnitRenderingMetadata != null &&
                _tryGetUnitRenderingMetadata(previewInstance, out UnitRenderingMetadata metadata)
                    ? metadata.AnimationOrder
                    : null;
            int animationIndex = ResolveConfiguredPreviewAnimationIndex(animationOrder, UnitAnimationKind.Idle, UnitAnimationKind.Walk, UnitAnimationKind.Aim);
            animationIndex = Mathf.Clamp(animationIndex, 0, animatorAuthoring.animations.Count - 1);
            MaterialAnimatorBake animation = animatorAuthoring.animations[animationIndex];
            int boneCount = Mathf.Max(1, animatorAuthoring.bonesCount);
            int frameCount = Mathf.Max(1, animation.frames);
            int chosenFrame = Mathf.Clamp(Mathf.FloorToInt(frameCount * 0.35f), 0, frameCount - 1);
            Vector4 renderPixel = new(animation.start + chosenFrame * boneCount, animation.start + chosenFrame * boneCount, 0f, 0f);

            Renderer[] animatedRenderers = indexAuthoring.GetComponentsInChildren<Renderer>(true);
            if (animatedRenderers == null || animatedRenderers.Length == 0)
                return;

            MaterialPropertyBlock propertyBlock = new();
            for (int rendererIndex = 0; rendererIndex < animatedRenderers.Length; rendererIndex++)
            {
                Renderer renderer = animatedRenderers[rendererIndex];
                if (renderer == null)
                    continue;

                int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetFloat(SnivelerModelShownId, 1f);
                    propertyBlock.SetVector(SnivelerRenderPixelId, renderPixel);
                    renderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }

        private static int ResolveConfiguredPreviewAnimationIndex(IReadOnlyList<UnitAnimationKind> animationOrder, params UnitAnimationKind[] preferredKinds)
        {
            if (animationOrder != null)
            {
                for (int preferredIndex = 0; preferredIndex < preferredKinds.Length; preferredIndex++)
                {
                    UnitAnimationKind preferred = preferredKinds[preferredIndex];
                    for (int orderIndex = 0; orderIndex < animationOrder.Count; orderIndex++)
                    {
                        if (animationOrder[orderIndex] == preferred)
                            return (int)preferred + 1;
                    }
                }
            }

            return preferredKinds != null && preferredKinds.Length > 1 ? 1 : 0;
        }

        private static bool IsCharacterPreview(GameObject prefab)
        {
            if (prefab == null)
                return false;

            return prefab.name.StartsWith("Unit_Chr_");
        }

        private static bool IsVehiclePreview(GameObject prefab)
        {
            if (prefab == null)
                return false;

            return prefab.name.StartsWith("Unit_Veh_");
        }

        private static bool IsBuildingPreview(GameObject prefab)
        {
            if (prefab == null)
                return false;

            return !IsCharacterPreview(prefab) && !IsVehiclePreview(prefab);
        }

        private static bool IsTentBuildingPreview(GameObject prefab)
        {
            return prefab != null && prefab.name.StartsWith("Tent_", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyFixedPreviewPose(GameObject prefab, Transform previewRoot)
        {
            if (previewRoot == null)
                return;

            if (IsCharacterPreview(prefab))
            {
                previewRoot.position = _characterPreviewModelPosition;
                previewRoot.rotation = _characterPreviewModelRotation;
                return;
            }

            if (IsVehiclePreview(prefab))
            {
                previewRoot.position = _vehiclePreviewModelPosition;
                previewRoot.rotation = _vehiclePreviewModelRotation;
                return;
            }

            previewRoot.position = _buildingPreviewModelPosition;
            previewRoot.rotation = _buildingPreviewModelRotation;
        }

        private static Vector3 FitCameraPositionToBounds(Bounds bounds, Vector3 baseCameraPosition, Quaternion cameraRotation, float distanceMultiplier)
        {
            float verticalHalfFovRadians = Mathf.Max(0.01f, _previewCamera != null ? _previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad : 12f * Mathf.Deg2Rad);
            float horizontalHalfFovRadians = Mathf.Atan(Mathf.Tan(verticalHalfFovRadians) * 1f);
            float tanVertical = Mathf.Tan(verticalHalfFovRadians);
            float tanHorizontal = Mathf.Tan(horizontalHalfFovRadians);
            Quaternion inverseRotation = Quaternion.Inverse(cameraRotation);
            Vector3[] corners = GetBoundsCorners(bounds);
            float requiredDelta = float.NegativeInfinity;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 cameraSpace = inverseRotation * (corners[i] - baseCameraPosition);
                requiredDelta = Mathf.Max(requiredDelta, Mathf.Abs(cameraSpace.x) / Mathf.Max(0.0001f, tanHorizontal) - cameraSpace.z);
                requiredDelta = Mathf.Max(requiredDelta, Mathf.Abs(cameraSpace.y) / Mathf.Max(0.0001f, tanVertical) - cameraSpace.z);
                requiredDelta = Mathf.Max(requiredDelta, 0.1f - cameraSpace.z);
            }

            float padding = Mathf.Max(0.15f, bounds.extents.magnitude * 0.1f) * distanceMultiplier;
            float moveBackDistance = Mathf.Max(0f, requiredDelta + padding);
            return baseCameraPosition - (cameraRotation * Vector3.forward * moveBackDistance);
        }

        private static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        private static bool TryGetDominantTentBounds(Transform previewRoot, out Bounds dominantBounds)
        {
            dominantBounds = default;
            if (previewRoot == null)
                return false;

            bool hasAny = false;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < previewRoot.childCount; i++)
            {
                Transform child = previewRoot.GetChild(i);
                Renderer[] childRenderers = child.GetComponentsInChildren<Renderer>(true);
                Bounds childBounds = default;
                bool hasChildBounds = false;

                for (int j = 0; j < childRenderers.Length; j++)
                {
                    Renderer renderer = childRenderers[j];
                    if (renderer == null || !renderer.enabled)
                        continue;

                    if (!hasChildBounds)
                    {
                        childBounds = renderer.bounds;
                        hasChildBounds = true;
                    }
                    else
                    {
                        childBounds.Encapsulate(renderer.bounds);
                    }
                }

                if (!hasChildBounds)
                    continue;

                Vector3 size = childBounds.size;
                float score = size.x * size.y * size.z;
                if (!hasAny || score > bestScore)
                {
                    dominantBounds = childBounds;
                    bestScore = score;
                    hasAny = true;
                }
            }

            return hasAny;
        }

        private static void RecenterPreviewRootToBounds(Transform previewRoot, Bounds bounds)
        {
            if (previewRoot == null)
                return;

            Vector3 offset = new(bounds.center.x, bounds.min.y, bounds.center.z);
            previewRoot.position -= offset;
        }

        private static bool TryCollectEnabledRendererBounds(Transform previewRoot, out Bounds bounds)
        {
            bounds = default;
            if (previewRoot == null)
                return false;

            Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static Transform FindNamedTransform(Transform root, string containsName)
        {
            if (root == null || string.IsNullOrEmpty(containsName))
                return null;

            string lowerName = containsName.ToLowerInvariant();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                if (current == null)
                    continue;

                if (current.name.ToLowerInvariant().Contains(lowerName))
                    return current;
            }

            return null;
        }
    }
}
