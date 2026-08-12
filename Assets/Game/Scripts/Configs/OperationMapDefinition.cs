using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Configs
{
    [Serializable]
    public struct OperationMapSourceBindingConfig
    {
        [SerializeField] private string sourceOperationMapId;
        [SerializeField] private string sourceIdentityHash;
        [SerializeField] private string sourceContentHash;

        public OperationMapSourceBindingConfig(
            string sourceOperationMapId, string sourceIdentityHash, string sourceContentHash)
        {
            this.sourceOperationMapId = sourceOperationMapId;
            this.sourceIdentityHash = sourceIdentityHash;
            this.sourceContentHash = sourceContentHash;
        }

        public string SourceOperationMapId => sourceOperationMapId;
        public string SourceIdentityHash => sourceIdentityHash;
        public string SourceContentHash => sourceContentHash;
        public bool IsConfigured => !string.IsNullOrEmpty(sourceOperationMapId);

        public bool TryValidate(string logicalOperationMapId, out string error)
        {
            if (!IsConfigured)
            {
                if (!string.IsNullOrEmpty(sourceIdentityHash) || !string.IsNullOrEmpty(sourceContentHash))
                {
                    error = "Operation-map source binding hashes require a source operation-map id.";
                    return false;
                }

                error = null;
                return true;
            }

            if (!OperationMapIdentityRules.IsValidOperationMapId(sourceOperationMapId) ||
                string.Equals(sourceOperationMapId, logicalOperationMapId, StringComparison.Ordinal) ||
                !OperationMapHashRules.IsValidSha256(sourceIdentityHash) ||
                !OperationMapHashRules.IsValidSha256(sourceContentHash))
            {
                error = "Operation-map physical-source binding is invalid or self-referential.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [CreateAssetMenu(menuName = "Game/Operation Maps/Operation Map Definition")]
    public sealed class OperationMapDefinition : ScriptableObject
    {
        [SerializeField] private string operationMapId;
        [SerializeField, Min(1)] private int schemaVersion = 1;
        [SerializeField, Min(1)] private int contentVersion = 1;
        [SerializeField] private string sourceIdentityHash;
        [SerializeField] private string contentHash;
        [SerializeField] private string generatedMetadataHash;
        [SerializeField] private OperationMapSourceBindingConfig sourceBinding;
        [SerializeField] private OperationMapBoundsConfig bounds;
        [SerializeField] private OperationMapGridMetadataConfig gridMetadata;
        [SerializeField] private OperationMapSurfaceMetadataConfig surfaceMetadata;
        [SerializeField] private OperationMapNavigationMetadataConfig navigationMetadata;
        [SerializeField] private OperationMapCameraConfig[] cameras = Array.Empty<OperationMapCameraConfig>();
        [SerializeField] private string planningCameraId;
        [SerializeField] private string battleCameraId;
        [SerializeField] private OperationMapMinimapConfig minimap;
        [SerializeField] private OperationMapAnchorConfig[] anchors = Array.Empty<OperationMapAnchorConfig>();
        [Header("Lazy map content")]
        [SerializeField] private OperationMapPresentationKind presentationKind =
            OperationMapPresentationKind.StaticSceneChunks;
        [SerializeField] private OperationMapRenderResidencyMode renderResidencyMode =
            OperationMapRenderResidencyMode.ResidentEntities;
        [SerializeField] private AssetReference sourceSceneReference;
        [SerializeField] private AssetReference optionalHeavyMetadataReference;
        [SerializeField] private AssetReference staticPresentationManifestReference;
        [SerializeField] private AssetReference mapSurfaceDataReference;
        [SerializeField] private AssetReference minimapRasterReference;
        [SerializeField] private AssetReference buildingPlacementsReference;
        [SerializeField] private AssetReference vehiclePlacementsReference;

        public string OperationMapId => operationMapId;
        public int SchemaVersion => schemaVersion;
        public int ContentVersion => contentVersion;
        public OperationMapPresentationKind PresentationKind => presentationKind;
        public OperationMapRenderResidencyMode RenderResidencyMode => renderResidencyMode;
        public string SourceIdentityHash => sourceIdentityHash;
        public string ContentHash => contentHash;
        public string GeneratedMetadataHash => generatedMetadataHash;
        public OperationMapSourceBindingConfig SourceBinding => sourceBinding;
        public OperationMapBoundsConfig Bounds => bounds;
        public OperationMapGridMetadataConfig GridMetadata => gridMetadata;
        public OperationMapSurfaceMetadataConfig SurfaceMetadata => surfaceMetadata;
        public OperationMapNavigationMetadataConfig NavigationMetadata => navigationMetadata;
        public ReadOnlySpan<OperationMapCameraConfig> Cameras => cameras;
        public string PlanningCameraId => planningCameraId;
        public string BattleCameraId => battleCameraId;
        public OperationMapMinimapConfig Minimap => minimap;
        public ReadOnlySpan<OperationMapAnchorConfig> Anchors => anchors;
        public AssetReference SourceSceneReference => sourceSceneReference;
        public AssetReference OptionalHeavyMetadataReference => optionalHeavyMetadataReference;
        public AssetReference StaticPresentationManifestReference => staticPresentationManifestReference;
        public AssetReference MapSurfaceDataReference => mapSurfaceDataReference;
        public AssetReference MinimapRasterReference => minimapRasterReference;
        public AssetReference BuildingPlacementsReference => buildingPlacementsReference;
        public AssetReference VehiclePlacementsReference => vehiclePlacementsReference;

#if UNITY_EDITOR
        public void EditorSetSurfaceMetadata(OperationMapSurfaceMetadataConfig value)
        {
            surfaceMetadata = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public bool TryValidateIdentity(out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (schemaVersion < 1 || contentVersion < 1)
            {
                error = "Schema and content versions must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidateMetadata(out string error)
        {
            if (!TryValidateIdentity(out error) ||
                !TryValidateHashes(out error) ||
                !sourceBinding.TryValidate(operationMapId, out error) ||
                !bounds.TryValidate(out error) ||
                !gridMetadata.TryValidate(out error) ||
                !surfaceMetadata.TryValidate(out error) ||
                !navigationMetadata.TryValidate(out error))
                return false;

            Vector3 expectedGridMax = gridMetadata.Origin + new Vector3(
                gridMetadata.Dimensions.x * gridMetadata.CellSize,
                0f,
                gridMetadata.Dimensions.y * gridMetadata.CellSize);
            if (bounds.WorldMin.x != gridMetadata.Origin.x ||
                bounds.WorldMin.z != gridMetadata.Origin.z ||
                bounds.WorldMax.x != expectedGridMax.x ||
                bounds.WorldMax.z != expectedGridMax.z)
            {
                error = "World X/Z bounds must exactly match the canonical grid extent.";
                return false;
            }

            if (cameras == null || cameras.Length == 0)
            {
                error = "At least one operation-map camera record is required.";
                return false;
            }

            for (int index = 0; index < cameras.Length; index++)
            {
                if (!cameras[index].TryValidate(out error))
                    return false;

                if (!OperationMapConfigValidation.Contains(bounds.CameraMin, bounds.CameraMax, cameras[index].Position))
                {
                    error = $"Camera '{cameras[index].CameraId}' position must remain inside camera bounds.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(cameras[index].CameraId, cameras[previous].CameraId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map camera id: '{cameras[index].CameraId}'.";
                        return false;
                    }
                }
            }

            if (!ContainsCamera(planningCameraId))
            {
                error = $"Planning camera id '{planningCameraId ?? "<null>"}' does not resolve to a camera record.";
                return false;
            }

            if (!ContainsCamera(battleCameraId))
            {
                error = $"Battle camera id '{battleCameraId ?? "<null>"}' does not resolve to a camera record.";
                return false;
            }

            if (!minimap.TryValidate(out error))
                return false;

            if (anchors == null || anchors.Length == 0)
            {
                error = "At least one typed operation-map anchor record is required.";
                return false;
            }

            for (int index = 0; index < anchors.Length; index++)
            {
                if (!anchors[index].TryValidate(out error))
                    return false;

                if (!OperationMapConfigValidation.Contains(bounds.WorldMin, bounds.WorldMax, anchors[index].Position))
                {
                    error = $"Anchor '{anchors[index].AnchorId}' position must remain inside world bounds.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(anchors[index].AnchorId, anchors[previous].AnchorId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map anchor id: '{anchors[index].AnchorId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryValidateHashes(out string error)
        {
            if (!OperationMapHashRules.IsValidSha256(sourceIdentityHash))
            {
                error = "Operation-map source identity hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            if (!OperationMapHashRules.IsValidSha256(contentHash))
            {
                error = "Operation-map content hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            if (!OperationMapHashRules.IsValidSha256(generatedMetadataHash))
            {
                error = "Operation-map generated-metadata hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidateLocalContentReferences(out string error)
        {
            if (presentationKind != OperationMapPresentationKind.StaticSceneChunks &&
                presentationKind != OperationMapPresentationKind.EntityScene)
            {
                error = $"Unknown operation-map presentation kind: {(byte)presentationKind}.";
                return false;
            }

            if (renderResidencyMode != OperationMapRenderResidencyMode.ResidentEntities &&
                renderResidencyMode != OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                error = $"Unknown operation-map render-residency mode: {(byte)renderResidencyMode}.";
                return false;
            }

            if (presentationKind == OperationMapPresentationKind.StaticSceneChunks &&
                renderResidencyMode != OperationMapRenderResidencyMode.ResidentEntities)
            {
                error =
                    "StaticSceneChunks operation maps require ResidentEntities render residency.";
                return false;
            }

            if (!TryValidateRequiredReference(sourceSceneReference, "source scene", out error) ||
                !TryValidateRequiredReference(mapSurfaceDataReference, "map surface data", out error) ||
                !TryValidateRequiredReference(minimapRasterReference, "minimap raster", out error))
            {
                return false;
            }

            if (presentationKind == OperationMapPresentationKind.StaticSceneChunks)
            {
                if (!TryValidateRequiredReference(
                        staticPresentationManifestReference,
                        "static presentation manifest",
                        out error) ||
                    !TryValidateRequiredReference(
                        buildingPlacementsReference,
                        "building placements",
                        out error) ||
                    !TryValidateRequiredReference(
                        vehiclePlacementsReference,
                        "vehicle placements",
                        out error))
                {
                    return false;
                }
            }
            else
            {
                if (HasConfiguredReference(staticPresentationManifestReference))
                {
                    error =
                        "EntityScene operation maps must not require a production static presentation manifest reference.";
                    return false;
                }

                // Legacy placement AssetReferences may remain as migration evidence only.
                // Runtime spawning is rejected separately once EntityScene loading is activated.
            }

            if (optionalHeavyMetadataReference != null &&
                !string.IsNullOrEmpty(optionalHeavyMetadataReference.AssetGUID) &&
                (!IsValidAssetGuid(optionalHeavyMetadataReference.AssetGUID) ||
                 !optionalHeavyMetadataReference.RuntimeKeyIsValid()))
            {
                error = "Optional heavy metadata reference is present but invalid.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool HasConfiguredReference(AssetReference reference)
        {
            return reference != null && !string.IsNullOrEmpty(reference.AssetGUID);
        }

        private static bool TryValidateRequiredReference(
            AssetReference reference,
            string role,
            out string error)
        {
            if (reference == null ||
                !IsValidAssetGuid(reference.AssetGUID) ||
                !reference.RuntimeKeyIsValid())
            {
                error = $"Operation-map {role} reference is missing or invalid.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsValidAssetGuid(string value)
        {
            if (value == null || value.Length != 32)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                    return false;
            }

            return true;
        }

        public bool TryCreatePersistentMetadataBlob(
            out BlobAssetReference<OperationMapBlob> metadataBlob,
            out string error)
        {
            metadataBlob = default;
            if (!TryValidateMetadata(out error))
                return false;

            using BlobBuilder builder = new(Allocator.Temp);
            ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
            root.OperationMapId = new FixedString64Bytes(operationMapId);
            root.PlanningCameraId = new FixedString64Bytes(planningCameraId);
            root.BattleCameraId = new FixedString64Bytes(battleCameraId);
            root.SourceIdentityHash = new FixedString128Bytes(sourceIdentityHash);
            root.ContentHash = new FixedString128Bytes(contentHash);
            root.GeneratedMetadataHash = new FixedString128Bytes(generatedMetadataHash);
            root.SchemaVersion = schemaVersion;
            root.ContentVersion = contentVersion;
            root.Grid = new OperationMapGridBlob
            {
                AssetGuid = new FixedString64Bytes(gridMetadata.AssetGuid),
                ContentHash = new FixedString128Bytes(gridMetadata.ContentHash),
                Origin = ToFloat3(gridMetadata.Origin),
                Dimensions = new int2(gridMetadata.Dimensions.x, gridMetadata.Dimensions.y),
                CellSize = gridMetadata.CellSize,
                AuthoredBlockedCellCount = gridMetadata.AuthoredBlockedCellCount
            };
            root.Surface = new OperationMapSurfaceMetadataBlob
            {
                AssetGuid = new FixedString64Bytes(surfaceMetadata.AssetGuid),
                ContentHash = new FixedString128Bytes(surfaceMetadata.ContentHash),
                RuntimeBlobHash = new FixedString64Bytes(surfaceMetadata.RuntimeBlobHash),
                SurfaceCount = surfaceMetadata.SurfaceCount,
                PayloadVersion = surfaceMetadata.PayloadVersion,
                PayloadEncoding = surfaceMetadata.PayloadEncoding,
                MinimumHeight = surfaceMetadata.MinimumHeight,
                MaximumHeight = surfaceMetadata.MaximumHeight
            };
            root.Navigation = new OperationMapNavigationMetadataBlob
            {
                AuthoredSubSceneGuid = new FixedString64Bytes(navigationMetadata.AuthoredSubSceneGuid),
                GridAuthoringLocalId = navigationMetadata.GridAuthoringLocalId,
                StaticGridBlockerCount = navigationMetadata.StaticGridBlockerCount,
                UsesSurfaceMovementMetadata = navigationMetadata.UsesSurfaceMovementMetadata ? (byte)1 : (byte)0,
                SupportsDynamicBlockers = navigationMetadata.SupportsDynamicBlockers ? (byte)1 : (byte)0,
                SupportsDynamicOccupancy = navigationMetadata.SupportsDynamicOccupancy ? (byte)1 : (byte)0
            };

            BlobBuilderArray<OperationMapAnchorBlob> blobAnchors = builder.Allocate(ref root.Anchors, anchors.Length);
            for (int index = 0; index < anchors.Length; index++)
            {
                OperationMapAnchorConfig source = anchors[index];
                blobAnchors[index] = new OperationMapAnchorBlob
                {
                    Id = new FixedString64Bytes(source.AnchorId),
                    Kind = source.Kind,
                    Position = ToFloat3(source.Position),
                    Rotation = ToQuaternion(source.EulerAngles),
                    Radius = source.Radius,
                    FactionId = source.FactionId,
                    LaneIndex = source.LaneIndex
                };
            }

            BlobBuilderArray<OperationMapCameraBlob> blobCameras = builder.Allocate(ref root.Cameras, cameras.Length);
            for (int index = 0; index < cameras.Length; index++)
            {
                OperationMapCameraConfig source = cameras[index];
                blobCameras[index] = new OperationMapCameraBlob
                {
                    Id = new FixedString64Bytes(source.CameraId),
                    Position = ToFloat3(source.Position),
                    Rotation = ToQuaternion(source.EulerAngles),
                    FieldOfView = source.FieldOfView,
                    OrthographicSize = source.OrthographicSize,
                    IsOrthographic = source.Orthographic ? (byte)1 : (byte)0,
                    ClampToCameraBounds = source.ClampToCameraBounds ? (byte)1 : (byte)0
                };
            }

            root.Minimap = new OperationMapMinimapBlob
            {
                Id = new FixedString64Bytes(minimap.MinimapId),
                ProjectionOrigin = ToFloat3(minimap.ProjectionOrigin),
                ProjectionSize = new float2(minimap.ProjectionSize.x, minimap.ProjectionSize.y),
                OrientationDegrees = minimap.OrientationDegrees
            };

            metadataBlob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
            if (!metadataBlob.IsCreated)
            {
                error = "Failed to create persistent operation-map metadata blob.";
                return false;
            }

            error = null;
            return true;
        }

        private static float3 ToFloat3(Vector3 value) => new(value.x, value.y, value.z);

        private static quaternion ToQuaternion(Vector3 eulerAngles)
        {
            Quaternion value = Quaternion.Euler(eulerAngles);
            return new quaternion(value.x, value.y, value.z, value.w);
        }

        private bool ContainsCamera(string cameraId)
        {
            if (!OperationMapIdentityRules.IsValidCameraId(cameraId))
                return false;

            for (int index = 0; index < cameras.Length; index++)
            {
                if (string.Equals(cameraId, cameras[index].CameraId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

}
